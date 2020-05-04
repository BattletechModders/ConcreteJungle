using System;
using System.Collections.Generic;

namespace ConcreteJungle {

    public static class ModStats 

    {
    }

    public class ModConfig {


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
