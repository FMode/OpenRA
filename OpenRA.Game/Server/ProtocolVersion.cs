﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Server
{
	public static class ProtocolVersion
	{
		// you *must* increment this whenever you make an incompatible protocol change
		public static readonly int Version = 7;
	}
}
