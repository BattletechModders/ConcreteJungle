using BattleTech;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModStats 
    {
    }

    public class ModConfig {

        public class DevestationOpts
        {
            public float MinDevestation = 0.6f;
            public float MaxDevestation = 0.9f;

            public bool Enabled = false;
        }
        public DevestationOpts Devestation = new DevestationOpts();


        public class ExplosionAmbushOpts
        {
            public int SearchRadius = 100;

            public int MinExplosions = 2;
            public int MaxExplosions = 6;

            public float DamagePerShot = 400f;
            public float DamageVariance = 5f;
            
            public float HeatDamagePerShot = 30f;
            //public float HeatDamageVariance = 5f;

            public float StabilityDamagePerShot = 20f;
            //public float StabilityDamageVariance = 5f;

            public bool Enabled = true;

        }
        public ExplosionAmbushOpts ExplosionAmbush = new ExplosionAmbushOpts();

        public class InfantryAmbushOpts
        {
            public int MinBuildings = 2;
            public int MaxBuidlings = 5;

            public List<string> TurretDefIds = new List<string>();
            public List<string> PilotDefIds = new List<string>();

            public string AmbushHUDTitle = "Entrenched Infantry";

            public int SearchRadius = 200;

            public bool VisibleTrapTurrets = true;

            public bool Enabled = true;
        }
        public InfantryAmbushOpts InfantryAmbush = new InfantryAmbushOpts();


        public class AmbushLance
        {
            public string MechDefId;
            public string VehicleDefId;
            public string TurretDefId;
            public string PilotDefId;
        }

        public class SpawnAmbushOpts
        {
            public List<AmbushLance> AmbushLance = new List<AmbushLance>() { 
                new AmbushLance() { VehicleDefId = "vehicledef_DEMOLISHER", PilotDefId = "pilot_d7_brawler" },
                new AmbushLance() { VehicleDefId = "vehicledef_MANTICORE", PilotDefId = "pilot_d7_brawler" }
            };

            public int searchRadius = 200;

            public bool Enabled = true;
        }
        public SpawnAmbushOpts SpawnAmbush = new SpawnAmbushOpts();

        public class QipsConfig
        {
            public List<string> ExplosiveAmbush = new List<string>()
            {
                "Watch your step",
                //"Pushing the plunger",
                //"Boom goes the dynamite",
                //"Not this time invaders",
                //"Salt the earth!",
            };

            public List<string> InfantryAmbush = new List<string>() {
                "Wrong neighborhood, fucko.",
                //"Concentrate fire!",
                //"Open fire!",
                //"Fire Fire Fire!",
                //"Welcome to the jungle!"
            };

            public List<string> SpawnAmbush = new List<string>()
            {
                "Charge!",
            };

        }
        public QipsConfig Qips = new QipsConfig();

        public float CritChanceMultiplier = 1f;

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
