using System.Collections.Generic;

namespace ConcreteJungle
{

    public class DevastationDef
    {
        public string PlanetTag = null;
        public float MinDevestation = 0f;
        public float MaxDevestation = 0f;
    }

    public abstract class AmbushDef
    {
        public int MinDifficulty = 1;
        public int MaxDifficulty = 3;

        public int MinSpawns = 1;
        public int MaxSpawns = 6;
    }

    public class WeaponDefRef
    {
        public string WeaponDefId;
    }

    public class ExplosionAmbushDef : AmbushDef
    {
        public List<WeaponDefRef> SpawnPool = new List<WeaponDefRef>();
    }

    public class TurretAndPilotDef
    {
        public string TurretDefId;
        public string PilotDefId;
    }
    public class InfantryAmbushDef : AmbushDef
    {
        public List<TurretAndPilotDef> SpawnPool = new List<TurretAndPilotDef>();
    }

    public class MechAndPilotDef
    {
        public string MechDefId;
        public string PilotDefId;
    }
    public class MechAmbushDef : AmbushDef
    {
        public List<MechAndPilotDef> SpawnPool = new List<MechAndPilotDef>();
    }

    public class VehicleAndPilotDef
    {
        public string VehicleDefId;
        public string PilotDefId;
    }
    public class VehicleAmbushDef : AmbushDef
    {
        public List<VehicleAndPilotDef> SpawnPool = new List<VehicleAndPilotDef>();
    }


}
