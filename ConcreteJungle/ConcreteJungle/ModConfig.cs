using System;
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModStats 

    {
    }

    public class ModConfig {

        public string TurretDef = "turretdef_Light_Shredder";
        public string TurretPilotDef = "pilot_d7_turret";
        public string VehicleDef = "vehicledef_DEMOLISHER";
        public string VehiclePilotDef = "pilot_d9_brawler";

        public int MaxAmbushTurrets = 1;

        // If true, many logs will be printed
        public bool Debug = false;
        // If true, all logs will be printed
        public bool Trace = false;

        
        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG:{this.Debug} Trace:{this.Trace}");
            

            Mod.Log.Info("=== MOD CONFIG END ===");
        }

        public void Init() {
            Mod.Log.Debug(" == Initializing Configuration");

            Mod.Log.Debug(" == Configuration Initialized");
        }
    }
}
