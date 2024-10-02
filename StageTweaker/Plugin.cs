using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;
using BepInEx.Configuration;

namespace StageTweaker {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class StageTweaker : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "pseudopulse";
        public const string PluginName = "StageTweaker";
        public const string PluginVersion = "1.0.0";

        public static AssetBundle bundle;
        public static BepInEx.Logging.ManualLogSource ModLogger;
        public static ConfigFile config;

        public void Awake() {
            // set logger and config
            ModLogger = Logger;
            config = Config;

            Properties.Initialize();
        }
    }
}