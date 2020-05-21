using System.Collections.Generic;

namespace ConcreteJungle
{

    public class AmbushOpts
    {
        public int MaxPerMap = 2;
        public float MinDistanceBetween = 600f;
        public float BaseChance = 0.3f;
        public float ChancePerActor = 0.05f;

        // How far from the trigger origin should we search for suitable buildings
        public float SearchRadius = 250.0f;

        // Maybe tie this into tags? Make this just a default?
        public List<string> AmbushWeights = new List<string>();
        public List<AmbushType> AmbushTypes = new List<AmbushType>(); // internal only - do not config in mod.json
    }


    public class DevastationOpts
    {
        // If false, buildings will not be pre-destroyed
        public bool Enabled = false;

        // If no tags match, the range we'll use
        public DevastationDef DefaultRange = new DevastationDef() { MinDevastation = 0.3f, MaxDevastation = 0.9f };

        // Ranges specified by planet tags. We'll use the worst effect.
        public List<DevastationDef> RangesByPlanetTag = new List<DevastationDef>();
    }

    public class ExplosionAmbushOpts
    {
        // If false, cannot be selected randomly
        public bool Enabled = true;

        // The visual effect to ask CAC to spawn. See CombatGameConstants for artillery_ VFX
        // "WFX_Nuke" provided by KMission
        public string VFX = "WFX_Nuke";

        // The sound effect to ask CAC to spawn. CAC will accept the AudioEventNames?
        // AudioEventList_explosion.explosion_large
        // AudioEventList_impact.impact_thumper
        // AudioEventList_impact.impact_mortar
        // big_explosion provided by KMission
        public string SFX = "big_explosion";

        // The weapons that can be used in the ambush
        public List<ExplosionAmbushDef> Ambushes = new List<ExplosionAmbushDef>();
    }

    public class InfantryAmbushOpts
    {
        // If false, cannot be selected randomly
        public bool Enabled = true;

        // If true, every unit will generate an attack sequence against the closest target
        public bool FreeAttackEnabled = true;

        // If true, the trap turrets will be visible to the player. 
        public bool VisibleTrapTurrets = true;

        // All of the ambush definitions
        public List<InfantryAmbushDef> Ambushes = new List<InfantryAmbushDef>();

    }

    public class MechAmbushOpts
    {
        // If false, cannot be selected randomly
        public bool Enabled = true;

        // If true, every unit will generate an attack sequence against the closest target
        public bool FreeAttackEnabled = true;

        // The actor/pilot pairs that are possible ambushers
        public List<MechAmbushDef> Ambushes = new List<MechAmbushDef>();

    }
    public class VehicleAmbushOpts
    {
        // If false, cannot be selected randomly
        public bool Enabled = true;

        // If true, every unit will generate an attack sequence against the closest target
        public bool FreeAttackEnabled = true;

        // All of the ambush definitions
        public List<VehicleAmbushDef> Ambushes = new List<VehicleAmbushDef>();

    }

    public class QuipsConfig
    {
        public List<string> ExplosiveAmbush = new List<string>()
            {
                "Watch your step",
            };

        public List<string> InfantryAmbush = new List<string>() {
                "Wrong neighborhood, fucko.",
            };

        public List<string> SpawnAmbush = new List<string>()
            {
                "Charge!",
            };

    }

}
