using BattleTech;
using ConcreteJungle.Helper;
using ConcreteJungle.Objects;
using Harmony;
using System.Collections.Generic;
using System.Linq;
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
            if (__instance.team.IsLocalPlayer)
            {

                //Mod.Log.Debug($"Tab targets available to: {CombatantUtils.Label(__instance)}");
                //foreach (ICombatant combatant in __instance.Combat.GetAllTabTargets(__instance))
                //{
                //    Mod.Log.Debug($"  -- {CombatantUtils.Label(combatant)}");
                //}

                // Check that we haven't exhausted the max traps for this mission
                if (ModState.TrapsSpawned >= Mod.Config.MaxSpawns) return;

                // Validate that we are far enough away from trap origins to spawn another
                foreach (Vector3 trapOrigin in ModState.TrapSpawnOrigins)
                {
                    float distance = (__instance.CurrentPosition - trapOrigin).magnitude;
                    if (distance < Mod.Config.MinSpawnDistance)
                    {
                        Mod.Log.Debug($" Actor {CombatantUtils.Label(__instance)} at pos: {__instance.CurrentPosition} is {distance}m away from " +
                            $"previous trap origin: {trapOrigin}. Skipping.");
                        return;
                    }
                }

                // Determine trap type - infantry ambush, tank ambush, IED
                // TODO: Randomize
                //TrapType trapType = TrapType.TRAP_INFANTRY_AMBUSH;

                //InfantryAmbushHelper.SpawnAmbush(__instance.CurrentPosition);
                ExplosionAmbushHelper.SpawnAmbush(__instance.CurrentPosition);
            }
            
        }
    }
}
