using ConcreteJungle.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConcreteJungle.Helper
{
    public static class InfantryAmbushHelper
    {
        public static void SpawnAmbush(Vector3 ambushOrigin)
        {
            if (!Mod.Config.InfantryAmbush.Enabled) return;

            int infantrySpawns = Mod.Random.Next(ModState.InfantryAmbushDefForContract.MinSpawns, ModState.InfantryAmbushDefForContract.MaxSpawns);
            Mod.Log.Debug?.Write($"Spawning up to {infantrySpawns} infantry spawns as part of this ambush.");

            // Create a new lance in the target team
            Lance ambushLance = TeamHelper.CreateAmbushLance(ModState.AmbushTeam);

            // Build list of candidate trap buildings
            List<BattleTech.Building> candidates = CandidateBuildingsHelper.ClosestCandidatesToPosition(ambushOrigin, Mod.Config.Ambush.SearchRadius);
            if (candidates.Count < ModState.InfantryAmbushDefForContract.MinSpawns)
            {
                Mod.Log.Debug?.Write($"Insufficient candidate buildings to spawn an infantry ambush. Skipping.");
                return;
            }

            // Make sure we don't spawn more turrets than buildings
            if (infantrySpawns > candidates.Count) infantrySpawns = candidates.Count;

            List<AbstractActor> spawnedActors = new List<AbstractActor>();
            List<BattleTech.Building> spawnBuildings = new List<BattleTech.Building>();
            for (int i = 0; i < infantrySpawns; i++)
            {
                BattleTech.Building spawnBuildingShell = candidates.ElementAt(i);

                // Spawn a turret trap
                AbstractActor ambushTurret = SpawnAmbushTurret(ModState.AmbushTeam, ambushLance, spawnBuildingShell, ambushOrigin);
                if (ambushTurret == null) continue;

                spawnedActors.Add(ambushTurret);
                spawnBuildings.Add(spawnBuildingShell);
                Mod.Log.Info?.Write($"Spawned turret: {ambushTurret.DisplayName} in building: {spawnBuildingShell.DisplayName}");
            }

            // Remove any buildings that are part of this ambush from candidates
            ModState.CandidateBuildings.RemoveAll(x => spawnBuildings.Contains(x));

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

            Mod.Log.Info?.Write($"Adding InfantryAmbushSequence for {spawnedActors.Count} actors.");
            try
            {
                InfantryAmbushSequence ambushSequence =
                    new InfantryAmbushSequence(ModState.Combat, ambushOrigin, spawnedActors, spawnBuildings, targets, Mod.Config.InfantryAmbush.FreeAttackEnabled);
                ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Failed to create AES sequence due to error!");
            }
        }

        public static AbstractActor SpawnAmbushTurret(Team team, Lance ambushLance, BattleTech.Building building, Vector3 ambushOrigin)
        {

            // Randomly determine one of the spawnpairs from the current ambushdef
            List<TurretAndPilotDef> shuffledSpawns = new List<TurretAndPilotDef>();
            shuffledSpawns.AddRange(ModState.InfantryAmbushDefForContract.SpawnPool);
            if (shuffledSpawns.Count < 1)
            {
                Mod.Log.Warn?.Write($"Spawn counts were less than 1, aborting spawn!");
                return null;
            }

            shuffledSpawns.Shuffle();
            Mod.Log.Info?.Write($"Shuffling {shuffledSpawns.Count} defs for this contract.");
            TurretAndPilotDef ambushDef = shuffledSpawns[0];

            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.Get(ambushDef.PilotDefId);
            TurretDef turretDef = ModState.Combat.DataManager.TurretDefs.GetOrCreate(ambushDef.TurretDefId);
            Mod.Log.Info?.Write($"Loading TuretDef with pilotDef: {pilotDef} turretDef: {turretDef}");
            turretDef.Refresh();

            // determine a position somewhere up the building's axis
            EncounterLayerData encounterLayerData = ModState.Combat.EncounterLayerData;
            Point cellPoint = new Point(
                ModState.Combat.MapMetaData.GetXIndex(building.CurrentPosition.x),
                ModState.Combat.MapMetaData.GetZIndex(building.CurrentPosition.z));
            MapEncounterLayerDataCell melDataCell =
                encounterLayerData.mapEncounterLayerDataCells[cellPoint.Z, cellPoint.X];
            float buildingHeight = melDataCell.GetBuildingHeight();

            float terrainHeight = ModState.Combat.MapMetaData.GetLerpedHeightAt(building.CurrentPosition, true);
            float heightDelta = (buildingHeight - terrainHeight) * 0.7f;
            float adjustedY = terrainHeight + heightDelta;
            Mod.Log.Debug?.Write($"At building position, terrain height is: {terrainHeight} while buildingHeight is: {buildingHeight}. " +
                $" Calculated 70% of building height + terrain as {adjustedY}.");

            Vector3 newPosition = building.GameRep.transform.position;
            newPosition.y = adjustedY;
            Mod.Log.Debug?.Write($"Changing transform position from: {building.GameRep.transform.position} to {newPosition}");

            /// Rotate to face the ambush origin
            Vector3 spawnDirection = Vector3.RotateTowards(building.CurrentRotation.eulerAngles, ambushOrigin, 1f, 0f);
            Quaternion spawnRotation = Quaternion.LookRotation(spawnDirection);

            // Create the turret
            Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            turret.Init(newPosition, spawnRotation.eulerAngles.y, true);
            turret.InitGameRep(null);

            if (turret == null) Mod.Log.Error?.Write($"Failed to spawn turretDefId: {ambushDef.TurretDefId} + pilotDefId: {ambushDef.PilotDefId} !");

            Mod.Log.Debug?.Write($" Spawned trap turret, adding to team.");
            team.AddUnit(turret);
            turret.AddToTeam(team);
            turret.AddToLance(ambushLance);

            turret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, turret, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug?.Write("Updated turret behaviorTree");

            ModState.AmbushBuildingGUIDToTurrets.Add(building.GUID, turret);
            ModState.AmbushTurretGUIDtoBuilding.Add(turret.GUID, building);

            // Associate the building withe the team
            building.AddToTeam(team);
            building.BuildingRep.IsTargetable = true;
            building.BuildingRep.SetHighlightColor(ModState.Combat, team);
            building.BuildingRep.RefreshEdgeCache();

            // Increase the building's health to the current value + turret structure
            float combinedStructure = (float)Math.Ceiling(building.CurrentStructure + turret.GetCurrentStructure(BuildingLocation.Structure));
            Mod.Log.Debug?.Write($"Setting ambush structure to: {combinedStructure} = building.currentStructure: {building.CurrentStructure} + " +
                $"turret.currentStructure: {turret.GetCurrentStructure(BuildingLocation.Structure)}");
            building.StatCollection.Set<float>("Structure", combinedStructure);

            // Finally notify others
            UnitSpawnedMessage message = new UnitSpawnedMessage("CJ_TRAP", turret.GUID);
            ModState.Combat.MessageCenter.PublishMessage(message);

            // Finally force the turret to be fully visible
            turret.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

            return turret;
        }
    }
}
