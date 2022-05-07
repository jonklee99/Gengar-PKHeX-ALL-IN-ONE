﻿using System;
using System.Collections.Generic;
using System.Linq;

using static PKHeX.Core.Legal;
using static PKHeX.Core.Encounters1;
using static PKHeX.Core.Encounters2;
using static PKHeX.Core.Encounters3;
using static PKHeX.Core.Encounters3GC;
using static PKHeX.Core.Encounters4;
using static PKHeX.Core.Encounters5;
using static PKHeX.Core.Encounters6;
using static PKHeX.Core.Encounters7;
using static PKHeX.Core.Encounters7b;
using static PKHeX.Core.Encounters8;
using static PKHeX.Core.Encounters8a;
using static PKHeX.Core.Encounters8b;

using static PKHeX.Core.GameVersion;

namespace PKHeX.Core
{
    public static class EncounterStaticGenerator
    {
        public static IEnumerable<EncounterStatic> GetPossible(PKM pkm, IReadOnlyList<EvoCriteria> chain, GameVersion gameSource)
        {
            var table = gameSource switch
            {
                RD or GN or BU or YW => StaticRBY.Where(z => z.Version.Contains(gameSource)),
                GD or SI => StaticGS.Where(z => z.Version.Contains(gameSource)),
                C => StaticC,
                _ => GetEncounterStaticTable(pkm, gameSource),
            };
            return table.Where(e => chain.Any(d => d.Species == e.Species));
        }

        public static IEnumerable<EncounterStatic> GetPossibleGBGifts(IReadOnlyList<EvoCriteria> chain, GameVersion gameSource)
        {
            static IEnumerable<EncounterStatic> GetEvents(GameVersion g)
            {
                if (g.GetGeneration() == 1)
                    return !ParseSettings.AllowGBCartEra ? Encounters1.StaticEventsVC : Encounters1.StaticEventsGB;

                return !ParseSettings.AllowGBCartEra ? Encounters2.StaticEventsVC : Encounters2.StaticEventsGB;
            }

            var table = GetEvents(gameSource);
            return table.Where(e => chain.Any(d => d.Species == e.Species));
        }

        public static IEnumerable<EncounterStatic> GetValidStaticEncounter(PKM pkm, IReadOnlyList<EvoCriteria> chain)
        {
            return GetValidStaticEncounter(pkm, chain, (GameVersion)pkm.Version);
        }

        public static IEnumerable<EncounterStatic> GetValidStaticEncounter(PKM pkm, IReadOnlyList<EvoCriteria> chain, GameVersion gameSource)
        {
            var table = GetEncounterStaticTable(pkm, gameSource);
            return GetMatchingStaticEncounters(pkm, table, chain);
        }

        public static IEnumerable<EncounterStatic> GetValidGBGifts(PKM pkm, IReadOnlyList<EvoCriteria> chain, GameVersion gameSource)
        {
            var poss = GetPossibleGBGifts(chain, gameSource: gameSource);
            foreach (EncounterStatic e in poss)
            {
                foreach (var dl in chain)
                {
                    if (dl.Species != e.Species)
                        continue;
                    if (!e.IsMatchExact(pkm, dl))
                        continue;

                    yield return e;
                }
            }
        }

        private static IEnumerable<EncounterStatic> GetMatchingStaticEncounters(PKM pkm, IEnumerable<EncounterStatic> poss, IReadOnlyList<EvoCriteria> evos)
        {
            // check for petty rejection scenarios that will be flagged by other legality checks
            foreach (var e in poss)
            {
                foreach (var dl in evos)
                {
                    if (dl.Species != e.Species)
                        continue;
                    if (!e.IsMatchExact(pkm, dl))
                        continue;

                    yield return e;
                }
            }
        }

        internal static EncounterStatic7 GetVCStaticTransferEncounter(PKM pkm, IEncounterTemplate enc, ReadOnlySpan<EvoCriteria> chain)
        {
            // Obtain the lowest evolution species with matching OT friendship. Not all species chains have the same base friendship.
            var met = (byte)pkm.Met_Level;
            if (pkm.VC1)
            {
                // Only yield a VC1 template if it could originate in VC1.
                // Catch anything that can only exist in VC2 (Entei) even if it was "transferred" from VC1.
                var species = GetVCSpecies(chain, pkm, MaxSpeciesID_1);
                var vc1Species = species > MaxSpeciesID_1 ? enc.Species : species;
                if (vc1Species <= MaxSpeciesID_1)
                    return EncounterStatic7.GetVC1(vc1Species, met);
            }
            // fall through else
            {
                var species = GetVCSpecies(chain, pkm, MaxSpeciesID_2);
                return EncounterStatic7.GetVC2(species > MaxSpeciesID_2 ? enc.Species : species, met);
            }
        }

        private static int GetVCSpecies(ReadOnlySpan<EvoCriteria> chain, PKM pkm, int max)
        {
            int species = pkm.Species;
            foreach (var z in chain)
            {
                if (z.Species > max)
                    continue;
                if (z.Form != 0)
                    continue;
                if (PersonalTable.SM.GetFormEntry(z.Species, z.Form).BaseFriendship != pkm.OT_Friendship)
                    continue;
                species = z.Species;
            }
            return species;
        }

        internal static EncounterStatic? GetStaticLocation(PKM pkm, IReadOnlyList<EvoCriteria> chain)
        {
            switch (pkm.Generation)
            {
                case 1:
                    return EncounterStatic7.GetVC1(MaxSpeciesID_1, (byte)pkm.Met_Level);
                case 2:
                    return EncounterStatic7.GetVC2(MaxSpeciesID_2, (byte)pkm.Met_Level);
                default:
                    return GetPossible(pkm, chain, (GameVersion)pkm.Version)
                        .OrderBy(z => !chain.Any(s => s.Species == z.Species && s.Form == z.Form))
                        .ThenBy(z => z.LevelMin)
                        .FirstOrDefault();
            }
        }

        // Generation Specific Fetching
        private static IEnumerable<EncounterStatic> GetEncounterStaticTable(PKM pkm, GameVersion game) => game switch
        {
            RBY or RD or BU or GN or YW => StaticRBY,

            GSC or GD or SI or C => GetEncounterStaticTableGSC(pkm),

            R => StaticR,
            S => StaticS,
            E => StaticE,
            FR => StaticFR,
            LG => StaticLG,
            CXD => Encounter_CXD,

            D => StaticD,
            P => StaticP,
            Pt => StaticPt,
            HG => StaticHG,
            SS => StaticSS,

            B => StaticB,
            W => StaticW,
            B2 => StaticB2,
            W2 => StaticW2,

            X => StaticX,
            Y => StaticY,
            AS => StaticA,
            OR => StaticO,

            SN => StaticSN,
            MN => StaticMN,
            US => StaticUS,
            UM => StaticUM,
            GP => StaticGP,
            GE => StaticGE,

            SW => StaticSW,
            SH => StaticSH,
            BD => StaticBD,
            SP => StaticSP,
            PLA => StaticLA,
            _ => Array.Empty<EncounterStatic>(),
        };

        private static IEnumerable<EncounterStatic> GetEncounterStaticTableGSC(PKM pkm)
        {
            if (!ParseSettings.AllowGen2Crystal(pkm))
                return StaticGS;
            if (pkm.Format != 2)
                return StaticGSC;

            if (pkm.HasOriginalMetLocation)
                return StaticC;
            return StaticGSC;
        }
    }
}
