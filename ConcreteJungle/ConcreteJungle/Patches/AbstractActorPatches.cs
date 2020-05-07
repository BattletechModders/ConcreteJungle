using BattleTech;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(AbstractActor), "OnMovePhaseComplete")]
    static class AbstractActor_OnMovePhaseComplete
    {
        static void Postfix(AbstractActor __instance)
        {
            List<BattleTech.Building> candidates = new List<BattleTech.Building>();
            Mod.Log.Debug($"Comparing distance from actor: {CombatantUtils.Label(__instance)} at position: {__instance.CurrentPosition}");
            foreach (BattleTech.Building building in ModState.TargetableBuildings)
            {                
                if (!building.IsDead && (building.CurrentPosition - __instance.CurrentPosition).magnitude < 200.0f)
                {
                    Mod.Log.Debug($" -- Building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is a candidate.");
                    candidates.Add(building);
                }
                else
                {
                    Mod.Log.Trace($" -- Building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is more than 200 meters away.");
                }
            }

            if (candidates.Count > 0)
            {
                int idx = Mod.Random.Next(0, candidates.Count - 1);
                BattleTech.Building trapBuilding = candidates.ElementAt(idx);
                Mod.Log.Debug($" -- using building: {CombatantUtils.Label(trapBuilding)} as trap.");
                float surfaceArea = trapBuilding.DestructibleObjectGroup.footprint.x * trapBuilding.DestructibleObjectGroup.footprint.y;
                Mod.Log.Debug($" -- building has dimensions: {surfaceArea}");
            }
        }
    }
}
