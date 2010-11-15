﻿#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Air
{
	class ReturnOnIdleInfo : TraitInfo<ReturnOnIdle> { }

	// fly home or fly-off-map behavior for idle planes

	class ReturnOnIdle : INotifyIdle
	{
		public void TickIdle(Actor self)
		{
			var altitude = self.Trait<Aircraft>().Altitude;
			if (altitude == 0) return;	// we're on the ground, let's stay there.

			var airfield = ReturnToBase.ChooseAirfield(self);
			if (airfield != null)
			{
				self.QueueActivity(new ReturnToBase(self, airfield));
				self.QueueActivity(new Rearm());
			}
			else
			{
				//Game.Debug("Plane has nowhere to land; flying away");
				self.QueueActivity(new FlyOffMap());
				self.QueueActivity(new RemoveSelf());
			}
		}
	}

	class FlyAwayOnIdleInfo : TraitInfo<FlyAwayOnIdle> { }

	class FlyAwayOnIdle : INotifyIdle
	{
		public void TickIdle(Actor self)
		{
			self.QueueActivity(new FlyOffMap());
			self.QueueActivity(new RemoveSelf());
		}
	}
}
