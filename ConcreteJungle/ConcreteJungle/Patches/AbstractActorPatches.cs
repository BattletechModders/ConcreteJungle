using BattleTech;
using Harmony;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Patches
{
    [HarmonyPatch(typeof(AbstractActor), "OnMovePhaseComplete")]
    static class AbstractActor_OnMovePhaseComplete
    {
        static void Postfix(AbstractActor __instance)
        {
            // TODO: Allow spawn on ally as well
            if (__instance.team.IsLocalPlayer && 
                !__instance.Combat.TurnDirector.IsInterleaved && 
                !__instance.Combat.TurnDirector.IsInterleavePending)
            {

                // We have a pending ambush, skip
                if (ModState.PendingAmbushOrigin.magnitude != 0) return;

                // Check that we haven't exhausted the max traps for this mission
                if (ModState.TrapsSpawned >= Mod.Config.MaxAbushesPerMap) return;

                // Validate that we are far enough away from trap origins to spawn another
                foreach (Vector3 trapOrigin in ModState.TrapSpawnOrigins)
                {
                    float distance = (__instance.CurrentPosition - trapOrigin).magnitude;
                    if (distance < Mod.Config.MinDistanceBetweenAmbushes)
                    {
                        Mod.Log.Debug($" Actor {CombatantUtils.Label(__instance)} at pos: {__instance.CurrentPosition} is {distance}m away from " +
                            $"previous trap origin: {trapOrigin}. Skipping.");
                        return;
                    }
                }

                // If everything passes, mark this as a potential ambush location
                ModState.PendingAmbushOrigin = __instance.CurrentPosition;

            }
            
        }
    }
}
