using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RetroTimers.TimeRewind
{
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

        [Header("Clone Readability")]
        [SerializeField] private Color activePlayerColor = new(0.15f, 0.85f, 1f, 1f);
        [SerializeField] private Color firstCloneColor = new(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color oldCloneColor = new(0.55f, 0.62f, 0.72f, 1f);

        [Header("Debug")]
        [SerializeField] private ClonePlaybackMode clonePlaybackMode;

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

            if (activePlayer != null)
            {
                List<ActorFrame> recording = activePlayer.FinishRecording();
                if (recording.Count > 0)
                {
                    recordings.Add(recording);
                }
            }

            yield return new WaitForSeconds(rewindTransitionSeconds);

            completedRewindCount++;
            float nextControlDelay = TimeRewindRules.CalculatePlayerControlDelay(cloneSpawnDelaySeconds, completedRewindCount);
            bool canCreatePlayableIteration = TimeRewindRules.HasEnoughActiveTime(levelTimeLimitSeconds, nextControlDelay, minimumActiveTimeAfterDelay);

            ResetWorld();
            DestroyActors();
            SpawnPlaybackClones();

            if (!canCreatePlayableIteration)
            {
                status = TimeRewindStatus.Failed;
                statusMessage = "Новой итерации не хватает времени. Нажми Restart";
                activePlayer = null;
                yield break;
            }

            status = TimeRewindStatus.Running;
            BeginIteration(nextControlDelay);
        }

        private void BeginIteration(float controlDelay)
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

            statusMessage = controlDelay > 0f ? $"Ожидание появления: {controlDelay:0.0} c" : "Отмотка доступна: R / Y";
            StartCoroutine(SpawnPlayerAfterDelay(controlDelay));
        }

        private IEnumerator SpawnPlayerAfterDelay(float delay)
        {
            float remaining = delay;
            while (remaining > 0f && status == TimeRewindStatus.Running)
            {
                statusMessage = $"Ожидание появления: {remaining:0.0} c";
                remaining -= Time.deltaTime;
                yield return null;
            }

            if (status != TimeRewindStatus.Running)
            {
                yield break;
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
            ApplyActorColor(activePlayer.gameObject, activePlayerColor);
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
                Color cloneColor = Color.Lerp(firstCloneColor, oldCloneColor, recordings.Count <= 1 ? 0f : (float)i / (recordings.Count - 1));
                ApplyActorColor(playback.gameObject, cloneColor);
                playback.Play(recording, cloneColor, clonePlaybackMode);
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

        private void DestroyActors()
        {
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
                meshRenderer.material.color = color;
            }

            if (actor.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.color = color;
            }
        }
    }
}
