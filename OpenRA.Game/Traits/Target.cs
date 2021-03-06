﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	public struct Target		// a target: either an actor, or a fixed location.
	{
		Actor actor;
		Player owner;
		int2 pos;
		bool valid;

		public static Target FromActor(Actor a)
		{
			return new Target
			{
				actor = a,
				valid = (a != null),
				owner = (a != null) ? a.Owner : null
			};
		}
		public static Target FromPos(int2 p) { return new Target { pos = p, valid = true }; }
		public static Target FromCell(int2 c) { return new Target { pos = Util.CenterOfCell(c), valid = true }; }
		public static Target FromOrder(Order o)
		{
			return o.TargetActor != null
				? Target.FromActor(o.TargetActor)
				: Target.FromCell(o.TargetLocation);
		}

		public static readonly Target None = new Target();

		public bool IsValid { get { return valid && (actor == null || (actor.IsInWorld && actor.Owner == owner)); } }
		public int2 PxPosition { get { return IsActor ? actor.Trait<IHasLocation>().PxPosition : pos; } }
		public int2 CenterLocation { get { return PxPosition; } }

		public Actor Actor { get { return IsActor ? actor : null; } }
		public bool IsActor { get { return actor != null && !actor.Destroyed; } }
	}
}
