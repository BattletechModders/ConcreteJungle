using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle.Helper
{
    public static class InfantryAmbushHelper
    {
        public static void SpawnAmbush(Vector3 originPos)
        {
            if (!Mod.Config.InfantryAmbush.Enabled) return;

            // Build list of candidate trap buildings
            List<BattleTech.Building> candidates = CandidateHelper.FilterCandidates(originPos, Mod.Config.InfantryAmbush.SearchRadius);

            if (candidates.Count < Mod.Config.InfantryAmbush.MinBuildings)
            {
                Mod.Log.Debug($"Insufficient candidate buildings to spawn an infantry ambush. Skipping.");
                return;
            }

            // Determine the team we should use for the traps
            int teamIdx = ModState.CandidateTeams.Count > 0 ? Mod.Random.Next(0, ModState.CandidateTeams.Count - 1) : 0;
            Team trapTeam = ModState.CandidateTeams.ElementAt<Team>(teamIdx);

            int ambushCount = Mod.Random.Next(Mod.Config.InfantryAmbush.MinBuildings, Mod.Config.InfantryAmbush.MaxBuidlings);
            if (candidates.Count < ambushCount) ambushCount = candidates.Count;
            Mod.Log.Debug($"Spawning {ambushCount} ambushes for team: {trapTeam}");

            List<Vector3> spawnPositions = new List<Vector3>();
            for (int i = 0; i < ambushCount; i++)
            {
                BattleTech.Building trapBuilding = candidates.ElementAt(i);

                // Spawn a turret trap
                int turretDefIdx = Mod.Random.Next(0, Mod.Config.InfantryAmbush.TurretDefIds.Count - 1);
                string turretDefId = Mod.Config.InfantryAmbush.TurretDefIds.ElementAt(turretDefIdx);
                int pilotDefIdx = Mod.Random.Next(0, Mod.Config.InfantryAmbush.PilotDefIds.Count - 1);
                string pilotDefId = Mod.Config.InfantryAmbush.PilotDefIds.ElementAt(pilotDefIdx);
                Mod.Log.Debug($"Spawning turretDef: {turretDefId} + pilotDef: {pilotDefId} within building at position: {trapBuilding.CurrentPosition}");

                AbstractActor trapTurret = SpawnTrapTurret(turretDefId, pilotDefId, trapTeam, trapBuilding);

                trapTurret.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
                trapTurret.OnPositionUpdate(trapBuilding.CurrentPosition, trapBuilding.CurrentRotation, -1, true, null, false);
                Mod.Log.Debug("Updated turret visibility and position.");

                trapTurret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(ModState.Combat.BattleTechGame, trapTurret, BehaviorTreeIDEnum.CoreAITree);
                Mod.Log.Debug("Updated turret behaviorTree");

                UnitSpawnedMessage message = new UnitSpawnedMessage("CJ_TRAP", trapTurret.GUID);
                ModState.Combat.MessageCenter.PublishMessage(message);
                
                Mod.Log.Debug($"Sent spawn message for position: {trapTurret.CurrentPosition}");
                spawnPositions.Add(trapTurret.CurrentPosition);

                if (i + 1 == ambushCount)
                {
                    // Create a quip
                    QuipHelper.PublishQuip(trapTurret, Mod.Config.Qips.InfantryAmbush, 6);
                }
            }

            ModState.TrapSpawnOrigins.Add(originPos);

        }

        public static AbstractActor SpawnTrapTurret(string turretDefId, string pilotDefId, Team team, BattleTech.Building building)
        {
            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.GetOrCreate(pilotDefId);
            TurretDef turretDef = ModState.Combat.DataManager.TurretDefs.GetOrCreate(turretDefId);
            turretDef.Refresh();

            Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            turret.Init(building.CurrentPosition, building.CurrentRotation.eulerAngles.y, true);
            turret.InitGameRep(null);

            if (turret == null) Mod.Log.Error($"Failed to spawn turretDefId: {turretDefId} + pilotDefId: {pilotDefId} !");

            Mod.Log.Debug($" Spawned trap turret, adding to team.");
            team.AddUnit(turret);
            turret.AddToLance(team.lances.First<Lance>());

            ModState.TrapBuildingsToTurrets.Add(building.GUID, turret);
            ModState.TrapTurretToBuildingIds.Add(turret.GUID, building.GUID);

            Mod.Log.Debug($" -- using building: {CombatantUtils.Label(building)} as trap.");
            float surfaceArea = building.DestructibleObjectGroup.footprint.x * building.DestructibleObjectGroup.footprint.z;
            Mod.Log.Debug($" -- building has dimensions {building.DestructibleObjectGroup.footprint} with surface: {surfaceArea} and " +
                $"height: {building.DestructibleObjectGroup.footprint.y}  team: {building.TeamId}");

            Mod.Log.Debug($" Parent building associated with team: {building.TeamId}, adding to team: {team.GUID}");
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

            return turret;
        }
    }
}
