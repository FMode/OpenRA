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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public interface IPlaceBuildingDecoration
	{
		void Render(WorldRenderer wr, World w, ActorInfo ai, int2 centerLocation);
	}

	class RenderRangeCircleInfo : TraitInfo<RenderRangeCircle>, IPlaceBuildingDecoration
	{
		public readonly string RangeCircleType = null;

		public void Render(WorldRenderer wr, World w, ActorInfo ai, int2 centerLocation)
		{
			wr.DrawRangeCircle(
				Color.FromArgb(128, Color.Yellow),
				centerLocation,
				ai.Traits.Get<AttackBaseInfo>().GetMaximumRange());

			foreach (var a in w.Queries.OwnedBy[w.LocalPlayer].WithTrait<RenderRangeCircle>())
				if (a.Actor.Info.Traits.Get<RenderRangeCircleInfo>().RangeCircleType == RangeCircleType)
					a.Trait.RenderBeforeWorld(wr, a.Actor);
		}
	}

	class RenderRangeCircle : IPreRenderSelection
	{
		public void RenderBeforeWorld(WorldRenderer wr, Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;
			
			wr.DrawRangeCircle(
				Color.FromArgb(128, Color.Yellow),
				self.CenterLocation, (int)self.Trait<AttackBase>().GetMaximumRange());
		}
	}
}
