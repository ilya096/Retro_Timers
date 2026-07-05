using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RetroTimers.TimeRewind
{
    [System.Serializable]
    public sealed class TimeRewindVisualConfig
    {
        [Header("Actors")]
        [SerializeField] private Color activePlayerColor = new(0.15f, 0.85f, 1f, 1f);
        [SerializeField] private Color firstCloneColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color oldCloneColor = new(0.55f, 0.62f, 0.72f, 1f);
        [SerializeField] private Color alteredFlashColor = new(1f, 1f, 1f, 1f);
        [SerializeField] private Color alteredOutlineColor = new(0.82f, 0.94f, 1f, 0.5f);

        [Header("Delayed Spawn")]
        [SerializeField] private Color delayedSpawnPreviewColor = new(0.15f, 0.85f, 1f, 0.8f);
        [SerializeField] private Color delayedSpawnProgressColor = new(0.15f, 0.85f, 1f, 0.9f);
        [SerializeField] private Color delayedSpawnProgressBackColor = new(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color delayedSpawnFlashColor = new(1f, 1f, 1f, 0.75f);

        [Header("Test Scene Objects")]
        [SerializeField] private Color cameraBackgroundColor = new(0.08f, 0.1f, 0.14f, 1f);
        [SerializeField] private Color solidLevelColor = new(0.2f, 0.24f, 0.3f, 1f);
        [SerializeField] private Color pressurePlateColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color doorColor = new(0.85f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color exitColor = new(0.2f, 1f, 0.55f, 1f);
        [SerializeField] private Color hazardColor = new(0.85f, 0.1f, 0.32f, 1f);
        [SerializeField] private Color sceneLabelColor = Color.white;

        public Color ActivePlayerColor => activePlayerColor;
        public Color FirstCloneColor => firstCloneColor;
        public Color OldCloneColor => oldCloneColor;
        public Color AlteredFlashColor => alteredFlashColor;
        public Color AlteredOutlineColor => alteredOutlineColor;
        public Color DelayedSpawnPreviewColor => delayedSpawnPreviewColor;
        public Color DelayedSpawnProgressColor => delayedSpawnProgressColor;
        public Color DelayedSpawnProgressBackColor => delayedSpawnProgressBackColor;
        public Color DelayedSpawnFlashColor => delayedSpawnFlashColor;
        public Color CameraBackgroundColor => cameraBackgroundColor;
        public Color SolidLevelColor => solidLevelColor;
        public Color PressurePlateColor => pressurePlateColor;
        public Color DoorColor => doorColor;
        public Color ExitColor => exitColor;
        public Color HazardColor => hazardColor;
        public Color SceneLabelColor => sceneLabelColor;
    }

    public sealed class TimeRewindController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform cloneParent;

        [Header("Balance")]
        [SerializeField] private float levelTimeLimitSeconds = 12f;
        [SerializeField] private float cloneSpawnDelaySeconds = 3f;
        [SerializeField] private float minimumActiveTimeAfterDelay = 1f;
        [SerializeField] private float rewindTransitionSeconds = 0.35f;

        [Header("Visual Config")]
        [SerializeField] private TimeRewindVisualConfig visualConfig = new();

        [Header("Clone Readability")]
        [SerializeField] private float cloneNewestAlpha = 0.8f;
        [SerializeField] private float cloneAlphaStep = 0.2f;
        [SerializeField] private float cloneMinAlpha = 0.2f;
        [SerializeField] private float cloneAlteredFlashSeconds = 0.18f;

        [Header("Delayed Spawn Preview")]
        [SerializeField] private float delayedSpawnPreviewThresholdSeconds = 0.25f;
        [SerializeField] private int delayedSpawnRadialSegments = 48;
        [SerializeField] private float delayedSpawnRadialRadius = 0.32f;
        [SerializeField] private float delayedSpawnRadialGapAboveGhost = 0.12f;

        [Header("Debug")]
        [SerializeField] private ClonePlaybackMode clonePlaybackMode = ClonePlaybackMode.PreservePhysicalOffset;

        private readonly List<List<ActorFrame>> recordings = new();
        private readonly List<TimeClonePlayback> activeClones = new();
        private IRewindResettable[] resettableObjects;
        private PlayerActor activePlayer;
        private TimeRewindStatus status = TimeRewindStatus.Running;
        private float iterationTimer;
        private float currentControlDelay;
        private int completedRewindCount;
        private string statusMessage = "Отмотка доступна: R / Y";
        private bool deathAlreadyFixed;
        private GameObject delayedSpawnPreview;
        private LineRenderer delayedSpawnProgressRing;
        private LineRenderer delayedSpawnProgressBackRing;
        private GameObject delayedSpawnFlash;
        private TimeRewindVisualConfig VisualConfig
        {
            get
            {
                if (visualConfig == null)
                {
                    visualConfig = new TimeRewindVisualConfig();
                }

                return visualConfig;
            }
        }

        public TimeRewindStatus Status => status;
        public float IterationTimer => iterationTimer;
        public float LevelTimeLimitSeconds => levelTimeLimitSeconds;
        public float CurrentControlDelay => currentControlDelay;
        public float RemainingActiveTimeAfterDelay =>
            TimeRewindRules.CalculateRemainingActiveTime(levelTimeLimitSeconds, currentControlDelay);
        public int CompletedRewindCount => completedRewindCount;
        public string StatusMessage => statusMessage;
        public ClonePlaybackMode PlaybackMode => clonePlaybackMode;
        public string PlaybackModeLabel => clonePlaybackMode == ClonePlaybackMode.FollowRecordedPosition ? "Recover to recording" : "Preserve displacement";
        public bool AnyCloneAlteredByControlledPlayer => activeClones.Exists(clone => clone != null && clone.IsAlteredByControlledPlayer);
        public bool RewindAvailable => TimeRewindRules.CanRequestManualRewind(IsLevelEnded, activePlayer != null && activePlayer.IsAlive, deathAlreadyFixed);
        public bool IsLevelEnded => status is TimeRewindStatus.Won or TimeRewindStatus.Failed;

        public void ConfigureSceneReferences(GameObject playerTemplate, Transform spawnPoint, Transform clonesRoot)
        {
            playerPrefab = playerTemplate;
            playerSpawn = spawnPoint;
            cloneParent = clonesRoot;
        }

        private void Awake()
        {
            if (cloneParent == null)
            {
                GameObject parent = new("Time Clones");
                cloneParent = parent.transform;
            }

            ResolveMissingPlayerTemplate();
            resettableObjects = FindResettableObjects();
        }

        private void Start()
        {
            BeginIteration(0f);
        }

        private void Update()
        {
            if (status != TimeRewindStatus.Running)
            {
                return;
            }

            iterationTimer += Time.deltaTime;

            if (WasRewindPressed())
            {
                RequestManualRewind();
            }

            if (iterationTimer >= levelTimeLimitSeconds && !IsLevelEnded)
            {
                StartCoroutine(PerformRewind(RewindReason.TimerExpired));
            }
        }

        public void RequestManualRewind()
        {
            if (!TimeRewindRules.CanRequestManualRewind(IsLevelEnded, activePlayer != null && activePlayer.IsAlive, deathAlreadyFixed))
            {
                statusMessage = "Отмотка недоступна";
                return;
            }

            StartCoroutine(PerformRewind(RewindReason.Manual));
        }

        public void NotifyExitReached()
        {
            if (IsLevelEnded)
            {
                return;
            }

            status = TimeRewindStatus.Won;
            statusMessage = "Уровень пройден";
            if (activePlayer != null)
            {
                activePlayer.SetControlEnabled(false);
            }
        }

        public void NotifyActorDied()
        {
            if (IsLevelEnded)
            {
                return;
            }

            deathAlreadyFixed = true;
            status = TimeRewindStatus.Failed;
            statusMessage = "Парадокс: клон погиб. R недоступна, перезапусти уровень";
            if (activePlayer != null)
            {
                activePlayer.Kill();
            }
        }

        public void RestartLevel()
        {
            StopAllCoroutines();
            DestroyActors();
            recordings.Clear();
            completedRewindCount = 0;
            deathAlreadyFixed = false;
            status = TimeRewindStatus.Running;
            ResetWorld();
            BeginIteration(0f);
        }

        public void ToggleClonePlaybackMode()
        {
            clonePlaybackMode = clonePlaybackMode == ClonePlaybackMode.FollowRecordedPosition
                ? ClonePlaybackMode.PreservePhysicalOffset
                : ClonePlaybackMode.FollowRecordedPosition;

            foreach (TimeClonePlayback clone in activeClones)
            {
                if (clone != null)
                {
                    clone.PlaybackMode = clonePlaybackMode;
                }
            }

            statusMessage = $"Clone mode: {PlaybackModeLabel}";
        }

        private IEnumerator PerformRewind(RewindReason reason)
        {
            if (status != TimeRewindStatus.Running)
            {
                yield break;
            }

            status = TimeRewindStatus.Rewinding;
            statusMessage = reason == RewindReason.Manual ? "Отмотка..." : "Время вышло: авто-отмотка";
            int nextCompletedRewindCount = completedRewindCount + 1;
            float nextControlDelay = TimeRewindRules.CalculatePlayerControlDelay(cloneSpawnDelaySeconds, nextCompletedRewindCount);
            bool canCreatePlayableIteration = TimeRewindRules.HasEnoughActiveTime(levelTimeLimitSeconds, nextControlDelay, minimumActiveTimeAfterDelay);
            float totalPreviewDelay = rewindTransitionSeconds + nextControlDelay;

            if (activePlayer != null)
            {
                List<ActorFrame> recording = activePlayer.FinishRecording();
                if (recording.Count > 0)
                {
                    recordings.Add(recording);
                }

                activePlayer.gameObject.SetActive(false);
            }

            if (canCreatePlayableIteration && totalPreviewDelay >= delayedSpawnPreviewThresholdSeconds)
            {
                ShowDelayedSpawnPreview();
            }

            float transitionRemaining = rewindTransitionSeconds;
            while (transitionRemaining > 0f)
            {
                if (canCreatePlayableIteration)
                {
                    UpdateDelayedSpawnPreview(nextControlDelay + transitionRemaining, totalPreviewDelay);
                }

                transitionRemaining -= Time.deltaTime;
                yield return null;
            }

            completedRewindCount++;

            ResetWorld();
            DestroyActors(hidePreview: false);
            SpawnPlaybackClones();

            if (!canCreatePlayableIteration)
            {
                HideDelayedSpawnPreview();
                status = TimeRewindStatus.Failed;
                statusMessage = "Новой итерации не хватает времени. Нажми Restart";
                activePlayer = null;
                yield break;
            }

            status = TimeRewindStatus.Running;
            BeginIteration(nextControlDelay, totalPreviewDelay, delayedSpawnPreview != null && delayedSpawnPreview.activeSelf);
        }

        private void BeginIteration(float controlDelay, float previewTotalDelay = -1f, bool previewAlreadyVisible = false)
        {
            iterationTimer = 0f;
            currentControlDelay = controlDelay;
            deathAlreadyFixed = false;

            if (playerPrefab == null || playerSpawn == null)
            {
                status = TimeRewindStatus.Failed;
                statusMessage = "Не назначен playerPrefab или playerSpawn";
                return;
            }

            statusMessage = controlDelay > 0f ? "Появление готовится" : "Отмотка доступна: R / Y";
            StartCoroutine(SpawnPlayerAfterDelay(controlDelay, previewTotalDelay, previewAlreadyVisible));
        }

        private IEnumerator SpawnPlayerAfterDelay(float delay, float previewTotalDelay, bool previewAlreadyVisible)
        {
            bool spawnFlashStarted = false;
            float totalDelay = previewTotalDelay > 0f ? previewTotalDelay : delay;
            if (!previewAlreadyVisible && totalDelay >= delayedSpawnPreviewThresholdSeconds)
            {
                ShowDelayedSpawnPreview();
            }
            else if (!previewAlreadyVisible && delay > 0f)
            {
                StartCoroutine(PlayDelayedSpawnFlash());
                spawnFlashStarted = true;
            }

            float remaining = delay;
            while (remaining > 0f && status == TimeRewindStatus.Running)
            {
                statusMessage = "Появление готовится";
                UpdateDelayedSpawnPreview(remaining, totalDelay);
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (status != TimeRewindStatus.Running)
            {
                HideDelayedSpawnPreview();
                yield break;
            }

            HideDelayedSpawnPreview();
            if (!spawnFlashStarted)
            {
                StartCoroutine(PlayDelayedSpawnFlash());
            }
            GameObject playerObject = Instantiate(playerPrefab, playerSpawn.position, playerSpawn.rotation);
            playerObject.SetActive(true);
            activePlayer = playerObject.GetComponent<PlayerActor>();
            if (activePlayer == null)
            {
                status = TimeRewindStatus.Failed;
                statusMessage = "Prefab игрока не содержит PlayerActor";
                Destroy(playerObject);
                yield break;
            }

            activePlayer.name = $"Player Iteration {completedRewindCount + 1}";
            ApplyActorColor(activePlayer.gameObject, VisualConfig.ActivePlayerColor);
            activePlayer.BeginIteration(enableRecording: true, initialIterationTime: delay);
            activePlayer.SetControlEnabled(true);
            statusMessage = "Отмотка доступна: R / Y";
        }

        private void SpawnPlaybackClones()
        {
            for (int i = 0; i < recordings.Count; i++)
            {
                List<ActorFrame> recording = recordings[i];
                if (recording.Count == 0 || playerPrefab == null)
                {
                    continue;
                }

                GameObject cloneObject = BuildCloneObject($"Clone {i + 1}", recording[0].Position);
                TimeClonePlayback playback = cloneObject.AddComponent<TimeClonePlayback>();
                Color cloneColor = Color.Lerp(VisualConfig.FirstCloneColor, VisualConfig.OldCloneColor, recordings.Count <= 1 ? 0f : (float)i / (recordings.Count - 1));
                int ageFromNewest = recordings.Count - 1 - i;
                cloneColor.a = TimeRewindRules.CalculateCloneAgeAlpha(cloneNewestAlpha, cloneAlphaStep, cloneMinAlpha, ageFromNewest);
                ApplyActorColor(playback.gameObject, cloneColor);
                playback.Play(
                    recording,
                    cloneColor,
                    clonePlaybackMode,
                    cloneAlteredFlashSeconds,
                    VisualConfig.AlteredFlashColor,
                    VisualConfig.AlteredOutlineColor,
                    levelTimeLimitSeconds);
                activeClones.Add(playback);
            }
        }

        private GameObject BuildCloneObject(string objectName, Vector2 position)
        {
            GameObject cloneObject = new(objectName);
            cloneObject.transform.SetParent(cloneParent);
            cloneObject.transform.position = position;
            cloneObject.transform.localScale = playerPrefab.transform.localScale;

            if (playerPrefab.TryGetComponent(out MeshFilter sourceMeshFilter))
            {
                MeshFilter cloneMeshFilter = cloneObject.AddComponent<MeshFilter>();
                cloneMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
            }

            if (playerPrefab.TryGetComponent(out MeshRenderer sourceRenderer))
            {
                MeshRenderer cloneRenderer = cloneObject.AddComponent<MeshRenderer>();
                cloneRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }
            else if (playerPrefab.TryGetComponent(out SpriteRenderer sourceSpriteRenderer))
            {
                SpriteRenderer cloneSpriteRenderer = cloneObject.AddComponent<SpriteRenderer>();
                cloneSpriteRenderer.sprite = sourceSpriteRenderer.sprite;
                cloneSpriteRenderer.sortingLayerID = sourceSpriteRenderer.sortingLayerID;
                cloneSpriteRenderer.sortingOrder = sourceSpriteRenderer.sortingOrder;
            }

            Rigidbody2D cloneBody = cloneObject.AddComponent<Rigidbody2D>();
            cloneBody.bodyType = RigidbodyType2D.Dynamic;
            cloneBody.gravityScale = 0f;
            cloneBody.mass = 0.2f;
            cloneBody.freezeRotation = true;
            cloneBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D cloneCollider = cloneObject.AddComponent<BoxCollider2D>();
            if (playerPrefab.TryGetComponent(out BoxCollider2D sourceCollider))
            {
                cloneCollider.size = sourceCollider.size;
                cloneCollider.offset = sourceCollider.offset;
            }

            return cloneObject;
        }

        private void ResolveMissingPlayerTemplate()
        {
            if (playerPrefab != null)
            {
                DisableSceneTemplateIfNeeded(playerPrefab);
                return;
            }

            PlayerActor[] scenePlayers = FindObjectsByType<PlayerActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (scenePlayers.Length == 0)
            {
                return;
            }

            playerPrefab = scenePlayers[0].gameObject;
            DisableSceneTemplateIfNeeded(playerPrefab);
        }

        private static void DisableSceneTemplateIfNeeded(GameObject template)
        {
            if (template == null || !template.scene.IsValid())
            {
                return;
            }

            template.SetActive(false);
        }

        private void DestroyActors(bool hidePreview = true)
        {
            if (hidePreview)
            {
                HideDelayedSpawnPreview();
            }

            if (activePlayer != null)
            {
                Destroy(activePlayer.gameObject);
                activePlayer = null;
            }

            foreach (TimeClonePlayback clone in activeClones)
            {
                if (clone != null)
                {
                    Destroy(clone.gameObject);
                }
            }

            activeClones.Clear();
        }

        private void ResetWorld()
        {
            resettableObjects = FindResettableObjects();
            foreach (IRewindResettable resettable in resettableObjects)
            {
                resettable.ResetForRewind();
            }
        }

        private static IRewindResettable[] FindResettableObjects()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            List<IRewindResettable> resettable = new();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IRewindResettable item)
                {
                    resettable.Add(item);
                }
            }

            return resettable.ToArray();
        }

        private static bool WasRewindPressed()
        {
            bool keyboardPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
            bool gamepadPressed = Gamepad.current != null &&
                                  (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame);
            return keyboardPressed || gamepadPressed;
        }

        private static void ApplyActorColor(GameObject actor, Color color)
        {
            if (actor.TryGetComponent(out MeshRenderer meshRenderer))
            {
                PrepareTransparentMaterial(meshRenderer.material);
                meshRenderer.material.color = color;
            }

            if (actor.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.color = color;
            }
        }

        private void ShowDelayedSpawnPreview()
        {
            if (playerSpawn == null || playerPrefab == null)
            {
                return;
            }

            EnsureDelayedSpawnPreview();
            delayedSpawnPreview.transform.position = playerSpawn.position;
            delayedSpawnPreview.SetActive(true);
            UpdateDelayedSpawnPreview(currentControlDelay, currentControlDelay);
        }

        private void UpdateDelayedSpawnPreview(float remainingDelay, float totalDelay)
        {
            if (delayedSpawnPreview == null || !delayedSpawnPreview.activeSelf || delayedSpawnProgressRing == null)
            {
                return;
            }

            float progress = TimeRewindRules.CalculateSpawnProgressNormalized(remainingDelay, totalDelay);
            SetRadialProgress(delayedSpawnProgressRing, GetDelayedSpawnRadialRadius(), progress);
        }

        private void HideDelayedSpawnPreview()
        {
            if (delayedSpawnPreview != null)
            {
                delayedSpawnPreview.SetActive(false);
            }
        }

        private IEnumerator PlayDelayedSpawnFlash()
        {
            if (playerSpawn == null)
            {
                yield break;
            }

            EnsureDelayedSpawnFlash();
            delayedSpawnFlash.transform.position = playerSpawn.position;
            delayedSpawnFlash.SetActive(true);
            yield return new WaitForSeconds(0.12f);
            if (delayedSpawnFlash != null)
            {
                delayedSpawnFlash.SetActive(false);
            }
        }

        private void EnsureDelayedSpawnPreview()
        {
            if (delayedSpawnPreview != null)
            {
                return;
            }

            delayedSpawnPreview = new GameObject("Delayed Spawn Preview");

            GameObject silhouette = GameObject.CreatePrimitive(PrimitiveType.Cube);
            silhouette.name = "Spawn Silhouette";
            silhouette.transform.SetParent(delayedSpawnPreview.transform, false);
            silhouette.transform.localScale = playerPrefab.transform.localScale;
            Destroy(silhouette.GetComponent<Collider>());
            MeshRenderer silhouetteRenderer = silhouette.GetComponent<MeshRenderer>();
            silhouetteRenderer.material = BuildRuntimeMaterial("DelayedSpawnPreviewMaterial", VisualConfig.DelayedSpawnPreviewColor);

            float radialRadius = GetDelayedSpawnRadialRadius();
            Vector3 radialPosition = GetDelayedSpawnRadialLocalPosition(radialRadius);
            delayedSpawnProgressBackRing = CreateProgressRing("Spawn Progress Back", VisualConfig.DelayedSpawnProgressBackColor, 0.02f, radialPosition);
            SetRadialProgress(delayedSpawnProgressBackRing, radialRadius, 1f);

            delayedSpawnProgressRing = CreateProgressRing("Spawn Progress Ring", VisualConfig.DelayedSpawnProgressColor, 0.035f, radialPosition);
            SetRadialProgress(delayedSpawnProgressRing, radialRadius, 0f);

            delayedSpawnPreview.SetActive(false);
        }

        private LineRenderer CreateProgressRing(string objectName, Color color, float width, Vector3 localPosition)
        {
            GameObject ringObject = new(objectName);
            ringObject.transform.SetParent(delayedSpawnPreview.transform, false);
            ringObject.transform.localPosition = localPosition;
            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = width;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.material = BuildRuntimeMaterial($"{objectName}Material", color);
            return line;
        }

        private void SetRadialProgress(LineRenderer ring, float radius, float progress)
        {
            if (ring == null)
            {
                return;
            }

            progress = Mathf.Clamp01(progress);
            int segmentCount = Mathf.Max(8, delayedSpawnRadialSegments);
            int pointCount = Mathf.Max(2, Mathf.CeilToInt(segmentCount * progress) + 1);
            float arc = Mathf.PI * 2f * progress;
            ring.positionCount = pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0f : (float)i / (pointCount - 1);
                float angle = Mathf.PI * 0.5f - arc * t;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.08f));
            }
        }

        private float GetDelayedSpawnRadialRadius()
        {
            float playerWidth = playerPrefab != null ? Mathf.Abs(playerPrefab.transform.localScale.x) : 0.75f;
            return Mathf.Min(Mathf.Max(0.05f, delayedSpawnRadialRadius), playerWidth * 0.5f);
        }

        private Vector3 GetDelayedSpawnRadialLocalPosition(float radius)
        {
            float playerHeight = playerPrefab != null ? Mathf.Abs(playerPrefab.transform.localScale.y) : 1.35f;
            float y = playerHeight * 0.5f + radius + delayedSpawnRadialGapAboveGhost;
            return new Vector3(0f, y, -0.08f);
        }

        private void EnsureDelayedSpawnFlash()
        {
            if (delayedSpawnFlash != null)
            {
                return;
            }

            delayedSpawnFlash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            delayedSpawnFlash.name = "Delayed Spawn Flash";
            delayedSpawnFlash.transform.localScale = playerPrefab != null ? playerPrefab.transform.localScale * 1.25f : Vector3.one;
            Destroy(delayedSpawnFlash.GetComponent<Collider>());
            MeshRenderer flashRenderer = delayedSpawnFlash.GetComponent<MeshRenderer>();
            flashRenderer.material = BuildRuntimeMaterial("DelayedSpawnFlashMaterial", VisualConfig.DelayedSpawnFlashColor);
            delayedSpawnFlash.SetActive(false);
        }

        private static Material BuildRuntimeMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material material = new(shader)
            {
                name = materialName,
                color = color
            };
            PrepareTransparentMaterial(material);
            return material;
        }

        private static void PrepareTransparentMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}
