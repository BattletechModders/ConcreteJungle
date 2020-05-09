﻿using BattleTech;
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
            // TODO: Allow spawn on ally as well
            if (__instance.team.IsLocalPlayer)
            {

                Mod.Log.Debug($"Tab targets available to: {CombatantUtils.Label(__instance)}");
                foreach (ICombatant combatant in __instance.Combat.GetAllTabTargets(__instance))
                {
                    Mod.Log.Debug($"  -- {CombatantUtils.Label(combatant)}");
                }
                
                // TODO: Random chance to spawn
                List<BattleTech.Building> candidates = new List<BattleTech.Building>();
                Mod.Log.Debug($"Comparing distance from actor: {CombatantUtils.Label(__instance)} at position: {__instance.CurrentPosition}");
                foreach (BattleTech.Building building in ModState.CandidateBuildings)
                {
                    if (!building.IsDead && (building.CurrentPosition - __instance.CurrentPosition).magnitude < 200.0f)
                    {
                        Mod.Log.Debug($" -- Building: {CombatantUtils.Label(building)} at position: {building.CurrentPosition} is within range.");
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

                    if (ModState.TrapBuildingsToTurrets.ContainsKey(trapBuilding.GUID)) return; // skip turret creation if this building already has one
                    if (ModState.TrapTurretToBuildingIds.Keys.Count >= Mod.Config.MaxAmbushTurrets) return; // skip turrets if we're already maxed out on the count


                    idx = ModState.CandidateTeams.Count > 0 ? Mod.Random.Next(0, ModState.CandidateTeams.Count - 1) : 0;
                    Team trapTeam = ModState.CandidateTeams.ElementAt<Team>(idx);
                    Mod.Log.Debug($" Spawning trap under team: {trapTeam}");

                    // Spawn a turret trap
                    // TODO: Create unique spawnPosition and rotation?
                    //Vector3 spawnPosition = trapBuilding.currentPosition;
                    //Quaternion quaternion = Quaternion.LookRotation(positionB - positionA);
                    AbstractActor trapTurret = SpawnHelper.SpawnTrapTurret(Mod.Config.TurretDef, Mod.Config.TurretPilotDef, trapTeam, trapBuilding);

                    trapTurret.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
                    //PilotableActorRepresentation par = trapTurret.GameRep as PilotableActorRepresentation;
                    //if (par == null) Mod.Log.Error("PAR IS NULL!");
                    //par.SetForcedPlayerVisibilityLevel(VisibilityLevel.Blip4Maximum, false);

                    Mod.Log.Debug("Updated turret visibility");
                    trapTurret.OnPositionUpdate(trapBuilding.CurrentPosition, trapBuilding.CurrentRotation, -1, true, null, false);
                    Mod.Log.Debug("Updated turret position.");

                    trapTurret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, trapTurret, BehaviorTreeIDEnum.CoreAITree);
                    Mod.Log.Debug("Updated turret behaviorTree");

                    UnitSpawnedMessage message = new UnitSpawnedMessage("CJ_TRAP", trapTurret.GUID);
                    ModState.Combat.MessageCenter.PublishMessage(message);
                    Mod.Log.Debug("Sent spawn message!");
                }
            }
            
        }
    }
}
