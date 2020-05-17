using BattleTech;
using ConcreteJungle.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class InfantryAmbushHelper
    {
        public static void SpawnAmbush(Vector3 ambushPos)
        {
            if (!Mod.Config.InfantryAmbush.Enabled) return;

            int infantrySpawns = Mod.Random.Next(ModState.InfantryAmbushDefForContract.MinSpawns, ModState.InfantryAmbushDefForContract.MaxSpawns);
            Mod.Log.Debug($"Spawning up to {infantrySpawns} infantry spawns as part of this ambush.");

            // Create a new lance in the target team
            Lance ambushLance = TeamHelper.CreateAmbushLance(ModState.TargetAllyTeam);

            // Build list of candidate trap buildings
            List<BattleTech.Building> candidates = CandidateBuildingsHelper.ClosestCandidatesToPosition(ambushPos, Mod.Config.Ambush.SearchRadius);
            if (candidates.Count < ModState.InfantryAmbushDefForContract.MinSpawns)
            {
                Mod.Log.Debug($"Insufficient candidate buildings to spawn an infantry ambush. Skipping.");
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
                AbstractActor ambushTurret = SpawnAmbushTurret(ModState.TargetAllyTeam, ambushLance, spawnBuildingShell);
                spawnedActors.Add(ambushTurret);
                spawnBuildings.Add(spawnBuildingShell);
                Mod.Log.Info($"Spawned turret: {ambushTurret} in building: {spawnBuildingShell}");
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
                    if (Vector3.Distance(ambushPos, combatant.CurrentPosition) <= Mod.Config.Ambush.SearchRadius)
                    {
                        targets.Add(combatant);
                    }
                }
            }

            Mod.Log.Info($"Adding InfantryAmbushSequence for {spawnedActors.Count} actors.");
            try
            {
                InfantryAmbushSequence ambushSequence = 
                    new InfantryAmbushSequence(ModState.Combat, ambushPos, spawnedActors, spawnBuildings, targets, Mod.Config.InfantryAmbush.FreeAttackEnabled);
                ModState.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(ambushSequence));
            }
            catch (Exception e)
            {
                Mod.Log.Error("Failed to create AES sequence due to error!", e);
            }
        }

        public static AbstractActor SpawnAmbushTurret(Team team, Lance ambushLance, BattleTech.Building building)
        {

            // Randomly determine one of the spawnpairs from the current ambushdef
            List<TurretAndPilotDef> shuffledSpawns = new List<TurretAndPilotDef>();
            shuffledSpawns.AddRange(ModState.InfantryAmbushDefForContract.SpawnPool);
            shuffledSpawns.Shuffle();
            TurretAndPilotDef ambushDef = shuffledSpawns[0];

            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.Get(ambushDef.PilotDefId);
            TurretDef turretDef = ModState.Combat.DataManager.TurretDefs.GetOrCreate(ambushDef.TurretDefId);
            turretDef.Refresh();

            // Create teh turret
            Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            turret.Init(building.CurrentPosition, building.CurrentRotation.eulerAngles.y, true);
            turret.InitGameRep(null);

            if (turret == null) Mod.Log.Error($"Failed to spawn turretDefId: {ambushDef.TurretDefId} + pilotDefId: {ambushDef.PilotDefId} !");

            Mod.Log.Debug($" Spawned trap turret, adding to team.");
            team.AddUnit(turret);
            turret.AddToLance(ambushLance);

            turret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, turret, BehaviorTreeIDEnum.CoreAITree);
            Mod.Log.Debug("Updated turret behaviorTree");

            ModState.AmbushBuildingGUIDToTurrets.Add(building.GUID, turret);
            ModState.AmbushTurretGUIDtoBuildingGUID.Add(turret.GUID, building.GUID);

            // Associate the building withe the team
            building.AddToTeam(team);
            building.BuildingRep.IsTargetable = true;
            building.BuildingRep.SetHighlightColor(ModState.Combat, team);
            building.BuildingRep.RefreshEdgeCache();

            // determine a position somewhere up the building's axis
            EncounterLayerData encounterLayerData = ModState.Combat.EncounterLayerData;
            Point cellPoint = new Point(
                ModState.Combat.MapMetaData.GetXIndex(building.CurrentPosition.x),
                ModState.Combat.MapMetaData.GetZIndex(building.CurrentPosition.z));
            MapEncounterLayerDataCell melDataCell =
                encounterLayerData.mapEncounterLayerDataCells[cellPoint.Z, cellPoint.X];
            float buildingHeight = melDataCell.GetBuildingHeight();

            float heightDelta = (float)Math.Floor(buildingHeight * 0.9f);
            Mod.Log.Debug($"Building has height: {buildingHeight} and position: {building.CurrentPosition} moving turret to a position 0.9 up the Y axis: {heightDelta}");

            Vector3 newPosition = turret.GameRep.transform.position;
            newPosition.y += heightDelta;
            Mod.Log.Debug($"Changing transform postition from: {turret.GameRep.transform.position} to {newPosition}");
            //turret.GameRep.transform.position = newPosition;
            // TODO: Re-enable?

            // After the position change, notify the game rep and set our visibility to full
            turret.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
            turret.OnPositionUpdate(turret.CurrentPosition, turret.CurrentRotation, -1, true, null, false);
            Mod.Log.Debug("Updated turret visibility and position.");

            // Finally notify others
            UnitSpawnedMessage message = new UnitSpawnedMessage("CJ_TRAP", turret.GUID);
            ModState.Combat.MessageCenter.PublishMessage(message);

            return turret;
        }
    }
}
