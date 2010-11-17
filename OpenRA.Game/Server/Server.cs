#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Network;

namespace OpenRA.Server
{
	public class Server
	{
		public readonly List<Connection> conns = new List<Connection>();
		readonly TcpListener listener = null;
		readonly Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();
		
		readonly TypeDictionary ServerTraits = new TypeDictionary();
		public Session lobbyInfo;
		public bool GameStarted = false;
		public readonly string Name;
		readonly int randomSeed;

		public readonly ModData ModData;
		public Map Map;

		public void Shutdown()
		{
			lock( this )
			{
				conns.Clear();
				GameStarted = false;
				foreach( var t in ServerTraits.WithInterface<INotifyServerShutdown>() )
					t.ServerShutdown( this );

				try { listener.Stop(); }
				catch { }
			}
		}
		
		public Server(ModData modData, Settings settings, string map)
		{
			Log.AddChannel("server", "server.log");

			listener = new TcpListener(IPAddress.Any, settings.Server.ListenPort);
			Name = settings.Server.Name;
			randomSeed = (int)DateTime.Now.ToBinary();
			ModData = modData;

			foreach (var trait in modData.Manifest.ServerTraits)
				ServerTraits.Add( modData.ObjectCreator.CreateObject<ServerTrait>(trait) );
			
			lobbyInfo = new Session( settings.Game.Mods );
			lobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			lobbyInfo.GlobalSettings.Map = map;
			lobbyInfo.GlobalSettings.AllowCheats = settings.Server.AllowCheats;
			lobbyInfo.GlobalSettings.ServerName = settings.Server.Name;
			
			foreach (var t in ServerTraits.WithInterface<INotifyServerStart>())
				t.ServerStarted(this);
						
			Log.Write("server", "Initial mods: ");
			foreach( var m in lobbyInfo.GlobalSettings.Mods )
				Log.Write("server","- {0}", m);
			
			Log.Write("server", "Initial map: {0}",lobbyInfo.GlobalSettings.Map);
			
			try
			{
				listener.Start();
			}
			catch (Exception)
			{
				throw new InvalidOperationException( "Unable to start server: port is already in use" );
			}

			new Thread( _ =>
			{
				var timeout = ServerTraits.WithInterface<ITick>().Min(t => t.TickTimeout);
				for( ; ; )
				{
					var checkRead = new ArrayList();
					checkRead.Add( listener.Server );

					Socket.Select( checkRead, null, null, timeout );

					lock( this )
					{
						foreach( Socket s in checkRead )
							if( s == listener.Server ) AcceptConnection();
							else throw new InvalidOperationException();

						foreach( var t in ServerTraits.WithInterface<ITick>() )
							t.Tick( this );

						if( conns.Count() == 0 )
						{
							listener.Stop();
							GameStarted = false;
							break;
						}
					}
				}
			} ) { IsBackground = true }.Start();
		}

		/* lobby rework todo: 
		 *	- "teams together" option for team games -- will eliminate most need
		 *		for manual spawnpoint choosing.
		 *	- 256 max players is a dirty hack
		 */
		int ChooseFreePlayerIndex()
		{
			for (var i = 0; i < 256; i++)
				if (conns.All(c => c.PlayerIndex != i))
					return i;

			throw new InvalidOperationException("Already got 256 players");
		}

		void AcceptConnection()
		{
			Socket newSocket = null;

			try
			{
				if (!listener.Server.IsBound) return;
				newSocket = listener.AcceptSocket();
			}catch
			{
				/* could have an exception here when listener 'goes away' when calling AcceptConnection! */
				/* alternative would be to use locking but the listener doesnt go away without a reason */
				return; 
			}

			Connection newConn = null;
			try
			{
				if (GameStarted)
				{
					Log.Write("server", "Rejected connection from {0}; game is already started.", 
						newSocket.RemoteEndPoint);
					newSocket.Close();
					return;
				}

				newSocket.NoDelay = true;

				newConn = new Connection( newSocket, ChooseFreePlayerIndex() );
				conns.Add(newConn);

				newConn.StartReader( this );

				foreach (var t in ServerTraits.WithInterface<IClientJoined>())
					t.ClientJoined(this, newConn);
			}
			catch (Exception e) { DropClient(newConn, e); }
		}

		public void UpdateInFlightFrames(int frame, Connection conn)
		{
			if (frame != 0)
			{
				if (!inFlightFrames.ContainsKey(frame))
					inFlightFrames[frame] = new List<Connection> { conn };
				else
					inFlightFrames[frame].Add(conn);

				if (conns.All(c => inFlightFrames[frame].Contains(c)))
				{
					inFlightFrames.Remove(frame);
				}
			}
		}

		void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			try
			{
				var ms = new MemoryStream();
				ms.Write( BitConverter.GetBytes( data.Length + 8 ) );
				ms.Write( BitConverter.GetBytes( client ) );
				ms.Write( BitConverter.GetBytes( frame ) );
				ms.Write( data );
				c.Send( ms.ToArray() );
			}
			catch( Exception e ) { DropClient( c, e ); }
		}

		public void DispatchOrders(Connection conn, int frame, byte[] data)
		{
			if (frame == 0 && conn != null)
				InterpretServerOrders(conn, data);
			else
			{
				var from = conn != null ? conn.PlayerIndex : 0;
				foreach (var c in conns.Except(conn).ToArray())
					DispatchOrdersToClient(c, from, frame, data);
			}
		}

		void InterpretServerOrders(Connection conn, byte[] data)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);

			try
			{
				for (; ; )
				{
					var so = ServerOrder.Deserialize(br);
					if (so == null) return;
					InterpretServerOrder(conn, so);
				}
			}
			catch (EndOfStreamException) { }
			catch (NotImplementedException) { }
		}
		
		public void SendChatTo(Connection conn, string text)
		{
			DispatchOrdersToClient(conn, 0, 0,
				new ServerOrder("Chat", text).Serialize());
		}

        public void SendChat(Connection asConn, string text)
        {
            DispatchOrders(asConn, 0, new ServerOrder("Chat", text).Serialize());
        }

        public void SendDisconnected(Connection asConn)
        {
            DispatchOrders(asConn, 0, new ServerOrder("Disconnected", "").Serialize());
        }

		void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "Command":
					{
						bool handled = false;
						foreach (var t in ServerTraits.WithInterface<IInterpretCommand>())
							if ((handled = t.InterpretCommand(this, conn, GetClient(conn), so.Data)))
								break;
						
						if (!handled)
						{
							Log.Write("server", "Unknown server command: {0}", so.Data);
							SendChatTo(conn, "Unknown server command: {0}".F(so.Data));
						}
					}
					break;

				case "Chat":
				case "TeamChat":
					foreach (var c in conns.Except(conn).ToArray())
						DispatchOrdersToClient(c, GetClient(conn).Index, 0, so.Serialize());
				break;
			}
		}

		public Session.Client GetClient(Connection conn)
		{
			return lobbyInfo.Clients.First(c => c.Index == conn.PlayerIndex);
		}

		public void DropClient(Connection toDrop, Exception e)
		{
			conns.Remove(toDrop);
			SendChat(toDrop, "Connection Dropped");

            if (GameStarted)
                SendDisconnected(toDrop); /* Report disconnection */

			lobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);

			DispatchOrders( toDrop, toDrop.MostRecentFrame, new byte[] { 0xbf } );
			
			if (conns.Count != 0)
				SyncLobbyInfo();
		}

		public void SyncLobbyInfo()
		{
			if (!GameStarted)	/* don't do this while the game is running, it breaks things. */
				DispatchOrders(null, 0,
					new ServerOrder("SyncInfo", lobbyInfo.Serialize()).Serialize());

			foreach (var t in ServerTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}
		
		public void StartGame()
		{
			GameStarted = true;
			foreach( var c in conns )
				foreach( var d in conns )
					DispatchOrdersToClient( c, d.PlayerIndex, 0x7FFFFFFF, new byte[] { 0xBF } );

			DispatchOrders(null, 0,
				new ServerOrder("StartGame", "").Serialize());

			foreach (var t in ServerTraits.WithInterface<IStartGame>())
				t.GameStarted(this);
		}
	}
}
