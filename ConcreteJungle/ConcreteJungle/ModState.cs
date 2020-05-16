
using BattleTech;
using System.Collections.Generic;
using UnityEngine;
using static ConcreteJungle.Helper.DataLoadHelper;

namespace ConcreteJungle {

    public static class ModState {

        // -- General spawn state        
        public static List<BattleTech.Building> CandidateBuildings = new List<BattleTech.Building>();
        public static List<DataloadResourcePair> LoadedResources = new List<DataloadResourcePair>();
        public static Team TargetAllyTeam = null;
        public static Team HostileToAllTeam = null;

        // -- Infantry Ambush state
        public static Dictionary<string, Turret> TrapBuildingsToTurrets = new Dictionary<string, Turret>();
        public static Dictionary<string, string> TrapTurretToBuildingIds = new Dictionary<string, string>();
        
        public static Turret CurrentTurretForLOF = null;
        public static Turret CurrentTurretForLOS = null;

        // -- General ambush state
        public static Vector3 PendingAmbushOrigin = new Vector3(0f, 0f, 0f);
        public static int TrapsSpawned = 0;
        public static List<Vector3> TrapSpawnOrigins = new List<Vector3>();

        // -- General state
        public static CombatGameState Combat = null;        
        public static bool IsUrbanBiome = false;
        public static int ContractDifficulty = 0;
        
        public static ExplosionAmbushDef ExplosionAmbushDefForContract = null;
        public static InfantryAmbushDef InfantryAmbushDefForContract = null;
        public static MechAmbushDef MechAmbushDefForContract = null;
        public static VehicleAmbushDef VehicleAmbushDefForContract = null;

        public static void Reset() {
            // Reinitialize state
            CandidateBuildings.Clear();
            TargetAllyTeam = null;
            HostileToAllTeam = null;

            TrapBuildingsToTurrets.Clear();
            TrapTurretToBuildingIds.Clear();

            CurrentTurretForLOF = null;
            CurrentTurretForLOS = null;

            PendingAmbushOrigin = new Vector3(0f, 0f, 0f);

            TrapsSpawned = 0;
            TrapSpawnOrigins.Clear();

            Combat = null;
            IsUrbanBiome = false;
            ContractDifficulty = 0;

            ExplosionAmbushDefForContract = null;
            InfantryAmbushDefForContract = null;
            MechAmbushDefForContract = null;
            VehicleAmbushDefForContract = null;
    }

    }

}


