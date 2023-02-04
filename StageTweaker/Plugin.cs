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
        private static List<SceneCollection> sceneCollections = new(); 

        public void Awake() {
            // set logger and config
            ModLogger = Logger;
            config = Config;
        }

        [SystemInitializer(typeof(SceneCatalog))]
        public static void GenerateConfigs() {
            foreach (SceneDef sceneDef in SceneCatalog.allSceneDefs) {
                if (sceneDef.sceneType == SceneType.Menu || sceneDef.sceneType == SceneType.Cutscene) {
                    continue;
                }

                if (sceneDef._cachedName == "ai_test" || sceneDef._cachedName == "testscene") {
                    continue;
                }

                string configSection = sceneDef._cachedName;
                sceneDef.validForRandomSelection = config.Bind<bool>(configSection, "Valid For Random Selection", sceneDef.validForRandomSelection, "Can this stage be randomly chosen in Prismatic Trials?.").Value;
                sceneDef.blockOrbitalSkills = config.Bind<bool>(configSection, "Block Orbital Skills", sceneDef.blockOrbitalSkills, "Prevent Captain from using his orbital skills here?").Value;
                sceneDef.suppressNpcEntry = config.Bind<bool>(configSection, "Suppress NPC Entry", sceneDef.suppressNpcEntry, "Prevent NPCs like Drones from entering this stage.").Value;
                if (sceneDef.destinationsGroup) {
                    List<SceneDef> validScenes = new();
                    foreach (SceneCollection.SceneEntry entry in sceneDef.destinationsGroup.sceneEntries) {
                        validScenes.Add(entry.sceneDef);
                    }
                    string collection = "";
                    foreach (SceneDef scene in validScenes) {
                        collection = collection + scene.cachedName + " ";
                    }
                    string newCollection = config.Bind<string>(configSection, "Destinations", collection, "A list of next-stage scene names, seperated by whitespace.").Value;
                    List<string> scenes = newCollection.Split(' ').ToList();
                    SceneCollection sceneCollection = ScriptableObject.CreateInstance<SceneCollection>();
                    DontDestroyOnLoad(sceneCollection);
                    List<SceneCollection.SceneEntry> entries = new();
                    foreach (string scene in scenes) {
                        SceneDef def = SceneCatalog.FindSceneDef(scene);
                        entries.Add(new() {
                            sceneDef = def,
                            weight = 1,
                            weightMinusOne = 0
                        });
                    }
                    sceneCollection._sceneEntries = entries.ToArray();
                    sceneCollections.Add(sceneCollection);
                    sceneDef.destinationsGroup = sceneCollection;
                }
            }
        }
    }
}