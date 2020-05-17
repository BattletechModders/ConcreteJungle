using BattleTech;
using Harmony;
using System;
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
            if (ModState.IsUrbanBiome && 
                __instance.team.IsLocalPlayer && 
                !__instance.Combat.TurnDirector.IsInterleaved && 
                !__instance.Combat.TurnDirector.IsInterleavePending)
            {
                // Check that we haven't exhausted the max traps for this mission
                if (ModState.Ambushes >= Mod.Config.Ambush.MaxPerMap) return;

                // Validate that we are far enough away from trap origins to spawn another
                foreach (Vector3 ambushOrigin in ModState.AmbushOrigins)
                {
                    float magnitude = (__instance.CurrentPosition - ambushOrigin).magnitude;
                    if (magnitude < Mod.Config.Ambush.MinDistanceBetween)
                    {
                        Mod.Log.Debug($" Actor {CombatantUtils.Label(__instance)} at pos: {__instance.CurrentPosition} is {magnitude}m away from " +
                            $"previous trap origin: {ambushOrigin}. Skipping.");
                        return;
                    }
                }

                // If we've made it this far, record it as a potential ambush site
                ModState.PotentialAmbushOrigins.Add(__instance.CurrentPosition);

            }
            
        }
    }

    // When buildings are destroyed, they check for actors standing on them and damage them with a fall. Prevent 
    //   this during our spawn ambush.
    [HarmonyPatch(typeof(AbstractActor), "OnActorDestroyed")]
    static class AbstractActor_OnActorDestroyed
    {
        static bool Prefix(AbstractActor __instance, MessageCenterMessage message)
        {
            if (ModState.CurrentSpawningLance != null && __instance != null)
            {
                ActorDestroyedMessage actorDestroyedMessage = message as ActorDestroyedMessage;
                if (actorDestroyedMessage != null &&
                    actorDestroyedMessage.HasAffectedObject &&
                    (actorDestroyedMessage.affectedObjectGuid == __instance.standingOnBuildingGuid || actorDestroyedMessage.affectedObjectGuid == __instance.standingOnBuildingGuid + ".Building") &&
                    __instance.LanceId == ModState.CurrentSpawningLance.GUID
                    )
                {
                    Mod.Log.Debug("Intercepted displacement message for ambush spawns.");

                    // Replay displacement w/o effects 
                    Vector3 vector = __instance.CurrentPosition;
                    vector.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(__instance.CurrentPosition, false);
                    
                    if (__instance.GameRep != null)
                    {
                        __instance.GameRep.transform.position = vector;
                    }

                    __instance.OnPositionUpdate(vector, __instance.CurrentRotation, -1, true, null, false);

                    return false;
                }
            }

            return true;
        }
    }
}
