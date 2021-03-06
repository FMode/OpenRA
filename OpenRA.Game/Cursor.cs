﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;

namespace OpenRA
{
	public class Cursor
	{
		CursorSequence sequence;
		public Cursor(string cursor)
		{
			sequence = CursorProvider.GetCursorSequence(cursor);
		}
		
		public void Draw(int frame, float2 pos)
		{
			sequence.GetSprite(frame).DrawAt(pos - sequence.Hotspot, Game.modData.Palette.GetPaletteIndex(sequence.Palette));
		}
	}
}
