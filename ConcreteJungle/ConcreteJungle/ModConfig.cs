
using System;
using System.Collections.Generic;
using System.Text;

namespace ConcreteJungle {

    public static class ModStats 
    {
    }

    public class ModConfig {

        public AmbushOpts Ambush = new AmbushOpts();
        public DevastationOpts Devastation = new DevastationOpts();
        public ExplosionAmbushOpts ExplosionAmbush = new ExplosionAmbushOpts();
        public InfantryAmbushOpts InfantryAmbush = new InfantryAmbushOpts();
        public MechAmbushOpts MechAmbush = new MechAmbushOpts();
        public VehicleAmbushOpts VehicleAmbush = new VehicleAmbushOpts();
        public QuipsConfig Quips = new QuipsConfig();

        public const string FT_IED_Default = "FT_IED_DEFAULT";
        public const string FT_Turret_Death = "TURRET_DEATH";
        public Dictionary<string, string> LocalizedText = new Dictionary<string, string> {
            { FT_IED_Default, "Explosive IED" },
            { FT_Turret_Death, "{0} DESTROYED" },
        };

        // If true, many logs will be printed
        public bool Debug = false;
        // If true, all logs will be printed
        public bool Trace = false;

        public void LogConfig() {
            Mod.Log.Info?.Write("=== MOD CONFIG BEGIN ===");
            Mod.Log.Info?.Write($"  DEBUG:{this.Debug} Trace:{this.Trace}");

            Mod.Log.Info?.Write(" -- General Ambush Options");
            Mod.Log.Info?.Write($"   MaxPerMap: {this.Ambush.MaxPerMap}  MinDistanceBetween: {this.Ambush.MinDistanceBetween}  " +
                $"BaseChance: {this.Ambush.BaseChance}  ChancePerActor: {this.Ambush.ChancePerActor}  " +
                $"SearchRadius: {this.Ambush.SearchRadius}");
            string ambushWeights = String.Join(", ", this.Ambush.AmbushWeights);
            Mod.Log.Info?.Write($" AmbushWeights: {ambushWeights}  EnableOnRound: {Ambush.EnableOnRound}");
            string blacklistedContracts = String.Join(", ", this.Ambush.BlacklistedContracts);
            Mod.Log.Info?.Write($" Blacklisted Contracts: {blacklistedContracts}");

            Mod.Log.Info?.Write(" -- Devastation Options");
            Mod.Log.Info?.Write($"   Enabled: {this.Devastation.Enabled}  " +
                $"DefaultMin: {this.Devastation.DefaultRange.MinDevastation}  DefaultMax: {this.Devastation.DefaultRange.MaxDevastation}"
                );

            Mod.Log.Info?.Write(" -- Explosive Ambush Options");
            Mod.Log.Info?.Write($"   Enabled: {this.ExplosionAmbush.Enabled}  ");
            foreach (ExplosionAmbushDef ambushDef in this.ExplosionAmbush.Ambushes)
            {
                Mod.Log.Info?.Write("   -- Explosion Ambush Def");
                Mod.Log.Info?.Write($"   Difficulty Min: {ambushDef.MinDifficulty} => Max: {ambushDef.MaxDifficulty}");
                Mod.Log.Info?.Write($"   Spawns Min: {ambushDef.MinSpawns} => Max: {ambushDef.MinSpawns}");
                Mod.Log.Info?.Write($"   AOEBlastDefs:");
                
                foreach(AOEBlastDef blastDef in ambushDef.SpawnPool)
                {
                    Mod.Log.Info?.Write($"   -- floatieLabel: '{blastDef.FloatieTextKey}' radius: {blastDef.Radius} damage: {blastDef.Damage} " +
                        $"heat: {blastDef.Heat} stability: {blastDef.Stability}"); ;
                    Mod.Log.Info?.Write($"      fireRadius: {blastDef.FireRadius} fireStrength: {blastDef.FireStrength} " +
                        $"fireChance: {blastDef.FireChance} fireDurationNoForest: {blastDef.FireDurationNoForest}"); ;
                }
            }

            Mod.Log.Info?.Write(" -- Infantry Ambush Options");
            Mod.Log.Info?.Write($"   Enabled: {this.InfantryAmbush.Enabled}  FreeAttackEnabled: {this.InfantryAmbush.FreeAttackEnabled}.");
            foreach (InfantryAmbushDef ambushDef in this.InfantryAmbush.Ambushes)
            {
                Mod.Log.Info?.Write("   -- Infantry Ambush Def");
                Mod.Log.Info?.Write($"   Difficulty Min: {ambushDef.MinDifficulty} => Max: {ambushDef.MaxDifficulty}");
                Mod.Log.Info?.Write($"   Spawns Min: {ambushDef.MinSpawns} => Max: {ambushDef.MinSpawns}");
                StringBuilder sb = new StringBuilder();
                foreach (TurretAndPilotDef loadDef in ambushDef.SpawnPool)
                {
                    sb.Append(loadDef.TurretDefId);
                    sb.Append("::");
                    sb.Append(loadDef.PilotDefId);
                    sb.Append(", ");
                }
                Mod.Log.Info?.Write($"   Turret and PilotDefs: [ {sb} ]");
            }

            Mod.Log.Info?.Write(" -- Mech Ambush Options");
            Mod.Log.Info?.Write($"   Enabled: {this.MechAmbush.Enabled}  FreeAttackEnabled: {this.MechAmbush.FreeAttackEnabled}.");
            foreach (MechAmbushDef ambushDef in this.MechAmbush.Ambushes)
            {
                Mod.Log.Info?.Write("   -- Mech Ambush Def");
                Mod.Log.Info?.Write($"   Difficulty Min: {ambushDef.MinDifficulty} => Max: {ambushDef.MaxDifficulty}");
                Mod.Log.Info?.Write($"   Spawns Min: {ambushDef.MinSpawns} => Max: {ambushDef.MinSpawns}");
                StringBuilder sb = new StringBuilder();
                foreach (MechAndPilotDef loadDef in ambushDef.SpawnPool)
                {
                    sb.Append(loadDef.MechDefId);
                    sb.Append("::");
                    sb.Append(loadDef.PilotDefId);
                    sb.Append(", ");
                }
                Mod.Log.Info?.Write($"   Mech and PilotDefs: [ {sb} ]");
            }

            Mod.Log.Info?.Write(" -- Vehicle Ambush Options");
            Mod.Log.Info?.Write($"   Enabled: {this.VehicleAmbush.Enabled}  FreeAttackEnabled: {this.VehicleAmbush.FreeAttackEnabled}.");
            foreach (VehicleAmbushDef ambushDef in this.VehicleAmbush.Ambushes)
            {
                Mod.Log.Info?.Write("   -- Vehicle Ambush Def");
                Mod.Log.Info?.Write($"   Difficulty Min: {ambushDef.MinDifficulty} => Max: {ambushDef.MaxDifficulty}");
                Mod.Log.Info?.Write($"   Spawns Min: {ambushDef.MinSpawns} => Max: {ambushDef.MinSpawns}");
                StringBuilder sb = new StringBuilder();
                foreach (VehicleAndPilotDef loadDef in ambushDef.SpawnPool)
                {
                    sb.Append(loadDef.VehicleDefId);
                    sb.Append("::");
                    sb.Append(loadDef.PilotDefId);
                    sb.Append(", ");
                }
                Mod.Log.Info?.Write($"   Mech and PilotDefs: [ {sb} ]");
            }

            Mod.Log.Info?.Write(" -- Quips");
            Mod.Log.Info?.Write(" --- Explosive Quips");
            foreach (string quip in Mod.Config.Quips.ExplosiveAmbush) Mod.Log.Info?.Write($"   '{quip}'");
            Mod.Log.Info?.Write(" --- Infantry Quips");
            foreach (string quip in Mod.Config.Quips.InfantryAmbush) Mod.Log.Info?.Write($"   '{quip}'");
            Mod.Log.Info?.Write(" --- Spawn Quips");
            foreach (string quip in Mod.Config.Quips.SpawnAmbush) Mod.Log.Info?.Write($"   '{quip}'");

            Mod.Log.Info?.Write(" -- Localized Text");
            foreach (KeyValuePair<string, string> kvp in Mod.Config.LocalizedText) Mod.Log.Info?.Write($"   {kvp.Key}='{kvp.Value}'"); 

            Mod.Log.Info?.Write("=== MOD CONFIG END ===");
        }

        public void Init() {
            Mod.Log.Debug?.Write(" == Initializing Configuration");

            if (Mod.Config.ExplosionAmbush.Ambushes.Count == 0)
            {
                Mod.Config.ExplosionAmbush.Ambushes.Add(new ExplosionAmbushDef
                {
                    MinDifficulty = 1,
                    MaxDifficulty = 10,
                    MinSpawns = 1,
                    MaxSpawns = 6,
                    SpawnPool = new List<AOEBlastDef>() {
                        new AOEBlastDef { 
                            FloatieTextKey = "FT_IED_DEFAULT",
                            Radius = 120.0f,
                            Damage = 40.0f,
                            Heat = 30.0f,
                            Stability = 50.0f,
                            FireRadius = 40,
                            FireStrength = 2,
                            FireChance = 50.0f,
                            FireDurationNoForest = 2
                        }
                    }
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
                    SpawnPool = new List<TurretAndPilotDef>() {
                        new TurretAndPilotDef{ TurretDefId = "turretdef_Light_Shredder", PilotDefId = "pilot_d5_turret" },
                        new TurretAndPilotDef{ TurretDefId = "turretdef_Light_Laser", PilotDefId = "pilot_d5_turret" }
                    }
                });
            }

            if (Mod.Config.MechAmbush.Ambushes.Count == 0)
            {
                Mod.Config.MechAmbush.Ambushes.Add(new MechAmbushDef
                {
                    MinDifficulty = 1,
                    MaxDifficulty = 10,
                    MinSpawns = 2,
                    MaxSpawns = 6,
                    SpawnPool = new List<MechAndPilotDef>() {
                        new MechAndPilotDef{ MechDefId = "mechdef_urbanmech_UM-R60", PilotDefId = "pilot_d3_gunner" },
                        new MechAndPilotDef{ MechDefId = "mechdef_urbanmech_UM-R60", PilotDefId = "pilot_d3_gunner" },
                        new MechAndPilotDef{ MechDefId = "mechdef_urbanmech_UM-R60L", PilotDefId = "pilot_d3_gunner" },
                        new MechAndPilotDef{ MechDefId = "mechdef_panther_PNT-9R", PilotDefId = "pilot_d3_gunner" },
                        new MechAndPilotDef{ MechDefId = "mechdef_hunchback_HBK-4G", PilotDefId = "pilot_d3_gunner" }
                    }
                });
            }

            if (Mod.Config.VehicleAmbush.Ambushes.Count == 0)
            {
                Mod.Config.VehicleAmbush.Ambushes.Add(new VehicleAmbushDef
                {
                    MinDifficulty = 1,
                    MaxDifficulty = 10,
                    MinSpawns = 2,
                    MaxSpawns = 6,
                    SpawnPool = new List<VehicleAndPilotDef>() {
                        new VehicleAndPilotDef{ VehicleDefId = "vehicledef_BULLDOG", PilotDefId = "pilot_d3_gunner" },
                        new VehicleAndPilotDef{ VehicleDefId = "vehicledef_MANTICORE", PilotDefId = "pilot_d3_gunner" },
                        new VehicleAndPilotDef{ VehicleDefId = "vehicledef_CARRIER_SRM", PilotDefId = "pilot_d3_gunner" },
                        new VehicleAndPilotDef{ VehicleDefId = "vehicledef_CARRIER_SRM", PilotDefId = "pilot_d3_gunner" }
                    }
                });
            }

            Mod.Log.Debug?.Write(" -- Initializing weights");
            this.BuildWeightTable();
            this.ValidateWeights();

            Mod.Log.Debug?.Write(" == Configuration Initialized");
        }

        // Translate the strings in the config to enum types
        private void BuildWeightTable()
        {
            foreach(string weightS in this.Ambush.AmbushWeights)
            {
                AmbushType enumVal = (AmbushType)Enum.Parse(typeof(AmbushType), weightS);
                this.Ambush.AmbushTypes.Add(enumVal);
            }
        }

        // Ensure that someone hasn't configured a weight value that is currently disabled
        private void ValidateWeights()
        {
            foreach (AmbushType type in this.Ambush.AmbushTypes)
            { 
                switch (type)
                {
                    case AmbushType.Explosion:
                        if (!Mod.Config.ExplosionAmbush.Enabled) throw new InvalidOperationException($"Ambush type: {type} in weight table but marked disabled!");
                        break;
                    case AmbushType.Infantry:
                        if (!Mod.Config.InfantryAmbush.Enabled) throw new InvalidOperationException($"Ambush type: {type} in weight table but marked disabled!");
                        break;
                    case AmbushType.Mech:
                        if (!Mod.Config.MechAmbush.Enabled) throw new InvalidOperationException($"Ambush type: {type} in weight table but marked disabled!");
                        break;
                    case AmbushType.Vehicle:
                        if (!Mod.Config.VehicleAmbush.Enabled) throw new InvalidOperationException($"Ambush type: {type} in weight table but marked disabled!");
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown ambush type: {type}!");
                }
            }
        }
    }
}
