using System.Collections.Generic;

namespace ConcreteJungle
{
    public abstract class AmbushDef
    {
        public int MinDifficulty = 1;
        public int MaxDifficulty = 3;

        public int MinSpawns = 1;
        public int MaxSpawns = 6;
    }

    public class InfantryAmbushDef : AmbushDef
    {
        public HashSet<string> TurretDefs = new HashSet<string>();
        public string PilotDefId;
    }

    public class ExplosionAmbushDef : AmbushDef
    {
        public HashSet<string> WeaponDefs = new HashSet<string>();
    }

    public class SpawnAmbushDef : AmbushDef
    {
        public HashSet<string> MechDefs = new HashSet<string>();
        public HashSet<string> TurretDefs = new HashSet<string>();
        public HashSet<string> VehicleDefs = new HashSet<string>();
        public string PilotDefId;
    }

    public class DevestationDef
    {
        public string PlanetTag = null;
        public float MinDevestation = 0f;
        public float MaxDevestation = 0f;
    }


}
