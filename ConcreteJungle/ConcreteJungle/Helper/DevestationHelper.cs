using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class DevestationHelper
    {

        public static void DevestateBuildings()
        {
            if (!Mod.Config.Devastation.Enabled) return;

            Mod.Log.Debug("Processing buildings for pre-battle devestation.");

            List<BattleTech.Building> shuffledBuildings = new List<BattleTech.Building>();
            shuffledBuildings.AddRange(ModState.CandidateBuildings);
            
            // Randomize the buildings by shuffling them
            shuffledBuildings.Shuffle();

            int minNum = (int)(Mod.Config.Devastation.DefaultRange.MinDevestation * 100f);
            int maxNum = (int)(Mod.Config.Devastation.DefaultRange.MaxDevestation * 100f);
            int destroyPercentile = Mod.Random.Next(minNum, maxNum);
            float destroyPercent = (float) destroyPercentile / 100f;
            int destroyedBuildings = (int)Math.Floor(shuffledBuildings.Count * destroyPercent);
            Mod.Log.Debug($"Destruction percentile: {destroyPercent} applied to {shuffledBuildings.Count} buildings = {destroyedBuildings} destroyed buildings.");

            for (int i = 0; i < destroyedBuildings; i++)
            {
                BattleTech.Building building = shuffledBuildings.ElementAt(i);
                Mod.Log.Debug($"Destroying building: {CombatantUtils.Label(building)}");

                building.FlagForDeath("CG_PREMAP_DESTROY", DeathMethod.DespawnedNoMessage, DamageType.NOT_SET, 1, -1, "0", true);
                building.HandleDeath("0");
                ModState.CandidateBuildings.Remove(building);
            }

        }


        // Brazenly stolen from https://stackoverflow.com/questions/273313/randomize-a-listt
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Mod.Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

    }
}
