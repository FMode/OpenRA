﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class AttackMoveInfo : ITraitInfo
	{
		public readonly bool JustMove = false;
		
		public object Create(ActorInitializer init) { return new AttackMove(init.self, this); }
	}

	class AttackMove : IResolveOrder, IOrderVoice, INotifyIdle, ISync
	{
		[Sync] public int2 _targetLocation { get { return TargetLocation.HasValue ? TargetLocation.Value : int2.Zero; } }
		public int2? TargetLocation = null; 
		
		readonly Mobile mobile;
		readonly AttackMoveInfo Info;
		
		public AttackMove(Actor self, AttackMoveInfo info)
		{
			Info = info;
			mobile = self.Trait<Mobile>();
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove")
				return "AttackMove";
			return null;
		}
		
		
		void Activate(Actor self)
		{
			self.CancelActivity();
			self.QueueActivity(new AttackMoveActivity(mobile.MoveTo(TargetLocation.Value, 1)));
			self.SetTargetLine(Target.FromCell(TargetLocation.Value), Color.Red);
		}
		
		public void TickIdle(Actor self)
		{
			if (TargetLocation.HasValue)
				Activate(self);
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			TargetLocation = null;
			if (order.OrderString == "AttackMove")
			{
				if (Info.JustMove)
					mobile.ResolveOrder(self, new Order("Move", order));
				else
				{
					TargetLocation = mobile.NearestMoveableCell(order.TargetLocation);
					Activate(self);
				}
			}
		}

		class AttackMoveActivity : CancelableActivity
		{
			IActivity inner;
			public AttackMoveActivity( IActivity inner ) { this.inner = inner; }

			public override IActivity Tick( Actor self )
			{
				self.Trait<AttackBase>().ScanAndAttack(self, true);

				if( inner == null )
					return NextActivity;
				
				inner = Util.RunActivity( self, inner );
				
				return this;
			}

			protected override bool OnCancel( Actor self )
			{
				if( inner != null )
					inner.Cancel( self );
				return base.OnCancel( self );
			}
		}
	}
}
