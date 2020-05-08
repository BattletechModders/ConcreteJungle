
using BattleTech;
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModState {
        
        public static List<BattleTech.Building> CandidateBuildings = new List<BattleTech.Building>();
        public static HashSet<Team> CandidateTeams = new HashSet<Team>();

        public static Dictionary<string, Turret> TrapBuildingsToTurrets = new Dictionary<string, Turret>();
        public static Dictionary<string, string> TrapTurretToBuildingIds = new Dictionary<string, string>();
        
        public static Turret CurrentTurretForLOF = null;
        
        public static CombatGameState Combat = null;

        public static void Reset() {
            // Reinitialize state
            CandidateBuildings.Clear();
            CandidateTeams.Clear();

            TrapBuildingsToTurrets.Clear();
            TrapTurretToBuildingIds.Clear();

            CurrentTurretForLOF = null;
            Combat = null;
        }

    }

}


