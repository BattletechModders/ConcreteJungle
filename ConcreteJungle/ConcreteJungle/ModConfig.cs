using System;
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModStats 
    {
    }

    public class ModConfig {

        public class InfantryAmbushOpts
        {
            public int MinBuildings = 2;
            public int MaxBuidlings = 5;

            public List<string> TurretDefIds = new List<string>();
            public List<string> PilotDefIds = new List<string>();

            public string AmbushHUDTitle = "Entrenched Infantry";

            public int SearchRadius = 300;

            public bool VisibleTrapTurrets = true;
        }
        public InfantryAmbushOpts InfantryAmbush = new InfantryAmbushOpts();

        public class DevestationOpts
        {
            public float MinDevestation = 0.6f;
            public float MaxDevestation = 0.9f;

            public bool Enabled = true;
        }
        public DevestationOpts Devestation = new DevestationOpts();

        public class QipsConfig
        {
            public List<string> InfantryAmbush = new List<string>() {
                "Wrong neighborhood, fucko.",
                //"Concentrate fire!",
                //"Open fire!",
                //"Fire Fire Fire!",
                //"Welcome to the jungle!"
            };

            public List<string> VehicleAmbush = new List<string>()
            {

            };

            public List<string> BattleArmorAmbush = new List<string>()
            {

            };

            public List<string> ExplosiveAmbush = new List<string>()
            {

            };

        }
        public QipsConfig Qips = new QipsConfig();

        public string VehicleDef = "vehicledef_DEMOLISHER";
        public string VehiclePilotDef = "pilot_d9_brawler";

        public int MaxSpawns = 2;
        public float MinSpawnDistance = 600f;
        
        // If true, many logs will be printed
        public bool Debug = false;
        // If true, all logs will be printed
        public bool Trace = false;

        
        public void LogConfig() {
            Mod.Log.Info("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info($"  DEBUG:{this.Debug} Trace:{this.Trace}");

            Mod.Log.Debug(" -- Infantry Ambush Options ");


            Mod.Log.Info("=== MOD CONFIG END ===");
        }

        public void Init() {
            Mod.Log.Debug(" == Initializing Configuration");

            // Infantry def defaults
            if (this.InfantryAmbush?.TurretDefIds?.Count == 0)
            {
                this.InfantryAmbush.TurretDefIds.Add("turretdef_Light_Shredder");
            }
            if (this.InfantryAmbush?.PilotDefIds?.Count == 0)
            {
                this.InfantryAmbush.PilotDefIds.Add("pilot_d7_turret");
            }

            Mod.Log.Debug(" == Configuration Initialized");
        }
    }
}
