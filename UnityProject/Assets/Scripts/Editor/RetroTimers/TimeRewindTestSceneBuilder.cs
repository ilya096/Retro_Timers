using System.IO;
using RetroTimers.TimeRewind;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RetroTimers.EditorTools
{
    public static class TimeRewindTestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TimeRewindTestScene.unity";
        private const string PrefabDirectory = "Assets/Prefabs";
        private const string MaterialDirectory = "Assets/Materials";
        private const string PlayerPrefabPath = "Assets/Prefabs/MvpPlayer.prefab";
        private const float PlayerJumpVelocity = 12f;

        [MenuItem("RetroTimers/Build Time Rewind Test Scene")]
        public static void BuildScene()
        {
            EnsureDirectory(PrefabDirectory);
            EnsureDirectory(MaterialDirectory);

            TimeRewindVisualConfig visualConfig = new();
            GameObject playerPrefab = BuildPlayerPrefab(visualConfig);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "TimeRewindTestScene";

            BuildCamera(visualConfig);
            BuildGlobalLight();

            Transform spawn = CreateMarker("Player Spawn", new Vector3(-5f, 0f, 0f)).transform;
            Transform cloneParent = new GameObject("Time Clones").transform;

            GameObject controllerObject = new("Time Rewind Controller");
            TimeRewindController controller = controllerObject.AddComponent<TimeRewindController>();
            controller.ConfigureSceneReferences(playerPrefab, spawn, cloneParent);
            SerializedObject controllerSerialized = new(controller);
            controllerSerialized.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            controllerSerialized.FindProperty("playerSpawn").objectReferenceValue = spawn;
            controllerSerialized.FindProperty("cloneParent").objectReferenceValue = cloneParent;
            controllerSerialized.FindProperty("levelTimeLimitSeconds").floatValue = 12f;
            controllerSerialized.FindProperty("cloneSpawnDelaySeconds").floatValue = 3f;
            controllerSerialized.FindProperty("minimumActiveTimeAfterDelay").floatValue = 1f;
            controllerSerialized.FindProperty("cloneNewestAlpha").floatValue = 0.8f;
            controllerSerialized.FindProperty("cloneAlphaStep").floatValue = 0.2f;
            controllerSerialized.FindProperty("cloneMinAlpha").floatValue = 0.2f;
            controllerSerialized.FindProperty("cloneAlteredFlashSeconds").floatValue = 0.18f;
            controllerSerialized.FindProperty("delayedSpawnPreviewThresholdSeconds").floatValue = 0.25f;
            controllerSerialized.FindProperty("delayedSpawnRadialSegments").intValue = 48;
            controllerSerialized.FindProperty("delayedSpawnRadialRadius").floatValue = 0.32f;
            controllerSerialized.FindProperty("delayedSpawnRadialGapAboveGhost").floatValue = 0.12f;
            controllerSerialized.FindProperty("clonePlaybackMode").enumValueIndex = (int)ClonePlaybackMode.PreservePhysicalOffset;
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
            controllerObject.AddComponent<RewindHud>();

            BuildLevel(controller, visualConfig);

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log($"Built RetroTimers rewind test scene at {ScenePath}");
        }

        private static GameObject BuildPlayerPrefab(TimeRewindVisualConfig visualConfig)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "MvpPlayer";
            player.transform.localScale = new Vector3(0.75f, 1.35f, 0.75f);
            Object.DestroyImmediate(player.GetComponent<BoxCollider>());

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            body.mass = 10f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.75f, 1.35f);

            MeshRenderer renderer = player.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = BuildMaterial("MvpPlayerMaterial", visualConfig.ActivePlayerColor);

            Transform groundCheck = new GameObject("Ground Check").transform;
            groundCheck.SetParent(player.transform);
            groundCheck.localPosition = new Vector3(0f, -0.78f, 0f);

            PlayerActor actor = player.AddComponent<PlayerActor>();
            SerializedObject actorSerialized = new(actor);
            actorSerialized.FindProperty("jumpVelocity").floatValue = PlayerJumpVelocity;
            actorSerialized.FindProperty("groundCheck").objectReferenceValue = groundCheck;
            actorSerialized.ApplyModifiedPropertiesWithoutUndo();
            player.AddComponent<RewindableTransform>();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return savedPrefab;
        }

        private static void BuildCamera(TimeRewindVisualConfig visualConfig)
        {
            GameObject cameraObject = new("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = visualConfig.CameraBackgroundColor;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(1.5f, 1.3f, -10f);
        }

        private static void BuildGlobalLight()
        {
            GameObject lightObject = new("Global Light 2D");
            System.Type lightType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType != null)
            {
                lightObject.AddComponent(lightType);
            }
        }

        private static void BuildLevel(TimeRewindController controller, TimeRewindVisualConfig visualConfig)
        {
            CreateSolidBox("Ground", new Vector3(1.5f, -1.1f, 0f), new Vector3(16f, 0.7f, 1f), visualConfig.SolidLevelColor);
            CreateSolidBox("Left Wall", new Vector3(-7f, 1f, 0f), new Vector3(0.5f, 4f, 1f), visualConfig.SolidLevelColor);

            GameObject plate = CreateTriggerBox("Pressure Plate", new Vector3(-0.2f, -0.55f, 0f), new Vector3(1.25f, 0.18f, 1f), visualConfig.PressurePlateColor);
            GameObject door = CreateSolidBox("Door", new Vector3(4.7f, 0.65f, 0f), new Vector3(0.65f, 3f, 1f), visualConfig.DoorColor);
            PressurePlateDoor pressureDoor = plate.AddComponent<PressurePlateDoor>();
            SerializedObject pressureSerialized = new(pressureDoor);
            pressureSerialized.FindProperty("detectionArea").objectReferenceValue = plate.GetComponent<Collider2D>();
            pressureSerialized.FindProperty("door").objectReferenceValue = door;
            pressureSerialized.FindProperty("doorCollider").objectReferenceValue = door.GetComponent<Collider2D>();
            pressureSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject exit = CreateTriggerBox("Exit", new Vector3(7.4f, 0.05f, 0f), new Vector3(0.8f, 2.0f, 1f), visualConfig.ExitColor);
            LevelExit levelExit = exit.AddComponent<LevelExit>();
            SerializedObject exitSerialized = new(levelExit);
            exitSerialized.FindProperty("controller").objectReferenceValue = controller;
            exitSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject hazard = CreateTriggerBox("Pit Hazard", new Vector3(1.9f, -1.65f, 0f), new Vector3(1.2f, 0.25f, 1f), visualConfig.HazardColor);
            Hazard hazardComponent = hazard.AddComponent<Hazard>();
            SerializedObject hazardSerialized = new(hazardComponent);
            hazardSerialized.FindProperty("controller").objectReferenceValue = controller;
            hazardSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateLabel("Goal: first run to the yellow plate, press R, then use the clone to pass the red door.", new Vector3(-2f, 4.3f, 0f), visualConfig.SceneLabelColor);
        }

        private static GameObject CreateSolidBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            Object.DestroyImmediate(box.GetComponent<BoxCollider>());
            box.AddComponent<BoxCollider2D>();
            box.AddComponent<RewindableTransform>();
            box.GetComponent<MeshRenderer>().sharedMaterial = BuildMaterial($"{name}Material", color);
            return box;
        }

        private static GameObject CreateTriggerBox(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject box = CreateSolidBox(name, position, scale, color);
            Collider2D collider = box.GetComponent<Collider2D>();
            collider.isTrigger = true;
            return box;
        }

        private static GameObject CreateMarker(string name, Vector3 position)
        {
            GameObject marker = new(name);
            marker.transform.position = position;
            return marker;
        }

        private static void CreateLabel(string text, Vector3 position, Color color)
        {
            GameObject label = new("Scene Instructions");
            label.transform.position = position;
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.22f;
            mesh.color = color;
        }

        private static Material BuildMaterial(string name, Color color)
        {
            string assetName = name.Replace(" ", string.Empty);
            string assetPath = $"{MaterialDirectory}/{assetName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material material = new(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, assetPath);
            return material;
        }

        private static void EnsureDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
    }
}
