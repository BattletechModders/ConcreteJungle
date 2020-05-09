using BattleTech;
using Harmony;
using System;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace ConcreteJungle
{
    public static class SpawnHelper
    {
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

            Mod.Log.Debug($" Parent building: " +
                $"bRep.IsTargetable: {building.BuildingRep.IsTargetable} " +
                $"gRep.IsTargetable: {building.GameRep.IsTargetable} ");

            // determine a position somewhere up the building's axis
            float height = building.DestructibleObjectGroup.footprint.y;
            float heightDelta = (float)Math.Floor(height * 0.80f);
            Vector3 newPosition = turret.GameRep.transform.position;
            newPosition.y += heightDelta;
            turret.GameRep.transform.position = newPosition;

            return turret;
        }
    }
}
