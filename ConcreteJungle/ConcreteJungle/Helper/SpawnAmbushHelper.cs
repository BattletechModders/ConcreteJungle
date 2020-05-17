using BattleTech;
using ConcreteJungle.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class SpawnAmbushHelper
    {
        public static void SpawnAmbush(Vector3 ambushOrigin, AmbushType ambushType)
        {
            if (ambushType == AmbushType.Mech && !Mod.Config.MechAmbush.Enabled) return;
            if (ambushType == AmbushType.Vehicle && !Mod.Config.VehicleAmbush.Enabled) return;

            // Determine how many units we're spawning
            int minSpawns = ambushType == AmbushType.Mech ? ModState.MechAmbushDefForContract.MinSpawns : ModState.VehicleAmbushDefForContract.MinSpawns;
            int maxSpawns = ambushType == AmbushType.Mech ? ModState.MechAmbushDefForContract.MaxSpawns : ModState.VehicleAmbushDefForContract.MaxSpawns;

            int actorsToSpawn = Mod.Random.Next(minSpawns, maxSpawns);
            Mod.Log.Debug($"Spawning {actorsToSpawn} actors as part of this ambush.");

            // Create a new lance in the target team
            Lance ambushLance = TeamHelper.CreateAmbushLance(ModState.TargetAllyTeam);

            // Starting with the closest building, look through the buildings and determine how many locations can support a unit.
            List<BattleTech.Building> candidates = CandidateBuildingsHelper.ClosestCandidatesToPosition(ambushOrigin, Mod.Config.Ambush.SearchRadius);

            EncounterLayerData encounterLayerData = ModState.Combat.EncounterLayerData;
            List<BattleTech.Building> buildingsToLevel = new List<BattleTech.Building>();
            List<AbstractActor> spawnedActors = new List<AbstractActor>();
            foreach(BattleTech.Building building in candidates)
            {
                if (actorsToSpawn == 0) break; // nothing more to do, end processing.

                // Spawn one unit at the origin of the building
                buildingsToLevel.Add(building);
                Mod.Log.Debug("Spawning actor at building origin.");
                if (ambushType == AmbushType.Mech)
                {
                    SpawnAmbushMech(ModState.TargetAllyTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation);
                }
                else
                {
                    SpawnAmbushVehicle(ModState.TargetAllyTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation);
                }
                actorsToSpawn--;

                // Iterate through adjacent hexes to see if we can spawn more units in the building
                List<Vector3> adjacentHexes = ModState.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(ambushOrigin, 3); // 3 hexes should cover most large buidlings
                foreach (Vector3 adjacentPos in adjacentHexes)
                {
                    if (actorsToSpawn == 0) break;

                    Point cellPoint = new Point(ModState.Combat.MapMetaData.GetXIndex(adjacentPos.x), ModState.Combat.MapMetaData.GetZIndex(adjacentPos.z));
                    if (encounterLayerData.mapEncounterLayerDataCells[cellPoint.Z, cellPoint.X].HasSpecifiedBuilding(building.GUID))
                    {
                        Mod.Log.Debug($"Spawning actor at adjacent hex at position: {adjacentPos}");
                        if (ambushType == AmbushType.Mech)
                        {
                            SpawnAmbushMech(ModState.TargetAllyTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation);
                        }
                        else
                        {
                            SpawnAmbushVehicle(ModState.TargetAllyTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation);
                        }
                        actorsToSpawn--;
                    }
                }
            }

            // Remove any buildings that are part of this ambush from candidates
            ModState.CandidateBuildings.RemoveAll(x => buildingsToLevel.Contains(x));

            // Determine the targets that should be prioritized by the enemies
            List<ICombatant> targets = new List<ICombatant>();
            foreach (ICombatant combatant in ModState.Combat.GetAllCombatants())
            {
                if (!combatant.IsDead && !combatant.IsFlaggedForDeath && 
                    combatant.team != null &&
                    ModState.Combat.HostilityMatrix.IsLocalPlayerFriendly(combatant.team))
                {
                    if (Vector3.Distance(ambushOrigin, combatant.CurrentPosition) <= Mod.Config.Ambush.SearchRadius)
                    {
                        targets.Add(combatant);
                    }
                }
            }

            Mod.Log.Info($"Adding SpawnAmbushSequence for {spawnedActors.Count} actors and {buildingsToLevel.Count} buildings to be leveled.");
            try
            {
                SpawnAmbushSequence ambushSequence = new SpawnAmbushSequence(ModState.Combat, ambushOrigin, spawnedActors, buildingsToLevel, targets);
                ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));
            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to create AES sequence due to error!", e);
            }
        }

        public static AbstractActor SpawnAmbushVehicle(Team team, Lance ambushLance, Vector3 ambushOrigin, Vector3 spawnPos, Quaternion spawnRot)
        {

            // Randomly determine one of the spawnpairs from the current ambushdef
            List<VehicleAndPilotDef> shuffledSpawns = new List<VehicleAndPilotDef>();
            shuffledSpawns.AddRange(ModState.VehicleAmbushDefForContract.SpawnPool);
            shuffledSpawns.Shuffle();

            VehicleAndPilotDef ambushDef = shuffledSpawns[0];

            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.Get(ambushDef.PilotDefId);
            VehicleDef vehicleDef = ModState.Combat.DataManager.VehicleDefs.Get(ambushDef.VehicleDefId);
            vehicleDef.Refresh();

            // Adjust position so we don't spawn in the ground.
            spawnPos.y = ModState.Combat.MapMetaData.GetLerpedHeightAt(spawnPos, true);

            // Rotate to face the ambush origin
            Vector3 spawnDirection = Vector3.RotateTowards(spawnRot.eulerAngles, ambushOrigin, 1f, 0f);
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection);

            Vehicle vehicle = ActorFactory.CreateVehicle(vehicleDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            vehicle.Init(spawnPos, spawnRotation.eulerAngles.y, true);
            vehicle.InitGameRep(null);
            Mod.Log.Debug($"Spawned vehicle {CombatantUtils.Label(vehicle)} at position: {spawnPos}");

            if (vehicle == null) Mod.Log.Error($"Failed to spawn vehicleDefId: {ambushDef.VehicleDefId} / pilotDefId: {ambushDef.PilotDefId} !");

            Mod.Log.Debug($" Spawned ambush vehicle, adding to team: {team} and lance: {ambushLance}");
            team.AddUnit(vehicle);
            vehicle.AddToLance(ambushLance);

            vehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, vehicle, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug("Enabled vehicle behavior tree");

            return vehicle;
        }

        public static AbstractActor SpawnAmbushMech(Team team, Lance ambushLance, Vector3 ambushOrigin, Vector3 spawnPos, Quaternion spawnRot)
        {

            // Randomly determine one of the spawnpairs from the current ambushdef
            List<MechAndPilotDef> shuffledSpawns = new List<MechAndPilotDef>();
            shuffledSpawns.AddRange(ModState.MechAmbushDefForContract.SpawnPool);
            shuffledSpawns.Shuffle();

            MechAndPilotDef ambushDef = shuffledSpawns[0];

            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.Get(ambushDef.PilotDefId);
            MechDef mechDef = ModState.Combat.DataManager.MechDefs.Get(ambushDef.MechDefId);
            mechDef.Refresh();

            // Adjust position so we don't spawn in the ground.
            spawnPos.y = ModState.Combat.MapMetaData.GetLerpedHeightAt(spawnPos, true);

            // Rotate to face the ambush origin
            Vector3 spawnDirection = Vector3.RotateTowards(spawnRot.eulerAngles, ambushOrigin, 1f, 0f);
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection);

            Mech mech = ActorFactory.CreateMech(mechDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            mech.Init(spawnPos, spawnRotation.eulerAngles.y, true);
            mech.InitGameRep(null);
            Mod.Log.Debug($"Spawned mech {CombatantUtils.Label(mech)} at position: {spawnPos}");

            if (mech == null) Mod.Log.Error($"Failed to spawn mechDefId: {ambushDef.MechDefId} / pilotDefId: {ambushDef.PilotDefId} !");

            Mod.Log.Debug($" Spawned ambush mech, adding to team: {team} and lance: {ambushLance}");
            team.AddUnit(mech);
            mech.AddToLance(ambushLance);

            mech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, mech, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug("Enabled mech behavior tree");

            return mech;
        }
    }
}
