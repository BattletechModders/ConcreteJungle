
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModState {
        
        public static List<BattleTech.Building> TargetableBuildings = new List<BattleTech.Building>();

        public static void Reset() {
            // Reinitialize state
            TargetableBuildings.Clear();
        }

    }

}


