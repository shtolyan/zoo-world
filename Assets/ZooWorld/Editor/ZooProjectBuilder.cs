using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZooWorld.Domain;

namespace ZooWorld.Editor
{
    /// <summary>Reproducible, Editor-only demo authoring. Generated content remains ordinary editable assets.</summary>
    public static class ZooProjectBuilder
    {
        public const string ScenePath = "Assets/ZooWorld/Scenes/ZooWorld.unity";
        private const string Root = "Assets/ZooWorld";
        private static readonly Color Ink = Hex("102D26"), Cream = Hex("F4F1DC"), Muted = Hex("A3B9A9");
        private static readonly Color Lime = Hex("C9ED8B"), Coral = Hex("FFB087");
        private static Font font;
        private static Sprite rounded;

        [MenuItem("Tools/Zoo World/Rebuild demo content")]
        public static void RebuildFromMenu()
        {
            if (EditorUtility.DisplayDialog("Rebuild Zoo World", "Regenerate demo assets and replace the demo scene?", "Rebuild", "Cancel")) Rebuild();
        }

        public static void Rebuild()
        {
            AssetDatabase.DeleteAsset("Assets/Scenes");
            AssetDatabase.DeleteAsset("Assets/InputSystem_Actions.inputactions");
            foreach (string folder in new[] { "Art", "Data", "Prefabs", "Scenes" }) Directory.CreateDirectory($"{Root}/{folder}");
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded = CreateRoundedSprite();

            var grass = Material("Meadow", "467957");
            var rim = Material("Habitat Rim", "264B39");
            var frogGreen = Material("Frog Green", "A9D56E");
            var frogLeg = Material("Frog Legs", "77AC50");
            var snakeGold = Material("Snake Gold", "EFAB65");
            var snakeStripe = Material("Snake Bands", "B86C45");
            var eye = Material("Eyes", "FFF6D9");
            var pupil = Material("Pupils", "172F27");
            var shade = Material("Ground Shadows", "315B43");
            var contact = Asset<PhysicsMaterial>($"{Root}/Art/Animal Contact.physicMaterial");
            contact.bounciness = 0.85f; contact.dynamicFriction = 0; contact.staticFriction = 0;
            contact.bounceCombine = PhysicsMaterialCombine.Maximum;
            contact.frictionCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(contact);

            var frogPrefab = BuildFrog(frogGreen, frogLeg, eye, pupil, shade, contact);
            var snakePrefab = BuildSnake(snakeGold, snakeStripe, eye, pupil, shade, contact);
            var hop = Asset<HopMovementDefinition>($"{Root}/Data/Frog Hop.asset");
            var linear = Asset<LinearMovementDefinition>($"{Root}/Data/Snake Glide.asset");
            hop.Interval = 1.45f; hop.Duration = 0.42f; hop.Distance = 1.65f; hop.Height = 0.65f;
            linear.Speed = 1.9f;
            var frog = Asset<AnimalDefinition>($"{Root}/Data/Frog.asset");
            frog.DisplayName = "Frog"; frog.Diet = Diet.Prey; frog.Prefab = frogPrefab; frog.Movement = hop;
            frog.Mass = 1; frog.SpawnWeight = 3; frog.InitialPopulation = 12; frog.Accent = Lime;
            var snake = Asset<AnimalDefinition>($"{Root}/Data/Snake.asset");
            snake.DisplayName = "Snake"; snake.Diet = Diet.Predator; snake.Prefab = snakePrefab; snake.Movement = linear;
            snake.Mass = 1.4f; snake.SpawnWeight = 1; snake.InitialPopulation = 4; snake.Accent = Coral;
            var settings = Asset<SimulationSettings>($"{Root}/Data/Simulation.asset");
            settings.Species = new[] { frog, snake }; settings.HabitatSize = new Vector2(24, 13);
            settings.MinimumSpawnInterval = 1; settings.MaximumSpawnInterval = 2;
            foreach (Object asset in new Object[] { hop, linear, frog, snake, settings }) EditorUtility.SetDirty(asset);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ZooWorld";
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.71f, 0.79f, 0.70f);
            RenderSettings.fog = false;
            var light = new GameObject("Sunlight").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.15f;
            light.color = Hex("FFF0CC"); light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(54, -32, 0);
            var camera = new GameObject("Habitat Camera", typeof(Camera), typeof(AudioListener), typeof(HabitatCamera)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 8;
            camera.transform.SetPositionAndRotation(new Vector3(0, 30, 0), Quaternion.Euler(90, 0, 0));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ink;
            camera.nearClipPlane = 0.3f; camera.farClipPlane = 60;
            BuildHabitat(grass, rim);
            var hud = BuildHud(settings);
            var eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var composition = new GameObject("Zoo World | Composition Root").AddComponent<ZooSimulation>();
            composition.Settings = settings; composition.WorldCamera = camera; composition.Hud = hud;
            composition.AnimalContainer = new GameObject("Animals (pooled at runtime)").transform;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Anatoly Sheshenin"; PlayerSettings.productName = "Zoo World";
            PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.shtolyan.zooworld");
            QualitySettings.vSyncCount = 1;
            AssetDatabase.SaveAssets();
            Debug.Log("Zoo World demo content generated: " + ScenePath);
        }

        private static void BuildHabitat(Material grass, Material rim)
        {
            var habitat = new GameObject("Habitat | decorative geometry, no walls").transform;
            Shape("Meadow", PrimitiveType.Cube, habitat, new Vector3(0, -0.18f, 0), new Vector3(24, 0.3f, 13), grass);
            Shape("Base", PrimitiveType.Cube, habitat, new Vector3(0, -0.38f, 0), new Vector3(24.5f, 0.35f, 13.5f), rim);
            var grid = Material("Survey Grid", "4B805B");
            for (int i = -9; i <= 9; i += 3) Shape("Survey line X", PrimitiveType.Cube, habitat, new Vector3(i, -0.015f, 0), new Vector3(0.018f, 0.01f, 13), grid);
            for (int i = -3; i <= 3; i += 3) Shape("Survey line Z", PrimitiveType.Cube, habitat, new Vector3(0, -0.015f, i), new Vector3(24, 0.01f, 0.018f), grid);
            var water = Material("Pond", "477F75");
            var bank = Material("Pond Bank", "3C6C50");
            foreach (var center in new[] { new Vector3(-8.8f, 0, 3.8f), new Vector3(8.6f, 0, -3.7f) })
            {
                Shape("Pond bank", PrimitiveType.Cylinder, habitat, center, new Vector3(3, 0.014f, 1.9f), bank);
                Shape("Water", PrimitiveType.Cylinder, habitat, center + Vector3.up * 0.02f, new Vector3(2.65f, 0.015f, 1.6f), water);
                var lily = Material("Lily Pads", "82A663");
                for (int i = 0; i < 3; i++) Shape("Lily pad", PrimitiveType.Cylinder, habitat,
                    center + new Vector3(-0.65f + i * 0.6f, 0.06f, 0.28f * Mathf.Sin(i * 2)), new Vector3(0.4f, 0.01f, 0.32f), lily);
            }
            var reed = Material("Reeds", "7DA36C");
            var rock = Material("Pebbles", "9FA98B");
            var rng = new System.Random(62);
            for (int i = 0; i < 32; i++)
            {
                float x = -11.5f + (float)rng.NextDouble() * 23;
                float z = (i % 2 == 0 ? 1 : -1) * 6.65f;
                float height = 0.3f + (float)rng.NextDouble() * 0.45f;
                Shape("Border reed", PrimitiveType.Cube, habitat, new Vector3(x, height * 0.5f - 0.1f, z), new Vector3(0.09f, height, 0.1f), reed);
                if (i % 3 == 0) Shape("Pebble", PrimitiveType.Sphere, habitat, new Vector3(x + 0.23f, 0, z - 0.1f), new Vector3(0.45f, 0.25f, 0.32f), rock);
            }
            var markers = Material("Corner Markers", "B9CE96");
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Shape("Corner tick", PrimitiveType.Cube, habitat, new Vector3(x * 11.6f, 0.01f, z * 6.12f), new Vector3(0.6f, 0.02f, 0.045f), markers);
                    Shape("Corner tick", PrimitiveType.Cube, habitat, new Vector3(x * 11.88f, 0.01f, z * 5.85f), new Vector3(0.045f, 0.02f, 0.6f), markers);
                }
        }

        private static AnimalAgent BuildFrog(Material green, Material legs, Material white, Material black, Material shade, PhysicsMaterial contact)
        {
            var root = AnimalRoot("Frog", contact, out Transform visual, out Transform shadow, shade);
            Shape("Body", PrimitiveType.Sphere, visual, new Vector3(0, 0.34f, 0), new Vector3(0.87f, 0.6f, 0.96f), green);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Hind leg", PrimitiveType.Sphere, visual, new Vector3(side * 0.39f, 0.16f, -0.25f), new Vector3(0.4f, 0.24f, 0.48f), legs);
                Shape("Front foot", PrimitiveType.Sphere, visual, new Vector3(side * 0.35f, 0.12f, 0.31f), new Vector3(0.24f, 0.16f, 0.27f), green);
                Shape("Eye", PrimitiveType.Sphere, visual, new Vector3(side * 0.23f, 0.57f, 0.28f), Vector3.one * 0.27f, white);
                Shape("Pupil", PrimitiveType.Sphere, visual, new Vector3(side * 0.23f, 0.69f, 0.32f), new Vector3(0.12f, 0.08f, 0.13f), black);
            }
            var agent = root.GetComponent<AnimalAgent>(); agent.ConfigureVisuals(visual, shadow);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Frog.prefab").GetComponent<AnimalAgent>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static AnimalAgent BuildSnake(Material gold, Material stripe, Material white, Material black, Material shade, PhysicsMaterial contact)
        {
            var root = AnimalRoot("Snake", contact, out Transform visual, out Transform shadow, shade);
            for (int i = 6; i >= 1; i--)
            {
                float size = Mathf.Lerp(0.56f, 0.13f, i / 7f);
                Shape("Body segment", PrimitiveType.Sphere, visual, new Vector3(Mathf.Sin(i * 0.8f) * 0.16f, 0.22f, -i * 0.23f), new Vector3(size, size * 0.72f, size * 1.15f), i % 2 == 0 ? stripe : gold);
            }
            Shape("Head", PrimitiveType.Sphere, visual, new Vector3(0, 0.29f, 0.16f), new Vector3(0.66f, 0.48f, 0.78f), gold);
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Eye", PrimitiveType.Sphere, visual, new Vector3(side * 0.2f, 0.48f, 0.27f), Vector3.one * 0.2f, white);
                Shape("Pupil", PrimitiveType.Sphere, visual, new Vector3(side * 0.2f, 0.57f, 0.3f), new Vector3(0.075f, 0.06f, 0.11f), black);
            }
            var agent = root.GetComponent<AnimalAgent>(); agent.ConfigureVisuals(visual, shadow);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Snake.prefab").GetComponent<AnimalAgent>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject AnimalRoot(string name, PhysicsMaterial contact, out Transform visual, out Transform shadow, Material shade)
        {
            var root = new GameObject(name, typeof(Rigidbody), typeof(SphereCollider), typeof(AnimalAgent));
            var body = root.GetComponent<Rigidbody>();
            body.useGravity = false; body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = root.GetComponent<SphereCollider>(); collider.radius = 0.49f; collider.center = Vector3.up * 0.35f; collider.sharedMaterial = contact;
            visual = new GameObject("Visual (hop height is cosmetic; collider stays planar)").transform;
            visual.SetParent(root.transform, false);
            shadow = Shape("Ground shadow", PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.012f, 0), new Vector3(1.2f, 0.012f, 0.9f), shade).transform;
            shadow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }

        private static ZooHud BuildHud(SimulationSettings settings)
        {
            var canvas = new GameObject("Habitat HUD | uGUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = 1;
            var hud = canvas.AddComponent<ZooHud>();
            var header = Panel("Header", canvas.transform, Ink);
            Stretch(header.rectTransform, new Vector2(0, 0.795f), Vector2.one);
            Label("Eyebrow", header.transform, "FIELD NOTES     /     001", 12, Muted, new Vector2(38, -21), new Vector2(350, 22));
            Label("Title", header.transform, "Zoo world", 46, Cream, new Vector2(35, -44), new Vector2(360, 62), FontStyle.Bold);
            Label("Subtitle", header.transform, "Small creatures. A living food chain.", 15, Muted, new Vector2(38, -111), new Vector2(380, 26));
            hud.StateCaption = Label("State", header.transform, "●   LIVE HABITAT", 12, Lime, new Vector2(38, -150), new Vector2(280, 22));

            var population = Rect("Population", header.transform, new Vector2(0.52f, 1), new Vector2(0.5f, 1), new Vector2(0, -23), new Vector2(220, 145));
            Label("Population caption", population, "IN THE HABITAT", 12, Muted, new Vector2(0, 0), new Vector2(220, 25));
            hud.Living = Label("Living count", population, "00", 48, Lime, new Vector2(0, -25), new Vector2(180, 63), FontStyle.Bold);
            hud.PopulationDetail = Label("Population detail", population, "12 prey    /    4 predators", 13, Muted, new Vector2(0, -91), new Vector2(235, 24));
            hud.Elapsed = Label("Elapsed", population, "00:00", 12, Muted, new Vector2(0, -123), new Vector2(100, 22));
            hud.DeadPrey = Counter(header.transform, "Prey lost", "PREY LOST", -218, Lime);
            hud.DeadPredators = Counter(header.transform, "Predators lost", "PREDATORS LOST", -38, Coral);

            var footer = Panel("Footer", canvas.transform, Ink);
            Stretch(footer.rectTransform, Vector2.zero, new Vector2(1, 0.115f));
            LabelBottom("Legend frog", footer.transform, "●  FROG  /  PREY", 12, Lime, new Vector2(38, 59), new Vector2(190, 24));
            LabelBottom("Legend snake", footer.transform, "●  SNAKE  /  PREDATOR", 12, Coral, new Vector2(205, 59), new Vector2(220, 24));
            hud.NextSpawn = LabelBottom("Next arrival", footer.transform, "NEXT ARRIVAL   1.5s", 12, Muted, new Vector2(38, 30), new Vector2(330, 24));

            hud.ResetButton = Button(footer.transform, "Reset", "Restart [R]", -38, 112, false, out _);
            hud.SpeedButton = Button(footer.transform, "Speed", "Speed  1x", -160, 116, false, out hud.SpeedCaption);
            hud.PauseButton = Button(footer.transform, "Pause", "Pause", -286, 116, true, out hud.PauseCaption);
            hud.SpawnButtons = new Button[settings.Species.Length];
            float right = -422;
            for (int i = settings.Species.Length - 1; i >= 0; i--)
            {
                hud.SpawnButtons[i] = Button(footer.transform, "Add " + settings.Species[i].DisplayName,
                    $"+ {settings.Species[i].DisplayName} [{i + 1}]", right, 125, false, out _);
                right -= 135;
            }
            var hint = Label("Hint", canvas.transform, "OBSERVE   /   Space to pause · 1 / 2 to add animals", 11, Muted,
                new Vector2(0, 0), new Vector2(600, 24));
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0.116f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0); hint.rectTransform.anchoredPosition = new Vector2(0, 6);
            hint.alignment = TextAnchor.MiddleCenter;
            hud.ToastLayer = new GameObject("Tasty labels (uGUI)", typeof(RectTransform)).GetComponent<RectTransform>();
            hud.ToastLayer.SetParent(canvas.transform, false); Stretch(hud.ToastLayer, Vector2.zero, Vector2.one);
            return hud;
        }

        private static Text Counter(Transform parent, string name, string caption, float right, Color accent)
        {
            var panel = Panel(name, parent, Hex("1B3A30"));
            var rt = panel.rectTransform; rt.anchorMin = rt.anchorMax = Vector2.one; rt.pivot = Vector2.one;
            rt.anchoredPosition = new Vector2(right, -25); rt.sizeDelta = new Vector2(164, 136);
            Label("Caption", panel.transform, caption, 11, Muted, new Vector2(18, -12), new Vector2(140, 23));
            var count = Label("Count", panel.transform, "00", 47, Cream, new Vector2(15, -35), new Vector2(140, 63), FontStyle.Bold);
            Label("Detail", panel.transform, "EATEN", 10, accent, new Vector2(18, -108), new Vector2(140, 19));
            return count;
        }

        private static Button Button(Transform parent, string name, string caption, float right, float width, bool primary, out Text text)
        {
            var image = Panel(name, parent, primary ? Lime : Hex("214538"));
            image.raycastTarget = true;
            var rt = image.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(right, 0); rt.sizeDelta = new Vector2(width, 44);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(0.75f, 0.85f, 0.75f); button.colors = colors;
            text = Label("Caption", image.transform, caption, 14, primary ? Ink : Cream, Vector2.zero, rt.sizeDelta, FontStyle.Bold);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one); text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static Image Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            image.sprite = rounded; image.type = Image.Type.Sliced;
            return image;
        }

        private static Text Label(string name, Transform parent, string value, int size, Color color, Vector2 position, Vector2 dimensions, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color; text.fontStyle = style;
            text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = text.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = position; rt.sizeDelta = dimensions;
            return text;
        }

        private static Text LabelBottom(string name, Transform parent, string value, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var text = Label(name, parent, value, size, color, position, dimensions);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = Vector2.zero;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = position; rt.sizeDelta = size;
            return rt;
        }

        private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        private static T Asset<T>(string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path); if (existing != null) return existing;
            T asset;
            if (typeof(ScriptableObject).IsAssignableFrom(typeof(T))) asset = ScriptableObject.CreateInstance(typeof(T)) as T;
            else if (typeof(T) == typeof(PhysicsMaterial)) asset = new PhysicsMaterial("Animal Contact") as T;
            else throw new System.InvalidOperationException(typeof(T).Name);
            AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static Material Material(string name, string color)
        {
            string path = $"{Root}/Art/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = Hex(color); material.SetFloat("_Glossiness", 0.08f); EditorUtility.SetDirty(material); return material;
        }

        private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>()); go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Sprite CreateRoundedSprite()
        {
            string path = $"{Root}/Art/Rounded Panel.png";
            if (!File.Exists(path))
            {
                const int size = 64; const float radius = 12;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        var delta = new Vector2(Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)), Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                        float distance = new Vector2(Mathf.Max(0, delta.x), Mathf.Max(0, delta.y)).magnitude;
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius + 0.5f - distance)));
                    }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.spriteBorder = Vector4.one * 14; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
    }
}
