#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class NukePaletteEffectInfo : TraitInfo<NukePaletteEffect> { }

	public class NukePaletteEffect : IPaletteModifier, ITick
	{
		const int nukeEffectLength = 20;
		int remainingFrames;

		public void Enable()
		{
			remainingFrames = nukeEffectLength;
		}

		public void Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		public void AdjustPalette(Dictionary<string,Palette> palettes)
		{
			if (remainingFrames == 0)
				return;
			
			var frac = (float)remainingFrames / nukeEffectLength;
			
			var excludePalettes = new List<string>(){"cursor", "chrome", "colorpicker", "shroud", "fog"};
			foreach (var pal in palettes)
			{
				if (excludePalettes.Contains(pal.Key))
					continue;
				
				for (var x = 0; x < 256; x++)
				{
					var orig = pal.Value.GetColor(x);
					var white = Color.FromArgb(orig.A, 255, 255, 255);
					pal.Value.SetColor(x, OpenRA.Graphics.Util.Lerp(frac,orig,white));
				}
			}
		}
	}
}
