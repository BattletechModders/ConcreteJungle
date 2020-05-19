using BattleTech;
using ConcreteJungle.Helper;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    static class CombatGameState__Init
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:_I - entered.");

            // Re-initialize everything to give us a clean slate.
            ModState.Reset();

            ModState.Combat = __instance;

        }

        
    }

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    static class CombatGameState_OnCombatGameDestroyed
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace("CGS:OCGD - entered.");
            
            // Remove any trap turrets for salvage

            try
            {
                DataLoadHelper.UnloadAmbushResources(__instance);
            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to unload ambush resources due to exception!", e);
                ModState.IsUrbanBiome = false;
            }

            ModState.Reset();
        }
    }

    // Remove any trap turrets as possible tab targets
    //static class CombatGameState_GetAllTabTargets
    //{
    //    static void Postfix(CombatGameState __instance, List<ICombatant> __result)
    //    {
    //        if (__result != null && __result.Count > 0)
    //        {
    //            __result.RemoveAll(x => ModState.TrapTurretToBuildingIds.ContainsKey(x.GUID));
    //        }
    //    }
    //}


}
