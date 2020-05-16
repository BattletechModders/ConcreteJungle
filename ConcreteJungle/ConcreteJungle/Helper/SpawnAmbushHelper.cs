using BattleTech;
using ConcreteJungle.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;
using static ConcreteJungle.ModConfig;

namespace ConcreteJungle.Helper
{
    public static class SpawnAmbushHelper
    {
        public static void SpawnAmbush(Vector3 originPos)
        {
            if (!Mod.Config.SpawnAmbush.Enabled) return;

            //Build list of candidate trap buildings
            List<BattleTech.Building> candidates = CandidateBuildingsHelper.FilterCandidates(originPos, Mod.Config.InfantryAmbush.SearchRadius);

            // Only spawn if there are sufficient buildings.
            if (candidates.Count < Mod.Config.SpawnAmbush.AmbushLance.Count)
            {
                Mod.Log.Debug($"Insufficient candidate buildings to for a spawn ambush. Need {Mod.Config.SpawnAmbush.AmbushLance.Count} buildings. Skipping.");
                return;
            }

            int teamIdx = ModState.CandidateTeams.Count > 0 ? Mod.Random.Next(0, ModState.CandidateTeams.Count - 1) : 0;
            Team trapTeam = ModState.CandidateTeams.ElementAt<Team>(teamIdx);

            BattleTech.Building[] spawnPoints = candidates.Take(Mod.Config.SpawnAmbush.AmbushLance.Count).ToArray();
            Dictionary<AbstractActor, BattleTech.Building> actorToSpawnBuildings = new Dictionary<AbstractActor, BattleTech.Building>();
            for (int i = 0; i < Mod.Config.SpawnAmbush.AmbushLance.Count; i++)
            {
                AmbushDef lance = Mod.Config.SpawnAmbush.AmbushLance[i];
                BattleTech.Building originBuilding = spawnPoints[i];
                AbstractActor spawnedActor = null;
                if (lance.IsVehicle)
                {
                    spawnedActor = SpawnAmbushVehicle(lance, trapTeam, originPos, originBuilding);
                }
                actorToSpawnBuildings.Add(spawnedActor, originBuilding);
            }

            // TODO: This should be 
            List<ICombatant> targets = new List<ICombatant>();
            foreach (ICombatant combatant in ModState.Combat.GetAllCombatants())
            {
                if (!combatant.IsDead && !combatant.IsFlaggedForDeath && !(combatant is BattleTech.Building))
                {
                    if (combatant.team != null && ModState.Combat.HostilityMatrix.IsLocalPlayerFriendly(combatant.team) &&
                        Vector3.Distance(originPos, combatant.CurrentPosition) <= Mod.Config.SpawnAmbush.SearchRadius)
                    {
                        Mod.Log.Debug($"Adding potential target: {CombatantUtils.Label(combatant)}");
                        targets.Add(combatant);
                    }                    
                }
            }

            Mod.Log.Debug("Sending AddSequence message for spawn ambush explosion.");
            try
            {
                SpawnAmbushSequence ambushSequence = new SpawnAmbushSequence(ModState.Combat, originPos, actorToSpawnBuildings, targets);
                ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));

            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to create AES sequence due to error!", e);
            }


            ModState.AmbushOrigins.Add(originPos);
        }

        public static AbstractActor SpawnAmbushVehicle(AmbushDef ambushLance, Team team, Vector3 ambushOrigin, BattleTech.Building originBuilding)
        {
            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.GetOrCreate(ambushLance.PilotDefId);
            VehicleDef vehicleDef = ModState.Combat.DataManager.VehicleDefs.GetOrCreate(ambushLance.VehicleDefId);
            vehicleDef.Refresh();

            Vector3 spawnPos = originBuilding.CurrentPosition;
            spawnPos.y = ModState.Combat.MapMetaData.GetLerpedHeightAt(spawnPos, true);

            Vector3 spawnDirection = Vector3.RotateTowards(originBuilding.CurrentRotation.eulerAngles, ambushOrigin, 1f, 0f);
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection);

            Vehicle vehicle = ActorFactory.CreateVehicle(vehicleDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            vehicle.Init(spawnPos, spawnRotation.eulerAngles.y, true);
            vehicle.InitGameRep(null);
            Mod.Log.Debug($"Spawned vehicle {CombatantUtils.Label(vehicle)} at position: {spawnPos}");

            if (vehicle == null) Mod.Log.Error($"Failed to spawn vehicle: {ambushLance.VehicleDefId} + pilotDefId: {ambushLance.PilotDefId} !");

            Lance lance = team.lances.First<Lance>();
            Mod.Log.Debug($" Spawned ambush vehicle, adding to team: {team} and lance: {lance}");
            team.AddUnit(vehicle);
            vehicle.AddToLance(lance);

            vehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, vehicle, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug("Enabled vehicle behavior tree");

            return vehicle;
        }
    }
}
