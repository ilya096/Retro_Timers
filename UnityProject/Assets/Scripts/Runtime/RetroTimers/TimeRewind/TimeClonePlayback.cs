using System.Collections.Generic;
using UnityEngine;

namespace RetroTimers.TimeRewind
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class TimeClonePlayback : MonoBehaviour
    {
        [SerializeField] private float maxFollowSpeed = 9f;
        [SerializeField] private ClonePlaybackMode playbackMode;

        private IReadOnlyList<ActorFrame> frames;
        private readonly List<TimeClonePlayback> ignoredPlaybackPeers = new();
        private readonly List<ActorFrame> resolvedFrames = new();
        private Rigidbody2D body;
        private Renderer[] renderers;
        private Collider2D[] colliders;
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
        private float playbackTime;
        private float playbackVisibleUntilTime;
        private int frameIndex;
        private bool playbackVisible;
        private bool alteredByControlledPlayer;
        private Vector2 lastRecordedSamplePosition;
        private Color baseColor = Color.white;
        private Color alteredColor = Color.white;
        private Color alteredFlashColor = Color.white;
        private Color alteredOutlineColor = Color.white;
        private float alteredFlashSeconds = 0.18f;
        private GameObject alteredOutline;
        private Coroutine alteredFlashCoroutine;
        private string visibilityReason = "not_started";
        private float lastControlledContactTime = -999f;
        private float lastControlledBlockTime = -999f;
        private float lastResolvedFrameTime = -999f;

        public ClonePlaybackMode PlaybackMode
        {
            get => playbackMode;
            set => playbackMode = value;
        }

        public bool IsAlteredByControlledPlayer => alteredByControlledPlayer;
        public bool IsPlaybackVisible => playbackVisible;
        public float PlaybackTime => playbackTime;
        public float RecordingStartTime => frames != null && frames.Count > 0 ? frames[0].Time : 0f;
        public float RecordingEndTime => frames != null && frames.Count > 0 ? frames[frames.Count - 1].Time : 0f;
        public float PlaybackVisibleUntilTime => playbackVisibleUntilTime;
        public int FrameCount => frames?.Count ?? 0;
        public string VisibilityReason => visibilityReason;
        public bool IsTouchingControlledPlayer => Time.fixedTime - lastControlledContactTime <= Time.fixedDeltaTime * 2f;
        public bool IsBlockedByControlledPlayer => Time.fixedTime - lastControlledBlockTime <= Time.fixedDeltaTime * 2f;
        public string DebugSummary =>
            $"{name}: visible={playbackVisible}, reason={visibilityReason}, t={playbackTime:0.00}/{RecordingEndTime:0.00}/{playbackVisibleUntilTime:0.00}, frames={FrameCount}, altered={alteredByControlledPlayer}, contact={IsTouchingControlledPlayer}, blocked={IsBlockedByControlledPlayer}";

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.mass = 0.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider2D>();
        }

        private void FixedUpdate()
        {
            if (frames == null || frames.Count == 0)
            {
                return;
            }

            EnsureCachedComponents();
            RecordResolvedFrameIfVisible();
            float nextPlaybackTime = playbackTime + Time.fixedDeltaTime;
            if (nextPlaybackTime > playbackVisibleUntilTime)
            {
                playbackTime = nextPlaybackTime;
                SetPlaybackVisible(false, "timeline_end");
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (nextPlaybackTime > frames[frames.Count - 1].Time)
            {
                playbackTime = nextPlaybackTime;
                frameIndex = frames.Count - 1;
                lastRecordedSamplePosition = frames[frames.Count - 1].Position;
                SetPlaybackVisible(true, "holding_after_recording_end");
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (nextPlaybackTime < frames[0].Time)
            {
                playbackTime = nextPlaybackTime;
                lastRecordedSamplePosition = frames[0].Position;
                SetPlaybackVisible(false, "waiting_first_frame");
                body.linearVelocity = Vector2.zero;
                return;
            }

            SetPlaybackVisible(true, "playing");
            Vector2 targetPosition = SamplePosition(nextPlaybackTime, out int sampledFrameIndex);
            Vector2 delta = GetDesiredDelta(targetPosition);
            if (IsControlledPlayerBlocking(delta))
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            playbackTime = nextPlaybackTime;
            frameIndex = sampledFrameIndex;
            lastRecordedSamplePosition = targetPosition;
            Vector2 desiredVelocity = delta / Time.fixedDeltaTime;
            body.linearVelocity = Vector2.ClampMagnitude(desiredVelocity, maxFollowSpeed);
        }

        public void Play(
            IReadOnlyList<ActorFrame> recording,
            Color color,
            ClonePlaybackMode mode,
            float flashSeconds,
            Color flashColor,
            Color outlineColor,
            float visibleUntilTime)
        {
            EnsureCachedComponents();
            frames = recording;
            playbackMode = mode;
            playbackVisibleUntilTime = Mathf.Max(0f, visibleUntilTime);
            playbackTime = 0f;
            frameIndex = 0;
            playbackVisible = true;
            alteredByControlledPlayer = false;
            resolvedFrames.Clear();
            lastRecordedSamplePosition = Vector2.zero;
            lastResolvedFrameTime = -999f;
            baseColor = color;
            alteredColor = Color.Lerp(color, Color.white, 0.35f);
            alteredColor.a = color.a;
            alteredFlashColor = flashColor;
            alteredFlashColor.a = color.a;
            alteredOutlineColor = outlineColor;
            alteredOutlineColor.a = Mathf.Min(outlineColor.a, color.a);
            alteredFlashSeconds = Mathf.Max(0.01f, flashSeconds);
            visibilityReason = "play_initialized";
            SetAlteredOutlineVisible(false);

            if (frames != null && frames.Count > 0)
            {
                playbackVisibleUntilTime = Mathf.Max(playbackVisibleUntilTime, frames[frames.Count - 1].Time);
                transform.position = frames[0].Position;
                lastRecordedSamplePosition = frames[0].Position;
                SetPlaybackVisible(frames[0].Time <= 0f, frames[0].Time <= 0f ? "playing" : "waiting_first_frame");
            }

            ApplyPlaybackColor(baseColor);
            RecordResolvedFrameIfVisible();
        }

        public bool TryBuildResolvedRecording(out List<ActorFrame> resolvedRecording)
        {
            resolvedRecording = null;
            if (!alteredByControlledPlayer)
            {
                return false;
            }

            RecordResolvedFrameIfVisible();
            if (resolvedFrames.Count == 0)
            {
                return false;
            }

            resolvedRecording = new List<ActorFrame>(resolvedFrames);
            if (frames != null && frames.Count > 0 && resolvedRecording[0].Time > frames[0].Time + 0.0001f)
            {
                resolvedRecording.Insert(0, frames[0]);
            }

            return true;
        }

        public void IgnorePlaybackCollisionWith(TimeClonePlayback other)
        {
            if (other == null || other == this)
            {
                return;
            }

            if (!ignoredPlaybackPeers.Contains(other))
            {
                ignoredPlaybackPeers.Add(other);
            }

            if (!other.ignoredPlaybackPeers.Contains(this))
            {
                other.ignoredPlaybackPeers.Add(this);
            }

            ApplyIgnoredPlaybackCollisions();
            other.ApplyIgnoredPlaybackCollisions();
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            PlayerActor player = collision.collider != null ? collision.collider.GetComponentInParent<PlayerActor>() : null;
            if (player != null && player.HasControl)
            {
                lastControlledContactTime = Time.fixedTime;
                MarkAlteredByControlledPlayer();
            }
        }

        private void ApplyPlaybackColor(Color color)
        {
            if (TryGetComponent(out MeshRenderer meshRenderer))
            {
                PrepareTransparentMaterial(meshRenderer.material);
                meshRenderer.material.color = color;
            }

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.color = color;
            }
        }

        private void MarkAlteredByControlledPlayer()
        {
            if (alteredByControlledPlayer)
            {
                return;
            }

            alteredByControlledPlayer = true;
            SetAlteredOutlineVisible(true);
            if (alteredFlashCoroutine != null)
            {
                StopCoroutine(alteredFlashCoroutine);
            }

            alteredFlashCoroutine = StartCoroutine(PlayAlteredFlash());
        }

        private System.Collections.IEnumerator PlayAlteredFlash()
        {
            ApplyPlaybackColor(alteredFlashColor);
            yield return new WaitForSeconds(alteredFlashSeconds);
            ApplyPlaybackColor(alteredColor);
            alteredFlashCoroutine = null;
        }

        private void SetPlaybackVisible(bool visible, string reason)
        {
            EnsureCachedComponents();
            visibilityReason = reason;
            if (playbackVisible == visible)
            {
                return;
            }

            playbackVisible = visible;

            foreach (Renderer item in renderers)
            {
                if (item != null)
                {
                    item.enabled = visible;
                }
            }

            foreach (Collider2D item in colliders)
            {
                if (item != null)
                {
                    item.enabled = visible;
                }
            }

            if (visible)
            {
                ApplyIgnoredPlaybackCollisions();
            }

            if (alteredByControlledPlayer)
            {
                SetAlteredOutlineVisible(visible);
            }
        }

        private void EnsureCachedComponents()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            renderers ??= GetComponentsInChildren<Renderer>();
            colliders ??= GetComponentsInChildren<Collider2D>();
        }

        private void RecordResolvedFrameIfVisible()
        {
            if (!playbackVisible || frames == null || frames.Count == 0)
            {
                return;
            }

            EnsureCachedComponents();
            if (Mathf.Abs(playbackTime - lastResolvedFrameTime) <= 0.0001f)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            resolvedFrames.Add(new ActorFrame(
                playbackTime,
                body.position,
                velocity,
                GetMovementDirection(velocity),
                GetMovementState(velocity)));
            lastResolvedFrameTime = playbackTime;
        }

        private static float GetMovementDirection(Vector2 velocity)
        {
            return Mathf.Abs(velocity.x) > 0.01f ? Mathf.Sign(velocity.x) : 0f;
        }

        private static ActorFrameMovementState GetMovementState(Vector2 velocity)
        {
            if (velocity.y > 0.01f)
            {
                return ActorFrameMovementState.Jumping;
            }

            if (velocity.y < -0.01f)
            {
                return ActorFrameMovementState.Falling;
            }

            return Mathf.Abs(velocity.x) > 0.01f ? ActorFrameMovementState.Moving : ActorFrameMovementState.Idle;
        }

        private void ApplyIgnoredPlaybackCollisions()
        {
            EnsureCachedComponents();
            for (int peerIndex = ignoredPlaybackPeers.Count - 1; peerIndex >= 0; peerIndex--)
            {
                TimeClonePlayback peer = ignoredPlaybackPeers[peerIndex];
                if (peer == null)
                {
                    ignoredPlaybackPeers.RemoveAt(peerIndex);
                    continue;
                }

                peer.EnsureCachedComponents();
                foreach (Collider2D ownCollider in colliders)
                {
                    if (ownCollider == null || !ownCollider.enabled || !ownCollider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    foreach (Collider2D peerCollider in peer.colliders)
                    {
                        if (peerCollider != null && peerCollider.enabled && peerCollider.gameObject.activeInHierarchy)
                        {
                            Physics2D.IgnoreCollision(ownCollider, peerCollider, true);
                        }
                    }
                }
            }
        }

        private void SetAlteredOutlineVisible(bool visible)
        {
            EnsureAlteredOutline();
            if (alteredOutline != null)
            {
                alteredOutline.SetActive(visible && playbackVisible);
            }
        }

        private void EnsureAlteredOutline()
        {
            if (alteredOutline != null)
            {
                return;
            }

            alteredOutline = GameObject.CreatePrimitive(PrimitiveType.Cube);
            alteredOutline.name = "Altered Outline";
            alteredOutline.transform.SetParent(transform, false);
            alteredOutline.transform.localPosition = new Vector3(0f, 0f, 0.04f);
            alteredOutline.transform.localScale = new Vector3(1.18f, 1.12f, 1.18f);

            Collider outlineCollider = alteredOutline.GetComponent<Collider>();
            if (outlineCollider != null)
            {
                Destroy(outlineCollider);
            }

            MeshRenderer outlineRenderer = alteredOutline.GetComponent<MeshRenderer>();
            outlineRenderer.material = BuildRuntimeMaterial("AlteredCloneOutlineMaterial", alteredOutlineColor);
            alteredOutline.SetActive(false);
        }

        private Vector2 GetDesiredDelta(Vector2 targetPosition)
        {
            if (playbackMode == ClonePlaybackMode.PreservePhysicalOffset)
            {
                return targetPosition - lastRecordedSamplePosition;
            }

            return targetPosition - body.position;
        }

        private Vector2 SamplePosition(float sampleTime, out int sampledFrameIndex)
        {
            sampledFrameIndex = frameIndex;
            while (sampledFrameIndex < frames.Count - 2 && frames[sampledFrameIndex + 1].Time < sampleTime)
            {
                sampledFrameIndex++;
            }

            ActorFrame from = frames[sampledFrameIndex];
            ActorFrame to = frames[Mathf.Min(sampledFrameIndex + 1, frames.Count - 1)];
            float duration = Mathf.Max(to.Time - from.Time, 0.0001f);
            float t = Mathf.Clamp01((sampleTime - from.Time) / duration);
            return Vector2.Lerp(from.Position, to.Position, t);
        }

        private bool IsControlledPlayerBlocking(Vector2 desiredDelta)
        {
            if (!playbackVisible || desiredDelta.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Vector2 direction = desiredDelta.normalized;
            float distance = desiredDelta.magnitude + 0.02f;

            foreach (Collider2D item in colliders)
            {
                if (item == null || !item.enabled)
                {
                    continue;
                }

                int hitCount = item.Cast(direction, castHits, distance);
                for (int i = 0; i < hitCount; i++)
                {
                    Collider2D hit = castHits[i].collider;
                    PlayerActor player = hit != null ? hit.GetComponentInParent<PlayerActor>() : null;
                    if (player != null && player.HasControl)
                    {
                        lastControlledBlockTime = Time.fixedTime;
                        MarkAlteredByControlledPlayer();
                        return true;
                    }
                }
            }

            return false;
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
