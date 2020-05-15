
using BattleTech;
using System.Collections.Generic;
using UnityEngine;

namespace ConcreteJungle {

    public static class ModState {
        
        public static List<BattleTech.Building> CandidateBuildings = new List<BattleTech.Building>();
        public static HashSet<Team> CandidateTeams = new HashSet<Team>();

        public static Dictionary<string, Turret> TrapBuildingsToTurrets = new Dictionary<string, Turret>();
        public static Dictionary<string, string> TrapTurretToBuildingIds = new Dictionary<string, string>();
        
        public static Turret CurrentTurretForLOF = null;
        public static Turret CurrentTurretForLOS = null;

        public static Vector3 PendingAmbushOrigin = new Vector3(0f, 0f, 0f);

        public static int TrapsSpawned = 0;
        public static List<Vector3> TrapSpawnOrigins = new List<Vector3>();

        public static CombatGameState Combat = null;

        public static void Reset() {
            // Reinitialize state
            CandidateBuildings.Clear();
            CandidateTeams.Clear();

            TrapBuildingsToTurrets.Clear();
            TrapTurretToBuildingIds.Clear();

            CurrentTurretForLOF = null;
            CurrentTurretForLOS = null;

            PendingAmbushOrigin = new Vector3(0f, 0f, 0f);

            TrapsSpawned = 0;
            TrapSpawnOrigins.Clear();

            Combat = null;
        }

    }

}


