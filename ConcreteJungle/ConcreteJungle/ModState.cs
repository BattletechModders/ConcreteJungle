
using BattleTech;
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModState {
        
        public static List<BattleTech.Building> CandidateBuildings = new List<BattleTech.Building>();
        public static HashSet<Team> CandidateTeams = new HashSet<Team>();

        public static HashSet<string> TrapTurretIds = new HashSet<string>();
        public static Dictionary<string, Turret> TrapBuildingsToTurrets = new Dictionary<string, Turret>();
        
        public static CombatGameState Combat = null;

        public static void Reset() {
            // Reinitialize state
            CandidateBuildings.Clear();
            CandidateTeams.Clear();

            TrapTurretIds.Clear();
            TrapBuildingsToTurrets.Clear();

            Combat = null;
        }

    }

}


