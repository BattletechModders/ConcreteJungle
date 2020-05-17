using BattleTech;
using System;

namespace ConcreteJungle.Helper
{
    public static class TeamHelper
    {
        public static Lance CreateAmbushLance(Team team)
        {
            Lance lance = new Lance(team, new BattleTech.Framework.LanceSpawnerRef[] { });
            Guid g = Guid.NewGuid();
            string lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(g);
            lance.lanceGuid = lanceGuid;
            ModState.Combat.ItemRegistry.AddItem(lance);
            team.lances.Add(lance);

            return lance;
        }
    }
}
