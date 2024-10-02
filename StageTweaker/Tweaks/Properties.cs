using System;
using System.Reflection;

namespace StageTweaker {
    public class Properties {
        private static Dictionary<string, int> defaultCredits = new() {
            {"blackbeach2", 220}, {"blackbeach", 220},
            {"golemplains2", 220}, {"golemplains", 220},
            {"snowyforest", 2300},
            {"foggyswamp", 280}, {"goolake", 220}, {"ancientloft", 280},
            {"wispgraveyard", 280}, {"frozenwall", 280}, {"sulfurpools", 280},
            {"dampcavesimple", 400}, {"shipgraveyard", 400}, {"rootjungle", 400},
            {"skymeadow", 500}
        };

        private static Dictionary<string, SpawnCard> spcards = new();
        internal static void Initialize() {
            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += (orig, self, m) => {
                orig(self, m);
                HandleConfigs(default(ConCommandArgs));
            };

            On.RoR2.SceneDirector.PopulateScene += Credits;
            // On.RoR2.ClassicStageInfo.Start += GenerateStagePoolConfig;

            foreach (FieldInfo info in typeof(Utils.Paths.CharacterSpawnCard).GetFields()) {
                if (info.IsStatic && info.GetValue(null) is CharacterSpawnCard) {
                    CharacterSpawnCard card = info.GetValue(null) as CharacterSpawnCard;
                    spcards.Add(card.name, card);
                }
            }

            foreach (FieldInfo info in typeof(Utils.Paths.SpawnCard).GetFields()) {
                if (info.IsStatic && info.GetValue(null) is SpawnCard) {
                    SpawnCard card = info.GetValue(null) as SpawnCard;
                    spcards.Add(card.name, card);
                }
            }
        }

        // [ConCommand(commandName = "st_reload_config", flags = ConVarFlags.None, helpText = "Reloads the Stage Tweaker config.")]
        public static void HandleConfigs(ConCommandArgs args) {
            StageTweaker.ModLogger.LogMessage("Reloading config...");
            foreach (SceneDef scene in SceneCatalog.allStageSceneDefs) {
                string name = Language.GetString(scene.nameToken);
                if (name == scene.nameToken) {
                    continue;
                }
                name = FilterUnsafe(name);
                scene.blockOrbitalSkills = AddConfig<bool>(scene.blockOrbitalSkills, name, "Block Orbital Skills", "Prevent Captain from using orbital skills here.");
                scene.suppressNpcEntry = AddConfig<bool>(scene.suppressNpcEntry, name, "Suppress NPC Entry", "Prevent NPCs (such as drones) from following players into this stage.");
                if (defaultCredits.TryGetValue(scene.cachedName, out int c)) {
                    _ = AddConfig<int>(c, name, "Interactable Credits", "How many credits should this scene have to spend on interactables");
                }
            }

            _ = AddConfig<string>("this config does nothing", ":: Stage Pools", "https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Scene-Names/", "List of stages and their internal names.");
            HandleStagePool("Stage One", Utils.Paths.SceneCollection.sgStage1.Load<SceneCollection>());
            HandleStagePool("Stage Two", Utils.Paths.SceneCollection.sgStage2.Load<SceneCollection>());
            HandleStagePool("Stage Three", Utils.Paths.SceneCollection.sgStage3.Load<SceneCollection>());
            HandleStagePool("Stage Four", Utils.Paths.SceneCollection.sgStage4.Load<SceneCollection>());
            HandleStagePool("Stage Five", Utils.Paths.SceneCollection.sgStage5.Load<SceneCollection>());
        }

        private static void HandleStagePool(string s, SceneCollection collection) {
            string scenes = "";
            foreach (SceneCollection.SceneEntry entry in collection.sceneEntries) {
                scenes = scenes + entry.sceneDef.cachedName + " ";
            }
            scenes = AddConfig<string>(scenes, ":: Stage Pools", s, "The valid stages for this pool (seperated by whitespace)");
            List<string> sceneNames = scenes.Split(' ').ToList();
            List<SceneCollection.SceneEntry> entries = new();
            foreach (string sc in sceneNames) {
                if (sc == " " || sc == "") {
                    continue;
                }
                SceneDef scene = SceneCatalog.GetSceneDefFromSceneName(sc);
                if (!scene) {
                    StageTweaker.ModLogger.LogError("Scene Name: " + sc + " is invalid!");
                    return;
                }

                entries.Add(new SceneCollection.SceneEntry {
                    sceneDef = scene,
                    weight = 1,
                    weightMinusOne = 0,
                });
            }

            collection._sceneEntries = entries.ToArray();
        }

        private static void Credits(On.RoR2.SceneDirector.orig_PopulateScene orig, SceneDirector self) {
            SceneDef scene = SceneCatalog.currentSceneDef;
            string name = Language.GetString(scene.nameToken);
            if (name == scene.nameToken) {
                orig(self);
                return;
            }
            name = FilterUnsafe(name);
            // Debug.Log(name);
            self.interactableCredit = AddConfig<int>(self.interactableCredit, name, "Interactable Credits", "How many credits should this scene have to spend on interactables");
            // Debug.Log(self.interactableCredit);
            orig(self);
            // Debug.Log(self.interactableCredit);
        }

        private static void GenerateStagePoolConfig(On.RoR2.ClassicStageInfo.orig_Start orig, ClassicStageInfo self) {
            SceneDef scene = SceneCatalog.currentSceneDef;
            string name = Language.GetString(scene.nameToken);
            if (name == scene.nameToken) {
                orig(self);
                return;
            }
            name = FilterUnsafe(name);
            if (self.monsterDccsPool && self.monsterDccsPool.poolCategories.Length > 0) {
                DccsPool.Category[] cats = self.monsterDccsPool.poolCategories;
                try {
                    DccsPool.ConditionalPoolEntry entry = cats[0].includedIfConditionsMet[0];
                    if (entry.requiredExpansions.Length == 0) {
                        entry = (DccsPool.ConditionalPoolEntry)cats[0].includedIfNoConditionsMet[0];
                    }
                    DirectorCardCategorySelection dccs = entry.dccs;
                    for (int i = 0; i < dccs.categories.Length; i++) {
                        DirectorCardCategorySelection.Category category = dccs.categories[i];
                        string cards = "";
                        List<DirectorCard> dcards = new();
                        foreach (DirectorCard card in category.cards) {
                            cards = cards + $"{card.spawnCard.name}:{card.selectionWeight}:{card.minimumStageCompletions}" + " ";
                        }
                        cards = AddConfig<string>(cards, name, "Enemies: " + category.name, "The enemies that can spawn in this category, seperated by whitespace. Format: [spawn card name]:[weight]:[min stages]");
                        List<string> enemies = cards.Split(' ').ToList();
                        foreach (string enemy in enemies) {
                            string[] parts = enemy.Split(':');
                            try {
                                SpawnCard scard = spcards[parts[0]];
                                int weight = int.Parse(parts[1]);
                                int min = int.Parse(parts[2]);
                                DirectorCard dcard = new();
                                dcard.spawnCard = scard;
                                dcard.minimumStageCompletions = min;
                                dcard.selectionWeight = weight;
                                dcard.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
                                dcards.Add(dcard);
                            }
                            catch (Exception ex) {
                                
                            }
                        }
                        category.cards = dcards.ToArray();
                    }
                }
                catch (Exception ex) {
                    orig(self);
                    throw ex;
                }
            }
            orig(self);
        }

        private static T AddConfig<T>(T def, string s, string n, string d) {
            return StageTweaker.config.Bind<T>(s, n, def, d).Value;
        }

        private static string FilterUnsafe(string str) {
            str = str.Replace("'", "");
            return str;
        }
    }
}