#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Air;

namespace OpenRA.Mods.RA.Missions
{
    class BadgerDesyncTestScriptInfo : TraitInfo<BadgerDesyncTestScript> { }

    class BadgerDesyncTestScript : ITick
    {
        int ticks;

        public void Tick(Actor self)
        {
            ticks++;

            if ((ticks % 50) == 20)
            {
                var w = self.World;
                var neutralPlayer = w.players.Values.First(p => p.PlayerRef.OwnsWorld);

                for (var i = 0; i < 50; i++)
                {
                    var badr = w.CreateActor("badr",
                        new TypeDictionary { 
                            new OwnerInit( neutralPlayer ),
                            new AltitudeInit( 30 ),
                            new LocationInit( w.ChooseRandomCell( w.SharedRandom ) ),
                            new FacingInit( w.SharedRandom.Next(256) ) });

                    badr.QueueActivity(Fly.ToCell(w.ChooseRandomCell(w.SharedRandom)));
                }
            }

            if ((ticks % 50) == 41)
            {
                foreach (var badr in self.World.Actors.Where(a => a.Info.Name == "badr"))
                    badr.Kill(badr);
            }
        }
    }
}
