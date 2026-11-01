using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EcoGame.Animals;
using EcoGame.Building;
using EcoGame.Cameras;
using EcoGame.Core;
using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Machines;
using EcoGame.Player;
using EcoGame.Quiz;
using EcoGame.Trash;
using EcoGame.UI;
using EcoGame.Water;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using EcoPlayerSettings = EcoGame.Data.PlayerSettings;

namespace EcoGame.Utils.Editor
{
    /// <summary>
    /// Generates materials, data assets, prefabs and Main.unity from scratch.
    /// Re-running overwrites generated prefabs and the scene.
    /// </summary>
    public static class EcoSceneBuilder
    {
        private const string P = "Assets/_Project/";
        private const string TP = "Assets/_ThirdParty/";
        private const string NATURE = TP + "kenney_nature-kit/Models/FBX format/";
        private const string FOOD = TP + "kenney_food-kit/Models/FBX format/";
        private const string PETS = TP + "kenney_cube-pets_1.0/Models/FBX format/";
        private const string CITY = TP + "kenney_city-kit-suburban_20/Models/FBX format/";
        private const string GRAVE = TP + "kenney_graveyard-kit_5.0/Models/FBX format/";
        private const string SURVIVOR = TP + "kenney_animated-characters-survivors/";
        private const string FACTORY = TP + "kenney_factory-kit_3.0/Models/FBX format/";
        private const string SURVIVAL = TP + "kenney_survival-kit/Models/FBX format/";
        private const string FANTASY =TP + "kenney_fantasy-town-kit_2.0/Models/FBX format/";
        private const int IGNORE_RAYCAST_LAYER = 2;

        private static readonly List<string> Placeholders = new();

        private class Mats
        {
            public Material Red, Blue, Green, Grey, Yellow, Ground, Water, Hologram, Smoke, Player;
        }

        private class Prefabs
        {
            public GameObject Player, TopDownCamera, Generator, Recycling, Desalinator;
            public GameObject SiteSolar, SiteWind, SiteMill, SiteNet;
            public GameObject RiverTrash, TreeWaterable, Garden, Trough, House, Bridge, IceFloe, Dock, Canoe;
            public GameObject Penguin, Polar, Deer, Fox, Chick, Bee;
            public GameObject[] ForestTrees;
            public Dictionary<TrashType, TrashTypeData> Trash;
        }

        private const string LAST_BUILD_PREF = "EcoGame.LastBuildUtcTicks";
        private static System.DateTime _lastBuildUtc;
        private static readonly List<string> KeptPrefabs = new();

        [MenuItem("EcoGame/Build Main Scene")]
        public static void BuildAll()
        {
            Placeholders.Clear();
            KeptPrefabs.Clear();
            string lastBuild = EditorPrefs.GetString(LAST_BUILD_PREF, "");
            _lastBuildUtc = long.TryParse(lastBuild, out long ticks) ? new System.DateTime(ticks, System.DateTimeKind.Utc) : System.DateTime.MaxValue;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mats = CreateMaterials();
            var envSettings = CreateOrLoad<EnvironmentSettings>(P + "Data/Settings/SO_EnvironmentSettings.asset");
            var playerSettings = CreateOrLoad<EcoPlayerSettings>(P + "Data/Settings/SO_PlayerSettings.asset");
            var quiz = CreateQuiz();
            var buildings = CreateBuildings();
            var prefabs = CreatePrefabs(mats, playerSettings, buildings);

            BuildScene(prefabs, mats, envSettings, quiz);
            WritePlaceholderList();
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(LAST_BUILD_PREF, System.DateTime.UtcNow.Ticks.ToString());
            Debug.Log("EcoGame: Main scene built." +
                (KeptPrefabs.Count > 0 ? " Kept hand-edited prefabs: " + string.Join(", ", KeptPrefabs) : ""));
        }

        // ---------------------------------------------------------------- Materials

        private static Mats CreateMaterials()
        {
            const string ph = P + "Art/Materials/Placeholder/";
            var mats = new Mats
            {
                Red = Mat(ph + "M_Placeholder_Red.mat", new Color(0.8f, 0.25f, 0.2f)),
                Blue = Mat(ph + "M_Placeholder_Blue.mat", new Color(0.25f, 0.45f, 0.85f)),
                Green = Mat(ph + "M_Placeholder_Green.mat", new Color(0.3f, 0.7f, 0.35f)),
                Grey = Mat(ph + "M_Placeholder_Grey.mat", new Color(0.55f, 0.55f, 0.58f)),
                Yellow = Mat(ph + "M_Placeholder_Yellow.mat", new Color(0.95f, 0.8f, 0.25f)),
                Ground = Mat(P + "Art/Materials/Environment/M_Ground.mat", new Color(0.42f, 0.68f, 0.33f)),
                Water = Mat(P + "Art/Materials/Water/M_Water.mat", new Color(0.2f, 0.55f, 0.9f)),
                Hologram = Mat(P + "Art/Materials/Buildings/M_Hologram.mat", new Color(0.3f, 0.9f, 1f, 0.35f), true),
            };

            mats.Smoke = AssetDatabase.LoadAssetAtPath<Material>(P + "Art/Materials/VFX/M_Smoke.mat");
            if (mats.Smoke == null)
            {
                mats.Smoke = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mats.Smoke, P + "Art/Materials/VFX/M_Smoke.mat");
            }
            mats.Smoke.SetTexture("_BaseMap", AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd"));
            MakeTransparent(mats.Smoke);

            mats.Player = Mat(P + "Art/Materials/Props/M_Player.mat", Color.white);
            mats.Player.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(SURVIVOR + "Skins/survivorMaleB.png"));
            EditorUtility.SetDirty(mats.Smoke);
            EditorUtility.SetDirty(mats.Player);
            return mats;
        }

        private static Material Mat(string path, Color color, bool transparent = false)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            if (transparent) MakeTransparent(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        // ---------------------------------------------------------------- Data

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static QuizDatabase CreateQuiz()
        {
            var questions = new (string q, string[] a, int c, string info)[]
            {
                ("Bir cam şişenin doğada çözünmesi yaklaşık ne kadar sürer?",
                    new[] { "10 yıl", "100 yıl", "1.000 yıl", "4.000 yıl ve üzeri" }, 3,
                    "Cam sonsuz kez geri dönüştürülebilir, kalitesi düşmez."),
                ("Plastik bir şişenin doğada çözünmesi yaklaşık ne kadar sürer?",
                    new[] { "1 yıl", "50 yıl", "450 yıl", "10 gün" }, 2,
                    "Plastik tam çözünmez; mikroplastiklere ayrışıp besin zincirine karışır."),
                ("Geri dönüştürülen bir alüminyum kutu ne kadar sürede yeniden rafa dönebilir?",
                    new[] { "Yaklaşık 60 gün", "1 yıl", "5 yıl", "Geri dönüştürülemez" }, 0,
                    "Alüminyum kalitesini kaybetmeden tekrar tekrar geri dönüştürülebilir."),
                ("Organik atıklar çöplükte çürürken hangi sera gazını açığa çıkarır?",
                    new[] { "Oksijen", "Metan", "Azot", "Helyum" }, 1,
                    "Metan ısıyı çok güçlü tutar. Kompost yapmak bu salımı azaltır."),
                ("Aşağıdakilerden hangisi yenilenebilir enerji kaynağıdır?",
                    new[] { "Kömür", "Doğalgaz", "Rüzgar", "Petrol" }, 2,
                    "Rüzgar türbinleri çalışırken sera gazı salmaz."),
                ("Dünyadaki suyun yaklaşık ne kadarı içilebilir tatlı sudur?",
                    new[] { "Yarısı", "Dörtte biri", "Onda biri", "Yüzde birinden az" }, 3,
                    "Suyun çoğu tuzlu okyanus suyudur; tatlı suyun büyük kısmı da buzullarda donmuştur."),
                ("Deniz buzu eridiğinde hangi hayvanın avlanma alanı daralır?",
                    new[] { "Deve", "Kutup ayısı", "Kanguru", "Zebra" }, 1,
                    "Kutup ayıları fok avlamak için deniz buzuna ihtiyaç duyar."),
            };

            var database = CreateOrLoad<QuizDatabase>(P + "Data/Quiz/SO_QuizDatabase.asset");
            database.questions.Clear();
            for (int i = 0; i < questions.Length; i++)
            {
                var data = CreateOrLoad<QuizQuestionData>($"{P}Data/Quiz/SO_Quiz_Q{i + 1:00}.asset");
                data.question = questions[i].q;
                data.answers = questions[i].a;
                data.correctIndex = questions[i].c;
                data.infoNote = questions[i].info;
                EditorUtility.SetDirty(data);
                database.questions.Add(data);
            }

            EditorUtility.SetDirty(database);
            return database;
        }

        private static Dictionary<string, BuildingData> CreateBuildings()
        {
            BuildingData Make(string id, string display, float pollution, float temperature, params (TrashType, int)[] req)
            {
                var data = CreateOrLoad<BuildingData>($"{P}Data/Buildings/SO_Building_{id}.asset");
                data.id = id;
                data.displayName = display;
                data.pollutionDrop = pollution;
                data.temperatureDrop = temperature;
                data.requirements = req.Select(r => new ResourceAmount { type = r.Item1, amount = r.Item2 }).ToArray();
                EditorUtility.SetDirty(data);
                return data;
            }

            return new Dictionary<string, BuildingData>
            {
                // Two of each are built, so the drop is split between them.
                ["SolarPanel"] = Make("SolarPanel", "Güneş Paneli", 8f, 5f, (TrashType.Glass, 10), (TrashType.Metal, 5)),
                ["WindTurbine"] = Make("WindTurbine", "Rüzgar Türbini", 8f, 5f, (TrashType.Metal, 10), (TrashType.Plastic, 5)),
                ["WaterMill"] = Make("WaterMill", "Su Değirmeni", 12f, 8f, (TrashType.Metal, 5), (TrashType.Plastic, 5)),
                ["RiverNet"] = Make("RiverNet", "Nehir Filesi", 0f, 0f, (TrashType.Plastic, 10)),
            };
        }

        // ---------------------------------------------------------------- Prefabs

        private static Prefabs CreatePrefabs(Mats mats, EcoPlayerSettings playerSettings, Dictionary<string, BuildingData> buildings)
        {
            var prefabs = new Prefabs();

            // Player
            {
                var root = NewRoot("PF_Player", out var visual);
                var controller = root.AddComponent<CharacterController>();
                controller.height = 1.8f;
                controller.radius = 0.35f;
                controller.center = new Vector3(0f, 0.9f, 0f);
                var inventory = root.AddComponent<PlayerInventory>();
                Wire(inventory, ("settings", playerSettings));
                Wire(root.AddComponent<PlayerInteractor>(), ("inventory", inventory));
                Wire(root.AddComponent<PlayerController>(), ("settings", playerSettings));

                var animatorController = CreatePlayerAnimator();
                var model = AddModel(visual, SURVIVOR + "Model/characterMedium.fbx", 1.8f, true);
                if (model != null)
                {
                    foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mats.Player;
                    var animator = model.GetComponent<Animator>();
                    if (animator == null) animator = model.AddComponent<Animator>();
                    animator.runtimeAnimatorController = animatorController;
                    animator.applyRootMotion = false;
                }

                var head = Child(root.transform, "Anchor_Head", new Vector3(0f, 1.65f, 0.2f));
                var fp = new GameObject("PF_Camera_FirstPerson");
                fp.transform.SetParent(head, false);
                var fpCamera = fp.AddComponent<Camera>();
                fpCamera.nearClipPlane = 0.05f;
                fpCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                fp.AddComponent<AudioListener>();
                Wire(fp.AddComponent<FirstPersonCameraRig>(), ("settings", playerSettings));
                foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = IGNORE_RAYCAST_LAYER;
                prefabs.Player = SavePrefab(root, "Player");
            }

            // Top-down camera
            {
                var root = new GameObject("PF_Camera_TopDown");
                var cam = root.AddComponent<Camera>();
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                root.AddComponent<AudioListener>();
                Wire(root.AddComponent<TopDownCameraRig>(), ("settings", playerSettings));
                prefabs.TopDownCamera = SavePrefab(root, "Player");
            }

            // Machines
            {
                // Factory machine with a fantasy-kit chimney on top; smoke comes out of the chimney.
                var root = NewRoot("PF_Generator", out var visual);
                var body = AddModel(visual, FACTORY + "machine-fortified.fbx", 2.4f);
                float bodyTop = body != null ? WorldBounds(body).max.y : 1.5f;
                var chimney = AddModel(visual, FANTASY + "chimney.fbx", 1.6f, true, new Vector3(0.6f, bodyTop, 0f));
                float chimneyTop = chimney != null ? WorldBounds(chimney).max.y : bodyTop + 1.6f;
                var anchor = Child(root.transform, "Anchor_SmokeOut", new Vector3(0.6f, chimneyTop, 0f));
                Smoke(anchor, "VFX_GeneratorSmoke", new Color(0.25f, 0.25f, 0.25f, 0.8f), 1.2f, 6f, 1.2f, 3f, mats.Smoke);
                FitBox(root);
                Wire(root.AddComponent<Generator>(), ("smokeAnchor", anchor));
                prefabs.Generator = SavePrefab(root, "Machines");
            }
            {
                // Factory tank with a glass pipe running toward the sea.
                var root = NewRoot("PF_Desalinator", out var visual);
                AddModel(visual, FACTORY + "hopper-high-round.fbx", 3f, true);
                AddModel(visual, FACTORY + "pipe-glass-large-long.fbx", 2.4f, false, new Vector3(-2f, 0f, 0f), 0f, true);
                FitBox(root);
                root.AddComponent<Desalinator>();
                prefabs.Desalinator = SavePrefab(root, "Machines");
            }

            // Build sites
            prefabs.SiteSolar = MakeBuildSite("PF_BuildSite_SolarPanel", buildings["SolarPanel"], mats,
                ModelShape(FACTORY + "screen-panel-wide.fbx", 1.8f, mats.Hologram), null, null, null);
            prefabs.SiteWind = MakeBuildSite("PF_BuildSite_WindTurbine", buildings["WindTurbine"], mats,
                WindTurbineShape(mats.Hologram), null, null, null);
            prefabs.SiteMill = MakeBuildSite("PF_BuildSite_WaterMill", buildings["WaterMill"], mats,
                ModelShape(FANTASY + "watermill.fbx", 5f, mats.Hologram, true), null, null, null);
            prefabs.SiteNet = MakeBuildSite("PF_BuildSite_RiverNet", buildings["RiverNet"], mats, (parent, main, accent) =>
            {
                PH(parent, PrimitiveType.Cylinder, "PH_RiverNet_PostL", new Vector3(-2.3f, 0.6f, 0f), new Vector3(0.15f, 0.6f, 0.15f), accent);
                PH(parent, PrimitiveType.Cylinder, "PH_RiverNet_PostR", new Vector3(2.3f, 0.6f, 0f), new Vector3(0.15f, 0.6f, 0.15f), accent);
                PH(parent, PrimitiveType.Cube, "PH_RiverNet", new Vector3(0f, 0.5f, 0f), new Vector3(4.6f, 0.8f, 0.05f), main);
            }, mats.Yellow, mats.Grey, "Nehir filesi");

            // Trash
            prefabs.Trash = new Dictionary<TrashType, TrashTypeData>
            {
                [TrashType.Glass] = MakeTrash(TrashType.Glass, "Cam", new[] { "wine-red", "wine-white", "glass-wine", "soda-bottle" }, mats),
                [TrashType.Metal] = MakeTrash(TrashType.Metal, "Metal", new[] { "can", "can-small", "can-open", "soda-can", "soda-can-crushed" }, mats),
                [TrashType.Plastic] = MakeTrash(TrashType.Plastic, "Plastik", new[] { "bottle-ketchup", "bottle-musterd", "soda", "cup-coffee", "candy-bar-wrapper" }, mats),
                [TrashType.Organic] = MakeTrash(TrashType.Organic, "Organik", new[] { "apple-half", "banana", "fish-bones", "pizza", "pear-half", "tomato", "watermelon" }, mats),
            };
            {
                var root = NewRoot("PF_RiverTrash", out var visual);
                foreach (var model in new[] { "soda", "bottle-ketchup", "cup-coffee" })
                    AddModel(visual, FOOD + model + ".fbx", 0.6f);
                Trigger(root, new Vector3(0f, 0.3f, 0f), Vector3.one * 0.8f);
                root.AddComponent<RiverTrash>();
                prefabs.RiverTrash = SavePrefab(root, "Trash");
            }
            prefabs.Recycling = MakeRecyclingStation(prefabs.Trash);

            // Trees
            prefabs.TreeWaterable = MakeTree("PF_Tree_Waterable", "tree_default", 4.5f, true);
            prefabs.ForestTrees = new[]
            {
                MakeTree("PF_Tree_Pine", "tree_pineRoundA", 5f, false),
                MakeTree("PF_Tree_Oak", "tree_oak", 5f, false),
                MakeTree("PF_Tree_Detailed", "tree_detailed", 5f, false),
                MakeTree("PF_Tree_Cone", "tree_cone", 5.5f, false),
            };

            // Garden
            {
                var root = NewRoot("PF_Garden", out var visual);
                for (int i = -1; i <= 1; i++)
                    AddModel(visual, GRAVE + "grave.fbx", 2f, false, new Vector3(i * 1.6f, 0f, 0f));
                for (int side = 0; side < 4; side++)
                {
                    bool alongX = side < 2;
                    float sign = side % 2 == 0 ? 1f : -1f;
                    for (int i = -2; i <= 2; i++)
                    {
                        var pos = alongX ? new Vector3(i * 1.1f, 0f, sign * 2.6f) : new Vector3(sign * 3.3f, 0f, i * 1.1f);
                        AddModel(visual, NATURE + "fence_simple.fbx", 1.1f, false, pos, alongX ? 0f : 90f);
                    }
                }

                var flowers = Child(root.transform, "Flowers", Vector3.zero);
                string[] flowerModels = { "flower_redA", "flower_yellowA", "flower_purpleA" };
                int n = 0;
                for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    AddModel(flowers, NATURE + flowerModels[n++ % 3] + ".fbx", 0.7f, true, new Vector3(x * 1.6f, 0.35f, z * 0.6f));
                flowers.gameObject.SetActive(false);

                Trigger(root, new Vector3(0f, 0.75f, 0f), new Vector3(7f, 1.5f, 5.6f));
                var garden = root.AddComponent<Garden>();
                Wire(garden, ("flowers", flowers.gameObject));
                SetBool(garden, "pollutesWhenDry", false);
                prefabs.Garden = SavePrefab(root, "Waterables");
            }

            // Trough
            {
                var root = NewRoot("PF_AnimalTrough", out var visual);
                PH(visual, PrimitiveType.Cube, "PH_AnimalTrough", new Vector3(0f, 0.3f, 0f), new Vector3(2f, 0.6f, 0.8f), mats.Grey);
                PH(visual, PrimitiveType.Cube, "PH_AnimalTrough_Water", new Vector3(0f, 0.58f, 0f), new Vector3(1.8f, 0.08f, 0.6f), mats.Blue);
                Box(root, new Vector3(0f, 0.3f, 0f), new Vector3(2f, 0.6f, 0.8f));
                var troughWater = root.AddComponent<Waterable>();
                SetBool(troughWater, "pollutesWhenDry", false);
                SetString(troughWater, "displayName", "Hayvan Suluğu");
                prefabs.Trough = SavePrefab(root, "Waterables");
                LogPH("PH_AnimalTrough", "Prefabs/Waterables/PF_AnimalTrough", "Hayvan suluğu", "Survival kit 'bucket' veya tahta yalak modeli");
            }

            // Environment pieces
            {
                var root = NewRoot("PF_House", out var visual);
                AddModel(visual, CITY + "building-type-a.fbx", 9f);
                FitBox(root);
                prefabs.House = SavePrefab(root, "Environment");
            }
            {
                // Three survival-kit plank floors in a row along local X; the scene rotates it across the river.
                var root = NewRoot("PF_Bridge", out var visual);
                for (int i = -1; i <= 1; i++)
                    AddModel(visual, SURVIVAL + "floor.fbx", 2.7f, false, new Vector3(i * 2.6f, 0.02f, 0f));
                prefabs.Bridge = SavePrefab(root, "Environment");
            }
            {
                var root = NewRoot("PF_Dock", out var visual);
                AddModel(visual, NATURE + "bridge_woodNarrow.fbx", 12f, false, Vector3.zero, 0f, true);
                prefabs.Dock = SavePrefab(root, "Environment");
            }
            {
                var root = NewRoot("PF_Canoe", out var visual);
                AddModel(visual, NATURE + "canoe.fbx", 3.5f);
                prefabs.Canoe = SavePrefab(root, "Environment");
            }
            {
                var root = NewRoot("PF_IceFloe", out var visual);
                var floor = AddModel(visual, NATURE + "stone_smallFlatA.fbx", 15f);
                AddModel(visual, NATURE + "stone_smallFlatB.fbx", 9f, false, new Vector3(5f, -0.1f, 3f), 40f);
                AddModel(visual, NATURE + "stone_tallA.fbx", 3.5f, true, new Vector3(-5f, 0f, 2.5f));
                AddModel(visual, NATURE + "stone_tallB.fbx", 3f, true, new Vector3(4.5f, 0f, -3.5f));
                AddModel(visual, NATURE + "stone_largeA.fbx", 2f, false, new Vector3(-2.5f, 0f, -4.5f));
                float top = floor != null ? WorldBounds(floor).max.y : 0.5f;
                var surface = Child(root.transform, "Anchor_Surface", new Vector3(0f, top, 0f));
                var ice = root.AddComponent<IceFloeController>();
                Wire(ice, ("surfaceAnchor", surface));
                SetFloat(ice, "baseRadius", 5f);
                prefabs.IceFloe = SavePrefab(root, "Environment");
            }

            // Animals
            prefabs.Penguin = MakeAnimal("PF_Animal_Penguin", "animal-penguin", 0.8f, go => go.AddComponent<IceAnimalHopper>());
            prefabs.Polar = MakeAnimal("PF_Animal_PolarBear", "animal-polar", 1.1f, go => go.AddComponent<IceAnimalHopper>());
            prefabs.Deer = MakeAnimal("PF_Animal_Deer", "animal-deer", 1.2f, go => go.AddComponent<AnimalVisitor>());
            prefabs.Fox = MakeAnimal("PF_Animal_Fox", "animal-fox", 0.8f, go => go.AddComponent<AnimalVisitor>());
            prefabs.Chick = MakeAnimal("PF_Animal_Chick", "animal-chick", 0.5f, go => SetBool(go.AddComponent<AnimalVisitor>(), "flies", true));
            prefabs.Bee = MakeAnimal("PF_Animal_Bee", "animal-bee", 0.3f, null);
            return prefabs;
        }

        /// <summary>Input belt -> machine -> output belt along local X, red floor button in front.</summary>
        private static GameObject MakeRecyclingStation(Dictionary<TrashType, TrashTypeData> trash)
        {
            {
                var bagRoot = new GameObject("PF_MaterialBag");
                AddModel(bagRoot.transform, FACTORY + "box-small.fbx", 0.7f);
                SavePrefab(bagRoot, "Machines");
            }
            var bagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(P + "Prefabs/Machines/PF_MaterialBag.prefab");

            var root = NewRoot("PF_RecyclingMachine", out var visual);
            AddModel(visual, FACTORY + "machine.fbx", 3f);
            var inputBelt = AddModel(visual, FACTORY + "conveyor-long.fbx", 5f, false, new Vector3(-4.2f, 0f, 0f), 0f, true);
            AddModel(visual, FACTORY + "conveyor-long.fbx", 5f, false, new Vector3(4.2f, 0f, 0f), 0f, true);
            AddModel(visual, FACTORY + "button-floor-round.fbx", 0.9f, false, new Vector3(0f, 0f, -2.3f));
            float beltTop = inputBelt != null ? WorldBounds(inputBelt).max.y + 0.15f : 0.6f;

            var beltStart = Child(root.transform, "Anchor_BeltStart", new Vector3(-6.4f, beltTop, 0f));
            var machineIn = Child(root.transform, "Anchor_MachineIn", new Vector3(-1.4f, beltTop, 0f));
            var machineOut = Child(root.transform, "Anchor_MachineOut", new Vector3(1.4f, beltTop, 0f));
            var beltEnd = Child(root.transform, "Anchor_BeltEnd", new Vector3(5.8f, beltTop, 0f));

            Box(root, new Vector3(0f, 1.5f, 0f), new Vector3(3f, 3f, 3f));
            var machine = root.AddComponent<RecyclingMachine>();
            Wire(machine, ("bagPrefab", bagPrefab), ("beltStart", beltStart), ("machineIn", machineIn),
                ("machineOut", machineOut), ("beltEnd", beltEnd));
            WireArray(machine, "trashTypes", TrashTypeExtensions.All.Select(t => (Object)trash[t]).ToArray());

            void Port(string name, RecyclerPortType type, Vector3 position, Vector3 size)
            {
                var port = Child(root.transform, name, position);
                Trigger(port.gameObject, new Vector3(0f, size.y * 0.5f, 0f), size);
                var component = port.gameObject.AddComponent<RecyclerPort>();
                Wire(component, ("machine", machine));
                SetEnum(component, "port", (int)type);
            }

            Port("Port_Input", RecyclerPortType.Input, new Vector3(-6.2f, 0f, 0f), new Vector3(1.6f, 1.5f, 1.6f));
            Port("Port_Button", RecyclerPortType.Button, new Vector3(0f, 0f, -2.3f), new Vector3(1.2f, 1f, 1.2f));
            Port("Port_Output", RecyclerPortType.Output, new Vector3(6f, 0f, 0f), new Vector3(1.6f, 1.5f, 1.6f));
            return SavePrefab(root, "Machines");
        }

        private static GameObject MakeBuildSite(string name, BuildingData data, Mats mats,
            System.Action<Transform, Material, Material> shape, Material main, Material accent, string what)
        {
            var root = NewRoot(name, out var visual);
            var ghost = Child(root.transform, "Ghost", Vector3.zero);
            shape(ghost, mats.Hologram, mats.Hologram);
            var built = Child(root.transform, "Built", Vector3.zero);
            shape(built, main, accent);
            Trigger(root, new Vector3(0f, 1f, 0f), new Vector3(3.5f, 2f, 3.5f));
            Wire(root.AddComponent<BuildSite>(), ("data", data), ("ghostVisual", ghost.gameObject), ("builtVisual", built.gameObject));
            if (what != null)
                LogPH(name.Replace("PF_BuildSite_", "PH_"), "Prefabs/BuildSites/" + name, what, "Hazır " + what.ToLowerInvariant() + " modeli (Ghost ve Built altına)");
            return SavePrefab(root, "BuildSites");
        }

        /// <summary>Build site shape from a real model; the ghost copy gets the hologram material everywhere.</summary>
        private static System.Action<Transform, Material, Material> ModelShape(string path, float height, Material hologram,
            bool alignLongAxisX = false) =>
            (parent, main, accent) =>
            {
                var model = AddModel(parent, path, height, true, Vector3.zero, 0f, alignLongAxisX);
                if (main == hologram) ApplyHologram(model, hologram);
            };

        /// <summary>Stone pillar tower with the fantasy windmill sails standing upright at the top front.</summary>
        private static System.Action<Transform, Material, Material> WindTurbineShape(Material hologram) =>
            (parent, main, accent) =>
            {
                var tower = StretchModel(parent, FANTASY + "pillar-stone.fbx", new Vector3(1.4f, 6f, 1.4f), 0f);
                var sails = AddModel(parent, FANTASY + "windmill.fbx", 6f, true, new Vector3(0f, 3.2f, -0.9f), 0f, true);
                if (main != hologram) return;
                ApplyHologram(tower, hologram);
                ApplyHologram(sails, hologram);
            };

        private static void ApplyHologram(GameObject model, Material hologram)
        {
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = Enumerable.Repeat(hologram, r.sharedMaterials.Length).ToArray();
        }

        private static TrashTypeData MakeTrash(TrashType type, string display, string[] models, Mats mats)
        {
            var root = NewRoot("PF_Trash_" + type, out var visual);
            foreach (var model in models) AddModel(visual, FOOD + model + ".fbx", 0.6f);
            bool organic = type == TrashType.Organic;
            if (organic)
            {
                var vfx = Child(root.transform, "Anchor_Smoke", new Vector3(0f, 0.3f, 0f));
                Smoke(vfx, "VFX_OrganicSmoke", new Color(0.35f, 0.9f, 0.3f, 0.6f), 0.3f, 3f, 0.4f, 2f, mats.Smoke);
            }

            Trigger(root, new Vector3(0f, 0.3f, 0f), Vector3.one * 0.8f);
            var item = root.AddComponent<TrashItem>();
            var data = CreateOrLoad<TrashTypeData>($"{P}Data/Trash/SO_Trash_{type}.asset");
            data.type = type;
            data.displayName = display;
            data.goesToMachine = true;
            Wire(item, ("data", data));
            data.prefab = SavePrefab(root, "Trash");
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject MakeTree(string name, string model, float height, bool waterable)
        {
            var root = NewRoot(name, out var visual);
            AddModel(visual, NATURE + model + ".fbx", height, true);
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.4f;
            capsule.height = height;
            capsule.center = new Vector3(0f, height * 0.5f, 0f);
            if (waterable) root.AddComponent<Waterable>();
            root.AddComponent<TreeColorController>();
            return SavePrefab(root, waterable ? "Waterables" : "Environment");
        }

        private static GameObject MakeAnimal(string name, string model, float size, System.Action<GameObject> addScript)
        {
            var root = NewRoot(name, out var visual);
            AddModel(visual, PETS + model + ".fbx", size);
            addScript?.Invoke(root);
            return SavePrefab(root, "Animals");
        }

        private static AnimatorController CreatePlayerAnimator()
        {
            string modelPath = SURVIVOR + "Model/characterMedium.fbx";
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (modelImporter.animationType != ModelImporterAnimationType.Generic)
            {
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modelImporter.SaveAndReimport();
            }

            var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();

            AnimationClip LoadClip(string clipName)
            {
                string path = SURVIVOR + $"Animations/{clipName}.fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Generic;
                if (avatar != null)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    importer.sourceAvatar = avatar;
                }

                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips) clip.loopTime = true;
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview"));
            }

            var idle = RotationOnly(LoadClip("idle"), P + "Art/Animations/ANIM_Idle.anim");
            var run = RotationOnly(LoadClip("run"), P + "Art/Animations/ANIM_Run.anim");

            string controllerPath = P + "Art/Animations/AC_Player.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            var runState = machine.AddState("Run");
            runState.motion = run;
            machine.defaultState = idleState;

            var toRun = idleState.AddTransition(runState);
            toRun.hasExitTime = false;
            toRun.duration = 0.1f;
            toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var toIdle = runState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            return controller;
        }

        /// <summary>
        /// The survivor animation files use a different unit scale and root axis than the model, so their
        /// position/scale/root curves blow the character up. Keep only bone rotation curves in a looping copy.
        /// </summary>
        private static AnimationClip RotationOnly(AnimationClip source, string path)
        {
            if (source == null) return null;
            var clip = new AnimationClip { frameRate = source.frameRate };
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                if (binding.path.Length == 0) continue; // root axis conversion differs from the model
                if (!binding.propertyName.Contains("Rotation") && !binding.propertyName.Contains("Euler")) continue;
                // The animation files lack the model's "Root" node above the bones.
                var target = binding;
                target.path = "Root/" + binding.path;
                AnimationUtility.SetEditorCurve(clip, target, AnimationUtility.GetEditorCurve(source, binding));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // ---------------------------------------------------------------- Scene

        private static void BuildScene(Prefabs prefabs, Mats mats, EnvironmentSettings envSettings, QuizDatabase quiz)
        {
            Random.InitState(42);

            Separator("---MANAGERS---");
            var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            var environment = new GameObject("EnvironmentManager").AddComponent<EnvironmentManager>();
            Wire(environment, ("settings", envSettings));
            var objectives = new GameObject("ObjectiveManager").AddComponent<ObjectiveManager>();
            var quizManager = new GameObject("QuizManager").AddComponent<QuizManager>();
            var cameraModes = new GameObject("CameraModeManager").AddComponent<CameraModeManager>();

            Separator("---PLAYER---");
            var player = Instantiate(prefabs.Player, W(545f, 490f));
            var topDown = Instantiate(prefabs.TopDownCamera, W(545f, 490f) + new Vector3(0f, 16f, -10f));
            Wire(topDown.GetComponent<TopDownCameraRig>(), ("target", player.transform));
            Wire(player.GetComponent<PlayerController>(), ("cameraModeManager", cameraModes));
            Wire(player.GetComponent<PlayerInteractor>(), ("cameraModeManager", cameraModes));
            Wire(cameraModes,
                ("firstPersonRig", player.GetComponentInChildren<FirstPersonCameraRig>(true)),
                ("topDownRig", topDown.GetComponent<TopDownCameraRig>()),
                ("player", player.transform));

            Separator("---ENVIRONMENT---");
            var waterRenderers = new List<Renderer>();
            BuildTerrain(prefabs, waterRenderers, out float riverEndX);

            var sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sea.name = "Sea";
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.transform.position = new Vector3(-100f, -0.35f, 0f);
            sea.transform.localScale = new Vector3(240f, 0.3f, 400f);
            sea.GetComponent<Renderer>().sharedMaterial = mats.Water;
            waterRenderers.Add(sea.GetComponent<Renderer>());

            // The layout is rotated so the river runs along world X at z = 0, flowing west into the sea.
            var river = new GameObject("River");
            var path = Child(river.transform, "Anchor_RiverPath", Vector3.zero);
            float[] waypointX = { riverEndX, riverEndX * 0.66f, riverEndX * 0.33f, 0f, -8f };
            var waypoints = new Transform[waypointX.Length];
            for (int i = 0; i < waypointX.Length; i++)
                waypoints[i] = Child(path, $"Waypoint_{i + 1:00}", new Vector3(waypointX[i], -0.1f, 0f));
            var waterColor = river.AddComponent<WaterColorController>();
            Wire(waterColor, ("environmentManager", environment));
            WireArray(waterColor, "waterRenderers", waterRenderers.Cast<Object>().ToArray());

            float bridgeX = W(600f, 204f).x;
            Instantiate(prefabs.Bridge, new Vector3(bridgeX, 0f, 0f), 90f, river.transform);

            var mapBounds = Child(river.transform, "MapBounds", Vector3.zero);
            LineBlocker(mapBounds, "Coast", new Vector3(COAST_X + 0.5f, 0f, -200f), new Vector3(COAST_X + 0.5f, 0f, 200f));
            LineBlocker(mapBounds, "Top", W(250f, 0f), W(1150f, 0f));
            LineBlocker(mapBounds, "East", W(1150f, 0f), W(1150f, 190f));
            LineBlocker(mapBounds, "Bottom", W(250f, 650f), W(820f, 650f));
            var roadEdge = RoadWestEdge();
            for (int i = 0; i < roadEdge.Count - 1; i++)
                LineBlocker(mapBounds, "Road_" + i, W(roadEdge[i].x, roadEdge[i].y), W(roadEdge[i + 1].x, roadEdge[i + 1].y));
            LineBlocker(mapBounds, "River_West", new Vector3(-10f, 0f, 0f), new Vector3(bridgeX - 1.4f, 0f, 0f), 2.5f);
            LineBlocker(mapBounds, "River_East", new Vector3(bridgeX + 1.4f, 0f, 0f), new Vector3(riverEndX + 10f, 0f, 0f), 2.5f);

            var house = new GameObject("House");
            Instantiate(prefabs.House, W(640f, 492f), 0f, house.transform);
            var iceFloe = Instantiate(prefabs.IceFloe, W(120f, 340f) + Vector3.down * 0.35f);
            iceFloe.name = "IceFloe";
            Wire(iceFloe.GetComponent<IceFloeController>(), ("environmentManager", environment));
            var props = new GameObject("Props").transform;
            Instantiate(prefabs.Dock, W(380f, 500f) + Vector3.down * 0.15f, 0f, props);
            Instantiate(prefabs.Canoe, W(360f, 540f) + Vector3.down * 0.3f, 20f, props);

            Separator("---INTERACTABLES---");
            var machines = new GameObject("Machines").transform;
            var genHouse = Instantiate(prefabs.Generator, W(537f, 437f), 0f, machines, "PF_Generator_House");
            var genDesal = Instantiate(prefabs.Generator, W(452f, 392f), 0f, machines, "PF_Generator_Desalinator");
            var genRecycle = Instantiate(prefabs.Generator, W(800f, 330f), 0f, machines, "PF_Generator_Recycling");
            var recycling = Instantiate(prefabs.Recycling, W(775f, 261f), 0f, machines);
            Wire(recycling.GetComponent<RecyclingMachine>(), ("quizManager", quizManager));
            Instantiate(prefabs.Desalinator, W(418f, 392f), 0f, machines);

            var sitesRoot = new GameObject("BuildSites").transform;
            var solar = Instantiate(prefabs.SiteSolar, W(524f, 551f), 0f, sitesRoot).GetComponent<BuildSite>();
            var solar2 = Instantiate(prefabs.SiteSolar, W(585f, 610f), 0f, sitesRoot).GetComponent<BuildSite>();
            var wind = Instantiate(prefabs.SiteWind, W(665f, 290f), 0f, sitesRoot).GetComponent<BuildSite>();
            var wind2 = Instantiate(prefabs.SiteWind, W(612f, 318f), 0f, sitesRoot).GetComponent<BuildSite>();
            // Wheel plane runs along the river (world X) and dips into the south bank edge.
            var mill = Instantiate(prefabs.SiteMill, new Vector3(W(455f, 282f).x, 0f, -2.6f), 0f, sitesRoot).GetComponent<BuildSite>();
            var net = Instantiate(prefabs.SiteNet, new Vector3(W(778f, 151f).x, 0f, 0f), 90f, sitesRoot).GetComponent<BuildSite>();
            NameGenerator(genHouse, "Jeneratör 1", "Evi elektrikle besliyor.", "2 Güneş Paneli kurulunca kapanır.", 2);
            NameGenerator(genDesal, "Jeneratör 2", "Su arıtma cihazını çalıştırıyor.", "Su Değirmeni kurulunca kapanır.", 1);
            NameGenerator(genRecycle, "Jeneratör 3", "Geri dönüşüm makinesini çalıştırıyor.", "2 Rüzgar Türbini kurulunca kapanır.", 2);
            foreach (var site in new[] { solar, solar2 }) Wire(site, ("linkedGenerator", genHouse.GetComponent<Generator>()));
            foreach (var site in new[] { wind, wind2 }) Wire(site, ("linkedGenerator", genRecycle.GetComponent<Generator>()));
            Wire(mill, ("linkedGenerator", genDesal.GetComponent<Generator>()));

            var waterables = new GameObject("Waterables").transform;
            var garden = Instantiate(prefabs.Garden, W(672f, 375f), 0f, waterables).GetComponent<Garden>();
            var trough = Instantiate(prefabs.Trough, W(520f, 165f), 0f, waterables).GetComponent<Waterable>();
            var frontTrees = new GameObject("Trees_Waterable").transform;
            frontTrees.SetParent(waterables, false);
            foreach (var p in new[] { new Vector2(470f, 75f), new Vector2(540f, 65f), new Vector2(610f, 75f), new Vector2(430f, 140f) })
                Instantiate(prefabs.TreeWaterable, W(p.x, p.y), Random.Range(0f, 360f), frontTrees);

            Separator("---TRASH---");
            var spawner = new GameObject("TrashSpawner").AddComponent<TrashSpawner>();
            var areas = new[]
            {
                SpawnArea(spawner.transform, 1, 860f, 200f, 70f, 35f),
                SpawnArea(spawner.transform, 2, 840f, 400f, 50f, 45f),
                SpawnArea(spawner.transform, 3, 760f, 560f, 45f, 55f),
                SpawnArea(spawner.transform, 4, 700f, 110f, 70f, 30f),
            };
            var spawnerSo = new SerializedObject(spawner);
            var table = spawnerSo.FindProperty("spawnTable");
            var counts = new Dictionary<TrashType, int>
            {
                [TrashType.Metal] = 24, [TrashType.Plastic] = 24, [TrashType.Glass] = 14, [TrashType.Organic] = 8,
            };
            table.arraySize = counts.Count;
            int index = 0;
            foreach (var pair in counts)
            {
                var element = table.GetArrayElementAtIndex(index++);
                element.FindPropertyRelative("data").objectReferenceValue = prefabs.Trash[pair.Key];
                element.FindPropertyRelative("count").intValue = pair.Value;
            }
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();
            WireArray(spawner, "spawnAreas", areas.Cast<Object>().ToArray());

            var flow = new GameObject("RiverTrashFlow").AddComponent<RiverTrashFlow>();
            Wire(flow, ("riverNet", net), ("riverTrashPrefab", prefabs.RiverTrash));
            WireArray(flow, "waypoints", waypoints.Cast<Object>().ToArray());

            Separator("---ANIMALS---");
            var iceAnimals = new GameObject("IceAnimals").transform;
            var iceController = iceFloe.GetComponent<IceFloeController>();
            foreach (var prefab in new[] { prefabs.Penguin, prefabs.Penguin, prefabs.Polar, prefabs.Polar })
            {
                var animal = Instantiate(prefab, iceFloe.transform.position, Random.Range(0f, 360f), iceAnimals);
                Wire(animal.GetComponent<IceAnimalHopper>(), ("ice", iceController));
            }

            var visitors = new GameObject("AnimalVisitorSpawner").AddComponent<AnimalVisitorSpawner>();
            visitors.transform.position = W(520f, -90f);
            Wire(visitors, ("spawnPoint", visitors.transform), ("trough", trough));
            WireArray(visitors, "visitorPrefabs", prefabs.Deer, prefabs.Fox, prefabs.Chick);

            var bees = new GameObject("BeeSwarm_Garden").AddComponent<BeeSwarm>();
            Wire(bees, ("garden", garden), ("beePrefab", prefabs.Bee));

            Separator("---VFX---");
            var fog = new GameObject("Fog").AddComponent<FogController>();
            Wire(fog, ("environmentManager", environment));

            Separator("---LIGHTING---");
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(P + "Settings/SampleSceneProfile.asset");

            Separator("---UI---");
            var ui = BuildUI(gameManager, environment, objectives, player);
            Wire(ui.Markers, ("cameraModeManager", cameraModes), ("garden", garden));

            Wire(gameManager, ("environmentManager", environment), ("objectiveManager", objectives),
                ("uiManager", ui.Manager), ("cameraModeManager", cameraModes));
            Wire(objectives, ("garden", garden), ("trashSpawner", spawner), ("environmentManager", environment));
            WireArray(objectives, "buildSites", solar, solar2, wind, wind2, mill, net);
            Wire(quizManager, ("database", quiz), ("quizPanel", ui.Quiz), ("gameManager", gameManager));

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), P + "Scenes/Main.unity");
        }

        // ---------------------------------------------------------------- UI

        private struct UIRefs
        {
            public UIManager Manager;
            public QuizPanel Quiz;
            public ObjectiveMarkers Markers;
        }

        private const string UI_PACK = TP + "kenney_ui-pack-rpg-expansion/PNG/";
        private static readonly Color Ink = new(0.25f, 0.16f, 0.09f);
        private static readonly Color Dim = new(0f, 0f, 0f, 0.55f);

        private static Font UIFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static UIRefs BuildUI(GameManager gameManager, EnvironmentManager environment, ObjectiveManager objectives, GameObject player)
        {
            var hud = MakeCanvas("Canvas_HUD", 0);
            Vector2 topLeft = new(0f, 1f), topRight = new(1f, 1f), topCenter = new(0.5f, 1f);
            Vector2 bottomLeft = Vector2.zero, bottomCenter = new(0.5f, 0f), center = new(0.5f, 0.5f);

            // Metrics (top right)
            var metrics = Panel("Panel_Metrics", hud, topRight, topRight, new Vector2(-20f, -20f), new Vector2(460f, 200f), Spr("panel_brown", 16));
            var metricFills = new RectTransform[3];
            var metricTexts = new Text[3];
            string[] metricNames = { "Pollution", "Temperature", "WaterPollution" };
            string[] metricSprites = { "barRed_horizontalMid", "barYellow_horizontalMid", "barBlue_horizontalBlue" };
            for (int i = 0; i < 3; i++)
            {
                metricTexts[i] = Txt("Txt_" + metricNames[i], metrics, topLeft, topRight, new Vector2(0f, -28f - i * 56f), new Vector2(-50f, 26f), 21, TextAnchor.MiddleLeft);
                metricFills[i] = SpriteBar("Bar_" + metricNames[i], metrics, new Vector2(0f, -52f - i * 56f), new Vector2(-50f, 18f), metricSprites[i]);
            }

            var objectivePanel = Panel("Panel_Objective", hud, topCenter, topCenter, new Vector2(0f, -20f), new Vector2(920f, 54f), Spr("panel_brown", 16));
            var objective = Txt("Txt_Objective", objectivePanel, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f), 22, TextAnchor.MiddleCenter);

            // Pickup feed (top left)
            var markersRect = Rect("Panel_ObjectiveMarkers", hud, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            markersRect.SetAsFirstSibling();
            var markers = markersRect.gameObject.AddComponent<ObjectiveMarkers>();
            Wire(markers, ("arrowSprite", Spr("arrowBeige_right")));

            var feed = Rect("Panel_Feed", hud, topLeft, topLeft, new Vector2(24f, -20f), new Vector2(520f, 200f));
            var feedLines = new Text[5];
            for (int i = 0; i < feedLines.Length; i++)
                feedLines[i] = Txt("Txt_Feed_" + (i + 1), feed, topLeft, topLeft, new Vector2(0f, -i * 36f), new Vector2(520f, 34f), 22, TextAnchor.MiddleLeft, null, true);

            // Inventory (bottom left)
            var inventory = Panel("Panel_Inventory", hud, bottomLeft, bottomLeft, new Vector2(20f, 20f), new Vector2(640f, 260f), Spr("panel_brown", 16));
            var bagText = Txt("Txt_Bag", inventory, topLeft, topRight, new Vector2(0f, -26f), new Vector2(-50f, 26f), 21, TextAnchor.MiddleLeft);
            var bagFill = SpriteBar("Bar_Bag", inventory, new Vector2(0f, -50f), new Vector2(-50f, 18f), "barGreen_horizontalMid");
            var trashCounts = SlotRow(inventory, "Trash", "Çöp", -90f, "iconCircle_grey", false);
            var bagCounts = SlotRow(inventory, "Bags", "Torba", -140f, "buttonSquare_grey", true);
            var waterText = Txt("Txt_Water", inventory, topLeft, topRight, new Vector2(0f, -192f), new Vector2(-50f, 26f), 21, TextAnchor.MiddleLeft);
            var waterFill = SpriteBar("Bar_Water", inventory, new Vector2(0f, -216f), new Vector2(-50f, 18f), "barBlue_horizontalBlue");

            // Info + prompt (bottom center)
            var info = Panel("Panel_Info", hud, bottomCenter, bottomCenter, new Vector2(0f, 20f), new Vector2(680f, 170f), Spr("panel_beige", 16));
            var infoTitle = Txt("Txt_InfoTitle", info, topLeft, topRight, new Vector2(0f, -28f), new Vector2(-40f, 32f), 26, TextAnchor.MiddleCenter, Ink);
            infoTitle.fontStyle = FontStyle.Bold;
            var infoStatus = Txt("Txt_InfoStatus", info, topLeft, topRight, new Vector2(0f, -82f), new Vector2(-50f, 74f), 20, TextAnchor.MiddleCenter, Ink);
            var infoBarFill = SpriteBar("Bar_Info", info, new Vector2(0f, 0f), new Vector2(-80f, 18f), "barGreen_horizontalMid", new Vector2(0f, 0f), new Vector2(1f, 0f), 26f);
            var prompt = Panel("Panel_Prompt", hud, bottomCenter, bottomCenter, new Vector2(0f, 200f), new Vector2(460f, 58f), Spr("buttonLong_brown", 12));
            var promptText = Txt("Txt_InteractPrompt", prompt, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f), 24, TextAnchor.MiddleCenter);

            var crosshair = Txt("Img_Crosshair", hud, center, center, Vector2.zero, new Vector2(40f, 40f), 30, TextAnchor.MiddleCenter, Color.white, true);
            crosshair.text = "+";
            var message = Txt("Txt_Message", hud, center, center, new Vector2(0f, 200f), new Vector2(1100f, 60f), 30, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f), true);
            Txt("Txt_Controls", hud, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(760f, 30f), 18, TextAnchor.LowerRight, Color.white, true)
                .text = "WASD hareket  •  Space zıpla  •  E etkileşim  •  V kamera  •  Esc duraklat";

            // Quiz
            var modalCanvas = MakeCanvas("Canvas_Quiz", 10);
            var quizPanel = Img("Panel_Quiz", modalCanvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            var box = Panel("Panel_QuizBox", quizPanel, center, center, Vector2.zero, new Vector2(1000f, 720f), Spr("panel_beige", 16));
            var header = Txt("Txt_Header", box, topCenter, topCenter, new Vector2(0f, -24f), new Vector2(920f, 30f), 22, TextAnchor.MiddleCenter, Ink);
            var question = Txt("Txt_Question", box, topCenter, topCenter, new Vector2(0f, -60f), new Vector2(900f, 110f), 30, TextAnchor.MiddleCenter, Ink);
            question.fontStyle = FontStyle.Bold;
            var answers = new Button[4];
            for (int i = 0; i < 4; i++)
                answers[i] = SpriteBtn("Btn_Answer" + (char)('A' + i), box, topCenter, new Vector2(0f, -185f - i * 82f), new Vector2(820f, 68f), "", 25, "buttonLong_beige", Ink);
            var feedback = Txt("Txt_Feedback", box, bottomCenter, bottomCenter, new Vector2(0f, 110f), new Vector2(900f, 90f), 22, TextAnchor.MiddleCenter, Ink);
            var cont = SpriteBtn("Btn_Continue", box, bottomCenter, new Vector2(0f, 30f), new Vector2(280f, 62f), "Devam", 26, "buttonLong_brown", Color.white);
            var quiz = quizPanel.gameObject.AddComponent<QuizPanel>();
            Wire(quiz, ("txtHeader", header), ("txtQuestion", question), ("txtFeedback", feedback), ("btnContinue", cont));
            WireArray(quiz, "answerButtons", answers.Cast<Object>().ToArray());
            quizPanel.gameObject.SetActive(false);


            // End screens
            var endCanvas = MakeCanvas("Canvas_EndScreens", 20);
            var win = EndPanel("Panel_Win", endCanvas, "Tebrikler! Doğayı kurtardın.", "Btn_RestartWin", out var restartWin);
            var lose = EndPanel("Panel_Lose", endCanvas, "Kaybettin! Dünya çok kirlendi ya da çok ısındı.", "Btn_RestartLose", out var restartLose);
            var pause = Img("Panel_Pause", endCanvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            var pauseBox = Panel("Panel_PauseBox", pause, center, center, Vector2.zero, new Vector2(600f, 220f), Spr("panel_brown", 16));
            Txt("Txt_Pause", pauseBox, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-40f, -40f), 44, TextAnchor.MiddleCenter)
                .text = "Duraklatıldı\n<size=24>Devam etmek için Esc</size>";
            win.gameObject.SetActive(false);
            lose.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            var manager = hud.gameObject.AddComponent<UIManager>();
            Wire(manager,
                ("gameManager", gameManager), ("environmentManager", environment), ("objectiveManager", objectives),
                ("inventory", player.GetComponent<PlayerInventory>()), ("interactor", player.GetComponent<PlayerInteractor>()),
                ("barPollutionFill", metricFills[0]), ("barTemperatureFill", metricFills[1]), ("barWaterPollutionFill", metricFills[2]),
                ("txtPollution", metricTexts[0]), ("txtTemperature", metricTexts[1]), ("txtWaterPollution", metricTexts[2]),
                ("barBagFill", bagFill), ("txtBag", bagText), ("barWaterFill", waterFill), ("txtWater", waterText),
                ("panelInfo", info.gameObject), ("txtInfoTitle", infoTitle), ("txtInfoStatus", infoStatus),
                ("infoBar", infoBarFill.parent.gameObject), ("infoBarFill", infoBarFill),
                ("panelPrompt", prompt.gameObject), ("txtInteractPrompt", promptText),
                ("txtMessage", message), ("txtObjective", objective), ("crosshair", crosshair.gameObject),
                ("panelWin", win.gameObject), ("panelLose", lose.gameObject), ("panelPause", pause.gameObject),
                ("btnRestartWin", restartWin), ("btnRestartLose", restartLose));
            WireArray(manager, "trashCounts", trashCounts.Cast<Object>().ToArray());
            WireArray(manager, "bagCounts", bagCounts.Cast<Object>().ToArray());
            WireArray(manager, "feedLines", feedLines.Cast<Object>().ToArray());

            return new UIRefs { Manager = manager, Quiz = quiz, Markers = markers };
        }

        /// <summary>Label plus one tinted icon and count per trash type.</summary>
        private static Text[] SlotRow(Transform parent, string name, string label, float y, string iconSprite, bool bagColors)
        {
            Vector2 topLeft = new(0f, 1f);
            Txt("Txt_" + name + "Label", parent, topLeft, topLeft, new Vector2(25f, y), new Vector2(80f, 34f), 21, TextAnchor.MiddleLeft);
            var counts = new Text[TrashTypeExtensions.Count];
            foreach (var type in TrashTypeExtensions.All)
            {
                float x = 100f + (int)type * 126f;
                ColorUtility.TryParseHtmlString(type.BagColorHex(), out var color);
                var icon = Panel($"Img_{name}_{type}", parent, topLeft, topLeft, new Vector2(x, y), new Vector2(28f, 28f), Spr(iconSprite, bagColors ? 12 : 0), color);
                icon.GetComponent<Image>().type = Image.Type.Simple;
                counts[(int)type] = Txt($"Txt_{name}_{type}", parent, topLeft, topLeft, new Vector2(x + 32f, y), new Vector2(94f, 30f), 18, TextAnchor.MiddleLeft, color);
            }
            return counts;
        }

        private static Sprite Spr(string name, int border = 0)
        {
            string path = UI_PACK + name + ".png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogWarning("EcoGame: UI sprite not found " + path);
                return null;
            }

            var spriteBorder = new Vector4(border, border, border, border);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != spriteBorder)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = spriteBorder;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RectTransform MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Img(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color) =>
            Panel(name, parent, anchorMin, anchorMax, position, size, null, color);

        private static RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            Sprite sprite, Color? tint = null)
        {
            var rect = Rect(name, parent, anchorMin, anchorMax, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = tint ?? Color.white;
            return rect;
        }

        private static Text Txt(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            int fontSize, TextAnchor alignment, Color? color = null, bool outline = false)
        {
            var rect = Rect(name, parent, anchorMin, anchorMax, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = UIFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color ?? Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline) rect.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            return text;
        }

        private static Button SpriteBtn(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
            string label, int fontSize, string sprite, Color textColor)
        {
            var rect = Panel(name, parent, anchor, anchor, position, size, Spr(sprite, 12));
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            Txt("Txt_Label", rect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f), fontSize, TextAnchor.MiddleCenter, textColor).text = label;
            return button;
        }

        /// <summary>Bar background stretched across the parent's top (or given anchors); returns the fill rect.</summary>
        private static RectTransform SpriteBar(string name, Transform parent, Vector2 position, Vector2 size, string fillSprite,
            Vector2? anchorMin = null, Vector2? anchorMax = null, float bottomOffset = 0f)
        {
            var back = Panel(name, parent, anchorMin ?? new Vector2(0f, 1f), anchorMax ?? new Vector2(1f, 1f),
                position + new Vector2(0f, bottomOffset), size, Spr("barBack_horizontalMid"));
            var fill = Panel("Fill", back, Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Spr(fillSprite));
            fill.pivot = Vector2.zero;
            return fill;
        }

        private static RectTransform EndPanel(string name, Transform parent, string title, string buttonName, out Button restart)
        {
            var panel = Img(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            var center = new Vector2(0.5f, 0.5f);
            var box = Panel(name + "Box", panel, center, center, Vector2.zero, new Vector2(1100f, 340f), Spr("panel_brown", 16));
            Txt("Txt_Title", box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 140f), 48, TextAnchor.MiddleCenter)
                .text = title;
            restart = SpriteBtn(buttonName, box, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 76f), "Yeniden Başlat", 30, "buttonLong_beige", Ink);
            return panel;
        }

        // ---------------------------------------------------------------- Helpers

        private static GameObject NewRoot(string name, out Transform visual)
        {
            var root = new GameObject(name);
            visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            return root;
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

/// <summary>
        /// Saves the generated prefab, unless the existing file was edited by hand after the last build:
        /// then the hand-edited prefab is kept and used as is.
        /// </summary>
        private static GameObject SavePrefab(GameObject root, string folder)
        {
            string path = $"{P}Prefabs/{folder}/{root.name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && File.GetLastWriteTimeUtc(path) > _lastBuildUtc)
            {
                Object.DestroyImmediate(root);
                KeptPrefabs.Add(existing.name);
                return existing;
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject PH(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Instantiates a model, scales it to the given size, and puts its base center at the parent origin plus offset.</summary>
        private static GameObject AddModel(Transform parent, string path, float size, bool byHeight = false,
            Vector3 offset = default, float yaw = 0f, bool alignLongAxisX = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("EcoGame: model not found " + path);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            var bounds = WorldBounds(go);
            if (alignLongAxisX && bounds.size.z > bounds.size.x)
            {
                go.transform.localRotation *= Quaternion.Euler(0f, 90f, 0f);
                bounds = WorldBounds(go);
            }

            float dimension = byHeight ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (dimension > 0.0001f) go.transform.localScale = Vector3.one * (size / dimension);

            bounds = WorldBounds(go);
            Vector3 anchor = parent.TransformPoint(offset);
            go.transform.position += new Vector3(anchor.x - bounds.center.x, anchor.y - bounds.min.y, anchor.z - bounds.center.z);
            return go;
        }

/// <summary>
        /// Non-uniformly scales a model to an exact box (x = long side) with its base at baseHeight.
        /// The model's longer horizontal axis is turned to X first.
        /// </summary>
        private static GameObject StretchModel(Transform parent, string path, Vector3 size, float baseHeight)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("EcoGame: model not found " + path);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var bounds = WorldBounds(go);
            bool longAlongZ = bounds.size.z > bounds.size.x;
            float sizeX = Mathf.Max(bounds.size.x, 0.0001f), sizeY = Mathf.Max(bounds.size.y, 0.0001f), sizeZ = Mathf.Max(bounds.size.z, 0.0001f);
            go.transform.localScale = longAlongZ
                ? new Vector3(size.z / sizeX, size.y / sizeY, size.x / sizeZ)
                : new Vector3(size.x / sizeX, size.y / sizeY, size.z / sizeZ);
            if (longAlongZ) go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            bounds = WorldBounds(go);
            Vector3 anchor = parent.position;
            go.transform.position += new Vector3(anchor.x - bounds.center.x, anchor.y + baseHeight - bounds.min.y, anchor.z - bounds.center.z);
            return go;
        }

        private static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var bounds = RendererBounds(renderers[0]);
            foreach (var r in renderers) bounds.Encapsulate(RendererBounds(r));
            return bounds;
        }

        /// <summary>Skinned bounds are unreliable in edit mode, so bake the posed mesh and measure that.</summary>
        private static Bounds RendererBounds(Renderer renderer)
        {
            if (renderer is not SkinnedMeshRenderer skinned || skinned.sharedMesh == null) return renderer.bounds;
            var baked = new Mesh();
            skinned.BakeMesh(baked);
            var local = baked.bounds;
            Object.DestroyImmediate(baked);
            var matrix = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
            var result = new Bounds(matrix.MultiplyPoint3x4(local.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                result.Encapsulate(matrix.MultiplyPoint3x4(corner));
            }
            return result;
        }

        private static void Box(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        private static void FitBox(GameObject root)
        {
            var bounds = WorldBounds(root);
            Box(root, root.transform.InverseTransformPoint(bounds.center), bounds.size);
        }

        private static void Blocker(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }

/// <summary>Box version of an SVG ellipse, aligned to the SVG axes.</summary>
        private static BoxCollider SpawnArea(Transform parent, int index, float sx, float sy, float rx, float ry)
        {
            var go = new GameObject($"SpawnArea_{index:00}");
            go.transform.SetParent(parent, false);
            Vector3 center = W(sx, sy);
            go.transform.SetPositionAndRotation(center, Quaternion.FromToRotation(Vector3.right, (W(sx + 1f, sy) - center).normalized));
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 0.5f, 0f);
            box.size = new Vector3(rx * 2f * SVG_SCALE, 1f, ry * 2f * SVG_SCALE);
            return box;
        }

        private static void LineBlocker(Transform parent, string name, Vector3 a, Vector3 b, float thickness = 1f)
        {
            var go = new GameObject("Blocker_" + name) { layer = IGNORE_RAYCAST_LAYER };
            go.transform.SetParent(parent, false);
            a.y = b.y = 0f;
            go.transform.SetPositionAndRotation((a + b) * 0.5f + Vector3.up, Quaternion.LookRotation(b - a));
            go.AddComponent<BoxCollider>().size = new Vector3(thickness, 3f, Vector3.Distance(a, b));
        }

        // ---------------------------------------------------------------- SVG layout (Harita_Yerlesim.svg)

        private const float SVG_SCALE = 0.13f;
        private const float CELL = 5f;
        private const float COAST_X = -7.5f;
        private static readonly Vector2 SvgRiverMouth = new(398f, 254f);
        private static readonly float RiverAngle = Mathf.Atan2(194f, 752f);

        /// <summary>SVG point to world. Rotated so the river is the world X axis.</summary>
        private static Vector3 W(float sx, float sy)
        {
            float x = (sx - SvgRiverMouth.x) * SVG_SCALE;
            float z = -(sy - SvgRiverMouth.y) * SVG_SCALE;
            float c = Mathf.Cos(RiverAngle), s = Mathf.Sin(RiverAngle);
            return new Vector3(x * c + z * s, 0f, -x * s + z * c);
        }

        private static Vector2 S(Vector3 world)
        {
            float c = Mathf.Cos(RiverAngle), s = Mathf.Sin(RiverAngle);
            float x = world.x * c - world.z * s;
            float z = world.x * s + world.z * c;
            return new Vector2(x / SVG_SCALE + SvgRiverMouth.x, -z / SVG_SCALE + SvgRiverMouth.y);
        }

        private static List<Vector2> Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, int steps)
        {
            var points = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                points.Add(u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d);
            }
            return points;
        }

        private static List<Vector2> RoadWestEdge() =>
            Bezier(new Vector2(1150f, 190f), new Vector2(1000f, 260f), new Vector2(880f, 420f), new Vector2(810f, 650f), 10);

        private static List<Vector2> RoadPolygon()
        {
            var polygon = RoadWestEdge();
            polygon.Add(new Vector2(810f, 800f));
            polygon.Add(new Vector2(950f, 800f));
            polygon.AddRange(Bezier(new Vector2(950f, 650f), new Vector2(1010f, 470f), new Vector2(1080f, 360f), new Vector2(1150f, 300f), 10));
            polygon.Add(new Vector2(1400f, 250f));
            polygon.Add(new Vector2(1400f, 190f));
            return polygon;
        }

        private static List<Vector2> BeyondRoadPolygon()
        {
            var polygon = RoadWestEdge();
            polygon.Add(new Vector2(810f, 800f));
            polygon.Add(new Vector2(1400f, 800f));
            polygon.Add(new Vector2(1400f, 190f));
            return polygon;
        }

        private static bool Inside(Vector2 point, List<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>Nature kit tiles on a grid; river row at z = 0, road tiles on the road, trees outside the playable area.</summary>
        private static void BuildTerrain(Prefabs prefabs, List<Renderer> waterRenderers, out float riverEndX)
        {
            string grass = NATURE + "ground_grass.fbx";
            string riverTile = NATURE + "ground_riverStraight.fbx";

            var terrain = new GameObject("Terrain").transform;
            var forest = new GameObject("Forest").transform;

            float riverYaw = RiverTileYaw(riverTile);

            var road = RoadPolygon();
            var beyondRoad = BeyondRoadPolygon();
            riverEndX = W(1150f, 60f).x;

            Vector3 min = Vector3.one * float.MaxValue, max = Vector3.one * float.MinValue;
            foreach (var corner in new[] { new Vector2(-100f, -180f), new Vector2(1300f, -180f), new Vector2(-100f, 780f), new Vector2(1300f, 780f) })
            {
                Vector3 w = W(corner.x, corner.y);
                min = Vector3.Min(min, w);
                max = Vector3.Max(max, w);
            }

            for (int i = Mathf.FloorToInt(min.x / CELL); i <= Mathf.CeilToInt(max.x / CELL); i++)
            for (int j = Mathf.FloorToInt(min.z / CELL); j <= Mathf.CeilToInt(max.z / CELL); j++)
            {
                var center = new Vector3(i * CELL, 0f, j * CELL);
                if (center.x < COAST_X + CELL * 0.5f) continue;
                Vector2 s = S(center);

                bool isRiver = j == 0;
                bool isRoad = !isRiver && Inside(s, road);
                AddTile(terrain, isRiver ? riverTile : grass, center, isRiver ? riverYaw : 0f, isRiver ? waterRenderers : null);

                bool playable = s.x <= 1150f && s.y >= 0f && s.y <= 650f && !Inside(s, beyondRoad);
                bool margin = !playable && !isRiver && !isRoad;
                bool edgeWoods = playable && !isRiver && s.y < 30f && Random.value < 0.35f;
                if ((margin && Random.value < 0.6f) || edgeWoods)
                {
                    var offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                    Instantiate(prefabs.ForestTrees[Random.Range(0, prefabs.ForestTrees.Length)], center + offset, Random.Range(0f, 360f), forest);
                }
            }

            var ground = new GameObject("GroundCollider").AddComponent<BoxCollider>();
            ground.transform.SetParent(terrain, false);
            ground.center = new Vector3((min.x + max.x) * 0.5f, -0.5f, (min.z + max.z) * 0.5f);
            ground.size = new Vector3(max.x - min.x, 1f, max.z - min.z);
        }

/// <summary>Scales a ground tile to one cell footprint and puts its top surface at y = 0.</summary>
        private static void AddTile(Transform parent, string path, Vector3 center, float yaw, List<Renderer> collect)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var bounds = WorldBounds(go);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.0001f) go.transform.localScale = Vector3.one * (CELL / footprint);
            bounds = WorldBounds(go);
            go.transform.position += new Vector3(center.x - bounds.center.x, -bounds.max.y, center.z - bounds.center.z);
            collect?.AddRange(go.GetComponentsInChildren<Renderer>());
        }

        /// <summary>0 if the tile's water runs along X, otherwise 90.</summary>
        private static float RiverTileYaw(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return 0f;
            foreach (var filter in asset.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
                for (int k = 0; k < materials.Length && k < mesh.subMeshCount; k++)
                {
                    if (materials[k] == null || !materials[k].name.ToLowerInvariant().Contains("water")) continue;
                    Vector3 size = filter.transform.TransformVector(mesh.GetSubMesh(k).bounds.size);
                    return Mathf.Abs(size.x) >= Mathf.Abs(size.z) ? 0f : 90f;
                }
            }

            Debug.LogWarning("EcoGame: no water material on river tile, orientation guessed.");
            return 0f;
        }

        private static void Smoke(Transform parent, string name, Color color, float size, float rate, float speed, float lifetime, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.1f;
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

        private static void Separator(string name) => new GameObject(name) { tag = "EditorOnly" };

        private static GameObject Instantiate(GameObject prefab, Vector3 position, float yaw = 0f, Transform parent = null, string name = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (name != null) go.name = name;
            return go;
        }

        private static void Wire(Object target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in pairs)
            {
                var property = so.FindProperty(field);
                if (property == null) Debug.LogError($"EcoGame: field '{field}' not found on {target.GetType().Name}");
                else property.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"EcoGame: field '{field}' not found on {target.GetType().Name}");
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Trigger(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = center;
            box.size = size;
        }

        private static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NameGenerator(GameObject generator, string title, string powers, string hint, int sitesToShutdown)
        {
            var component = generator.GetComponent<Generator>();
            var so = new SerializedObject(component);
            so.FindProperty("sitesToShutdown").intValue = sitesToShutdown;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetString(component, "displayName", title);
            SetString(component, "powers", powers);
            SetString(component, "shutdownHint", hint);
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LogPH(string name, string prefabPath, string what, string suggestion) =>
            Placeholders.Add($"| {name} | {prefabPath} | {what} | {suggestion} |");

        private static void WritePlaceholderList()
        {
            var builder = new StringBuilder()
                .AppendLine("# Placeholder List")
                .AppendLine()
                .AppendLine("Blockout (`PH_`) olarak yapılmış ve gerçek asset bekleyen her obje burada listelenir.")
                .AppendLine("Gerçek model takıldığında ilgili satır silinir (bkz. proje dokümanı Bölüm 4.2).")
                .AppendLine()
                .AppendLine("| PH_ adı | Prefab yolu | Neyi temsil ediyor | Önerilen gerçek asset |")
                .AppendLine("|---|---|---|---|");
            foreach (var line in Placeholders) builder.AppendLine(line);
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Docs", "PlaceholderList.md");
            File.WriteAllText(path, builder.ToString());
        }
    }
}
