﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class MineInfo : ITraitInfo
	{
		public readonly string[] CrushClasses = { };
		[WeaponReference]
		public readonly string Weapon = "ATMine";
		public readonly bool AvoidFriendly = true;

		public object Create(ActorInitializer init) { return new Mine(init, this); }
	}

	class Mine : ICrushable, IOccupySpace, ISync
	{
		readonly Actor self;
		readonly MineInfo info;
		[Sync]
		readonly int2 location;

		public Mine(ActorInitializer init, MineInfo info)
		{
			this.self = init.self;
			this.info = info;
			this.location = init.Get<LocationInit,int2>();
		}

		public void OnCrush(Actor crusher)
		{
			if (crusher.HasTrait<MineImmune>() && crusher.Owner == self.Owner)
				return;

			var info = self.Info.Traits.Get<MineInfo>();
			Combat.DoExplosion(self, info.Weapon, crusher.CenterLocation, 0);
			self.QueueActivity(new RemoveSelf());
		}
		
		// TODO: Re-implement friendly-mine avoidance
		public IEnumerable<string> CrushClasses { get { return info.CrushClasses; } }
		
		public int2 TopLeft { get { return location; } }

		public IEnumerable<Pair<int2, SubCell>> OccupiedCells() { yield return Pair.New(TopLeft, SubCell.FullCell); }
		public int2 PxPosition { get { return Util.CenterOfCell( location ); } }
	}

	/* tag trait for stuff that shouldnt trigger mines */
	class MineImmuneInfo : TraitInfo<MineImmune> { }
	class MineImmune { }
}
