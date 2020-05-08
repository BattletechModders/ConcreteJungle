using BattleTech;
using Harmony;
using System.Linq;

namespace ConcreteJungle
{
    public static class SpawnHelper
    {
        public static AbstractActor SpawnTrapTurret(string turretDefId, string pilotDefId, Team team, BattleTech.Building parentBuilding)
        {
            PilotDef pilotDef = ModState.Combat.DataManager.PilotDefs.GetOrCreate(pilotDefId);
            TurretDef turretDef = ModState.Combat.DataManager.TurretDefs.GetOrCreate(turretDefId);
            turretDef.Refresh();

            Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, team.EncounterTags, ModState.Combat, team.GetNextSupportUnitGuid(), "", null);
            turret.Init(parentBuilding.CurrentPosition, parentBuilding.CurrentRotation.eulerAngles.y, true);
            turret.InitGameRep(null);

            if (turret == null) Mod.Log.Error($"Failed to spawn turretDefId: {turretDefId} + pilotDefId: {pilotDefId} !");

            Mod.Log.Debug($" Spawned trap turret, adding to team.");
            team.AddUnit(turret);
            turret.AddToLance(team.lances.First<Lance>());

            ModState.TrapBuildingsToTurrets.Add(parentBuilding.GUID, turret);
            ModState.TrapTurretToBuildingIds.Add(turret.GUID, parentBuilding.GUID);

            Mod.Log.Debug($" Parent building associated with team: {parentBuilding.TeamId}, adding to team: {team.GUID}");
            parentBuilding.AddToTeam(team);
            parentBuilding.BuildingRep.IsTargetable = true;
            parentBuilding.BuildingRep.SetHighlightColor(ModState.Combat, team);
            parentBuilding.BuildingRep.RefreshEdgeCache();

            Mod.Log.Debug($" Parent building: " +
                $"bRep.IsTargetable: {parentBuilding.BuildingRep.IsTargetable} " +
                $"gRep.IsTargetable: {parentBuilding.GameRep.IsTargetable} ");

            return turret;
        }
    }
}
