
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModStats 
    {
    }

    public class ModConfig {

        public class DevestationOpts
        {
            // If false, buildings will not be pre-destroyed
            public bool Enabled = false;
            
            // If no tags match, the range we'll use
            public DevestationDef DefaultRange = new DevestationDef() { MinDevestation = 0.3f, MaxDevestation = 0.9f };

            // Ranges specified by planet tags. We'll use the worst effect.
            public HashSet<DevestationDef> RangesByPlanetTag = new HashSet<DevestationDef>();
        }
        public DevestationOpts Devestation = new DevestationOpts();

        public class ExplosionAmbushOpts
        {
            // If false, cannot be selected randomly
            public bool Enabled = true;

            // How far from the trigger origin should we search for suitable buildings
            public int SearchRadius = 100;

            // All of the ambush definitions
            public List<ExplosionAmbushDef> Ambushes = new List<ExplosionAmbushDef>();
        }
        public ExplosionAmbushOpts ExplosionAmbush = new ExplosionAmbushOpts();

        public class InfantryAmbushOpts
        {
            // If false, cannot be selected randomly
            public bool Enabled = true;

            // If true, every unit will generate an attack sequence against the closest target
            public bool FreeAttackEnabled = true;

            // How far from the trigger origin should we search for suitable buildings
            public int SearchRadius = 200;

            // If true, the trap turrets will be visible to the player. 
            public bool VisibleTrapTurrets = true;

            // All of the ambush definitions
            public List<InfantryAmbushDef> Ambushes = new List<InfantryAmbushDef>();

        }
        public InfantryAmbushOpts InfantryAmbush = new InfantryAmbushOpts();

        public class SpawnAmbushOpts
        {
            // If false, cannot be selected randomly
            public bool Enabled = true;

            // If true, every unit will generate an attack sequence against the closest target
            public bool FreeAttackEnabled = true;

            // How far from the trigger origin should we search for suitable buildings
            public int SearchRadius = 200;

            // All of the ambush definitions
            public List<SpawnAmbushDef> Ambushes = new List<SpawnAmbushDef>();

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
                //"Concentrate on the lead!",
                //"Open fire!",
                //"Fire Fire Fire!",
                //"Focus on the critical points!",
                //"Welcome to the jungle!"
            };

            public List<string> SpawnAmbush = new List<string>()
            {
                "Charge!",
                "Get'em boys!"
            };

        }
        public QipsConfig Qips = new QipsConfig();

        public int MaxAbushesPerMap = 2;
        public float MinDistanceBetweenAmbushes = 600f;

        // TODO: need a weight for ambushes, from 1-10. S, I, E maybe? 
        
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

            if (Mod.Config.ExplosionAmbush.Ambushes.Count == 0)
            {
                Mod.Config.ExplosionAmbush.Ambushes.Add(new ExplosionAmbushDef
                {
                    MinDifficulty = 1,
                    MaxDifficulty = 10,
                    MinSpawns = 2,
                    MaxSpawns = 6,
                    WeaponDefs = { "Weapon_Ambush_Explosion" }
                    
                });
            }
            
            if (Mod.Config.InfantryAmbush.Ambushes.Count == 0)
            {
                Mod.Config.InfantryAmbush.Ambushes.Add(new InfantryAmbushDef
                {
                    MinDifficulty = 1,
                    MaxDifficulty = 10,
                    MinSpawns = 2,
                    MaxSpawns = 6,
                    TurretDefs = { "turretdef_Light_Shredder", "turretdef_Light_Laser" },
                    PilotDefId = "pilot_d5_turret"
                });
            }
            
            if (Mod.Config.SpawnAmbush.Ambushes.Count == 0)
            {
                Mod.Config.SpawnAmbush.Ambushes.Add(new SpawnAmbushDef
                {
                    MinDifficulty = 1,
                    MaxDifficulty = 10,
                    MinSpawns = 2,
                    MaxSpawns = 6,
                    MechDefs = { "mechdef_urbanmech_UM-R60" },
                    TurretDefs = { },
                    VehicleDefs = { "vehicledef_BULLDOG", "vehicledef_MANTICORE", "vehicledef_CARRIER_SRM" },
                    PilotDefId = "pilot_d3_gunner"
                });
            }

            Mod.Log.Debug(" == Configuration Initialized");
        }
    }
}
