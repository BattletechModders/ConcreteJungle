using ConcreteJungle.Helper;
using System;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    static class CombatGameState__Init
    {
        static void Postfix(CombatGameState __instance)
        {
            Mod.Log.Trace?.Write("CGS:_I - entered.");

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
            Mod.Log.Trace?.Write("CGS:OCGD - entered.");

            // Remove any trap turrets for salvage

            try
            {
                DataLoadHelper.UnloadAmbushResources(__instance);
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Failed to unload ambush resources due to exception!");
                ModState.ProcessAmbushes = false;
            }

            ModState.Reset();
        }
    }

}
