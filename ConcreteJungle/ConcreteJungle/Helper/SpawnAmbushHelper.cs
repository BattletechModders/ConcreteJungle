using ConcreteJungle.Sequence;
using System;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class SpawnAmbushHelper
    {
        public static void SpawnAmbush(Vector3 ambushOrigin, AmbushType ambushType)
        {
            // Determine how many units we're spawning
            int minSpawns = 0, maxSpawns = 0;
            if (ambushType == AmbushType.BattleArmor)
            {
                if (!Mod.Config.BattleArmorAmbush.Enabled) return;
                minSpawns = ModState.BattleArmorAmbushDefForContract.MinSpawns;
                maxSpawns = ModState.BattleArmorAmbushDefForContract.MaxSpawns;
            }
            else if (ambushType == AmbushType.Mech)
            {
                if (!Mod.Config.MechAmbush.Enabled) return;
                minSpawns = ModState.MechAmbushDefForContract.MinSpawns;
                maxSpawns = ModState.MechAmbushDefForContract.MaxSpawns;
            }
            else if (ambushType == AmbushType.Vehicle)
            {
                if (!Mod.Config.VehicleAmbush.Enabled) return;
                minSpawns = ModState.VehicleAmbushDefForContract.MinSpawns;
                maxSpawns = ModState.VehicleAmbushDefForContract.MaxSpawns;
            }

            int actorsToSpawn = Mod.Random.Next(minSpawns, maxSpawns);
            Mod.Log.Debug?.Write($"Spawning {actorsToSpawn} actors as part of this ambush.");

            // Starting with the closest building, look through the buildings and determine how many locations can support a unit.
            List<BattleTech.Building> candidates = CandidateBuildingsHelper.ClosestCandidatesToPosition(ambushOrigin, Mod.Config.Ambush.SearchRadius);
            if (candidates.Count < minSpawns)
            {
                Mod.Log.Debug?.Write($"Insufficient candidate buildings for a spawn ambush. Skipping.");
                return;
            }

            // Create a new lance in the target team
            Lance ambushLance = TeamHelper.CreateAmbushLance(ModState.AmbushTeam);
            ModState.CurrentSpawningLance = ambushLance;

            EncounterLayerData encounterLayerData = ModState.Combat.EncounterLayerData;
            List<BattleTech.Building> buildingsToLevel = new List<BattleTech.Building>();
            List<AbstractActor> spawnedActors = new List<AbstractActor>();
            foreach (BattleTech.Building building in candidates)
            {
                if (actorsToSpawn == 0) break; // nothing more to do, end processing.

                // Spawn one unit at the origin of the building
                buildingsToLevel.Add(building);
                Mod.Log.Debug?.Write($"Spawning actor at building: {CombatantUtils.Label(building)} with position: {building.CurrentPosition}");

                AbstractActor spawnedActor = null;
                if (ambushType == AmbushType.BattleArmor)
                    spawnedActor = SpawnAmbushMech(ModState.AmbushTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation, ModState.BattleArmorAmbushDefForContract.SpawnPool);
                else if (ambushType == AmbushType.Mech)
                    spawnedActor = SpawnAmbushMech(ModState.AmbushTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation, ModState.MechAmbushDefForContract.SpawnPool);
                else if (ambushType == AmbushType.Vehicle)
                    spawnedActor = SpawnAmbushVehicle(ModState.AmbushTeam, ambushLance, ambushOrigin, building.CurrentPosition, building.CurrentRotation);

                spawnedActors.Add(spawnedActor);
                actorsToSpawn--;

                // Iterate through adjacent hexes to see if we can spawn more units in the building
                List<Vector3> adjacentHexes = ModState.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(ambushOrigin, 3); // 3 hexes should cover most large buidlings
                foreach (Vector3 adjacentHex in adjacentHexes)
                {
                    if (actorsToSpawn == 0) break;

                    Point cellPoint = new Point(ModState.Combat.MapMetaData.GetXIndex(adjacentHex.x), ModState.Combat.MapMetaData.GetZIndex(adjacentHex.z));
                    Mod.Log.Debug?.Write($" Evaluating point {cellPoint.X}, {cellPoint.Z} for potential ambush.");
                    if (encounterLayerData.mapEncounterLayerDataCells[cellPoint.Z, cellPoint.X].HasSpecifiedBuilding(building.GUID))
                    {
                        Mod.Log.Debug?.Write($"Spawning actor at adjacent hex at position: {adjacentHex}");
                        AbstractActor additionalSpawn = null;
                        if (ambushType == AmbushType.BattleArmor)
                            additionalSpawn = SpawnAmbushMech(ModState.AmbushTeam, ambushLance, ambushOrigin, adjacentHex, building.CurrentRotation, ModState.BattleArmorAmbushDefForContract.SpawnPool);
                        else if (ambushType == AmbushType.Mech)
                            additionalSpawn = SpawnAmbushMech(ModState.AmbushTeam, ambushLance, ambushOrigin, adjacentHex, building.CurrentRotation, ModState.MechAmbushDefForContract.SpawnPool);
                        else if (ambushType == AmbushType.Vehicle)
                            additionalSpawn = SpawnAmbushVehicle(ModState.AmbushTeam, ambushLance, ambushOrigin, adjacentHex, building.CurrentRotation);

                        spawnedActors.Add(additionalSpawn);
                        actorsToSpawn--;
                    }
                    else
                    {
                        Mod.Log.Debug?.Write($" Hex {adjacentHex} is outside of the main building, skipping.");
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

            bool applyAttacks = false;
            if (ambushType == AmbushType.BattleArmor && Mod.Config.BattleArmorAmbush.FreeAttackEnabled) applyAttacks = true;
            if (ambushType == AmbushType.Mech && Mod.Config.MechAmbush.FreeAttackEnabled) applyAttacks = true;
            if (ambushType == AmbushType.Vehicle && Mod.Config.VehicleAmbush.FreeAttackEnabled) applyAttacks = true;

            Mod.Log.Info?.Write($"Adding SpawnAmbushSequence for {spawnedActors.Count} actors and {buildingsToLevel.Count} buildings to be leveled.");
            try
            {
                SpawnAmbushSequence ambushSequence = new SpawnAmbushSequence(ModState.Combat, ambushOrigin, spawnedActors, buildingsToLevel, targets, applyAttacks);
                ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Failed to create AES sequence due to error!");
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
            spawnPos.y = CalculateSpawnHeight(spawnPos, true);

            // Rotate to face the ambush origin
            Vector3 spawnDirection = Vector3.RotateTowards(spawnRot.eulerAngles, ambushOrigin, 1f, 0f);
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection);

            Vehicle vehicle = ActorFactory.CreateVehicle(vehicleDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            vehicle.Init(spawnPos, spawnRotation.eulerAngles.y, true);
            vehicle.InitGameRep(null);
            Mod.Log.Debug?.Write($"Spawned vehicle {CombatantUtils.Label(vehicle)} at position: {spawnPos}");

            if (vehicle == null) Mod.Log.Error?.Write($"Failed to spawn vehicleDefId: {ambushDef.VehicleDefId} / pilotDefId: {ambushDef.PilotDefId} !");

            Mod.Log.Debug?.Write($" Spawned ambush vehicle, adding to team: {team} and lance: {ambushLance}");
            team.AddUnit(vehicle);
            vehicle.AddToTeam(team);
            vehicle.AddToLance(ambushLance);

            vehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, vehicle, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug?.Write("Enabled vehicle behavior tree");

            UnitSpawnedMessage message = new UnitSpawnedMessage("CJ_VEHICLE", vehicle.GUID);
            ModState.Combat.MessageCenter.PublishMessage(message);

            // Attempting to force an update to fix underground issues
            vehicle.OnPositionUpdate(spawnPos, spawnRotation, -1, true, null, false);

            return vehicle;
        }

        public static AbstractActor SpawnAmbushMech(Team team, Lance ambushLance, Vector3 ambushOrigin, Vector3 spawnPos, Quaternion spawnRot, List<MechAndPilotDef> spawnPool)
        {

            // Randomly determine one of the spawnpairs from the current ambushdef
            List<MechAndPilotDef> shuffledSpawns = new List<MechAndPilotDef>();
            shuffledSpawns.AddRange(spawnPool);
            shuffledSpawns.Shuffle();

            MechAndPilotDef ambushDef = shuffledSpawns[0];

            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.Get(ambushDef.PilotDefId);
            MechDef mechDef = ModState.Combat.DataManager.MechDefs.Get(ambushDef.MechDefId);
            mechDef.Refresh();

            // Adjust position so we don't spawn in the ground.
            spawnPos.y = CalculateSpawnHeight(spawnPos, true);

            // Rotate to face the ambush origin
            Vector3 spawnDirection = Vector3.RotateTowards(spawnRot.eulerAngles, ambushOrigin, 1f, 0f);
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection);

            Mech mech = ActorFactory.CreateMech(mechDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            mech.Init(spawnPos, spawnRotation.eulerAngles.y, true);
            mech.InitGameRep(null);
            Mod.Log.Debug?.Write($"Spawned mech {CombatantUtils.Label(mech)} at position: {spawnPos}");

            if (mech == null) Mod.Log.Error?.Write($"Failed to spawn mechDefId: {ambushDef.MechDefId} / pilotDefId: {ambushDef.PilotDefId} !");

            Mod.Log.Debug?.Write($" Spawned ambush mech, adding to team: {team} and lance: {ambushLance}");
            team.AddUnit(mech);
            mech.AddToTeam(team);
            mech.AddToLance(ambushLance);

            mech.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, mech, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug?.Write("Enabled mech behavior tree");

            UnitSpawnedMessage message = new UnitSpawnedMessage("CJ_MECH", mech.GUID);
            ModState.Combat.MessageCenter.PublishMessage(message);

            // Attempting to force an update to fix underground issues
            mech.OnPositionUpdate(spawnPos, spawnRotation, -1, true, null, false);

            return mech;
        }

        private static float CalculateSpawnHeight(Vector3 worldPos, bool terrainOnly = false)
        {
            // Following the same logic as MapMetaData.GetLerpedHeightAt
            Vector3 vector = new Vector3((float)MapMetaDataExporter.cellSize * 0.5f, 0f, (float)MapMetaDataExporter.cellSize * 0.5f);
            Point index = ModState.Combat.MapMetaData.GetIndex(worldPos - vector);
            
            // Clamp indices to valid bounds (same as GetLerpedHeightAt)
            if (index.Z < 0)
            {
                index.Z = 0;
            }
            else if (index.Z >= ModState.Combat.MapMetaData.mapTerrainDataCells.GetLength(0) - 1)
            {
                index.Z = ModState.Combat.MapMetaData.mapTerrainDataCells.GetLength(0) - 2;
            }
            if (index.X < 0)
            {
                index.X = 0;
            }
            else if (index.X >= ModState.Combat.MapMetaData.mapTerrainDataCells.GetLength(1) - 1)
            {
                index.X = ModState.Combat.MapMetaData.mapTerrainDataCells.GetLength(1) - 2;
            }

            // Get the four corner cells used for interpolation
            MapTerrainDataCell mapTerrainDataCell = ModState.Combat.MapMetaData.mapTerrainDataCells[index.Z, index.X];
            MapTerrainDataCell mapTerrainDataCell2 = ModState.Combat.MapMetaData.mapTerrainDataCells[index.Z, index.X + 1];
            MapTerrainDataCell mapTerrainDataCell3 = ModState.Combat.MapMetaData.mapTerrainDataCells[index.Z + 1, index.X];
            MapTerrainDataCell mapTerrainDataCell4 = ModState.Combat.MapMetaData.mapTerrainDataCells[index.Z + 1, index.X + 1];

            // Log terrain heights for each cell
            Mod.Log.Debug?.Write($"Height calculation for position {worldPos}:");
            Mod.Log.Debug?.Write($"  Cell [{index.Z},{index.X}] - terrainHeight: {mapTerrainDataCell.terrainHeight:F2}, cachedHeight: {mapTerrainDataCell.cachedHeight:F2}");
            Mod.Log.Debug?.Write($"  Cell [{index.Z},{index.X + 1}] - terrainHeight: {mapTerrainDataCell2.terrainHeight:F2}, cachedHeight: {mapTerrainDataCell2.cachedHeight:F2}");
            Mod.Log.Debug?.Write($"  Cell [{index.Z + 1},{index.X}] - terrainHeight: {mapTerrainDataCell3.terrainHeight:F2}, cachedHeight: {mapTerrainDataCell3.cachedHeight:F2}");
            Mod.Log.Debug?.Write($"  Cell [{index.Z + 1},{index.X + 1}] - terrainHeight: {mapTerrainDataCell4.terrainHeight:F2}, cachedHeight: {mapTerrainDataCell4.cachedHeight:F2}");
            Mod.Log.Debug?.Write($"  terrainOnly parameter: {terrainOnly}");

            // Calculate interpolation factors (same as GetLerpedHeightAt)
            Vector3 worldPos2 = ModState.Combat.MapMetaData.getWorldPos(index);
            float t = (worldPos.x - worldPos2.x) / (float)MapMetaDataExporter.cellSize;
            float t2 = (worldPos.z - worldPos2.z) / (float)MapMetaDataExporter.cellSize;

            // Perform bilinear interpolation (same as GetLerpedHeightAt)
            float a = Mathf.Lerp(terrainOnly ? mapTerrainDataCell.terrainHeight : mapTerrainDataCell.cachedHeight, 
                                terrainOnly ? mapTerrainDataCell2.terrainHeight : mapTerrainDataCell2.cachedHeight, t);
            float b = Mathf.Lerp(terrainOnly ? mapTerrainDataCell3.terrainHeight : mapTerrainDataCell3.cachedHeight, 
                                terrainOnly ? mapTerrainDataCell4.terrainHeight : mapTerrainDataCell4.cachedHeight, t);
            float finalHeight = Mathf.Lerp(a, b, t2);

            Mod.Log.Debug?.Write($"  Interpolation factors - t: {t:F3}, t2: {t2:F3}");
            Mod.Log.Debug?.Write($"  Intermediate values - a: {a:F2}, b: {b:F2}");
            Mod.Log.Debug?.Write($"  Final calculated height: {finalHeight:F2}");

            return finalHeight;
        }
    }
}
