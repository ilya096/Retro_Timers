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
        private Rigidbody2D body;
        private Renderer[] renderers;
        private Collider2D[] colliders;
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
        private float playbackTime;
        private int frameIndex;
        private bool playbackVisible;
        private Vector2 lastRecordedSamplePosition;

        public ClonePlaybackMode PlaybackMode
        {
            get => playbackMode;
            set => playbackMode = value;
        }

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
            float nextPlaybackTime = playbackTime + Time.fixedDeltaTime;
            if (nextPlaybackTime < frames[0].Time)
            {
                playbackTime = nextPlaybackTime;
                lastRecordedSamplePosition = frames[0].Position;
                SetPlaybackVisible(false);
                body.linearVelocity = Vector2.zero;
                return;
            }

            SetPlaybackVisible(true);
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

        public void Play(IReadOnlyList<ActorFrame> recording, Color color, ClonePlaybackMode mode)
        {
            EnsureCachedComponents();
            frames = recording;
            playbackMode = mode;
            playbackTime = 0f;
            frameIndex = 0;
            playbackVisible = true;
            lastRecordedSamplePosition = Vector2.zero;

            if (frames != null && frames.Count > 0)
            {
                transform.position = frames[0].Position;
                lastRecordedSamplePosition = frames[0].Position;
                SetPlaybackVisible(frames[0].Time <= 0f);
            }

            if (TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.material.color = color;
            }

            if (TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                spriteRenderer.color = color;
            }
        }

        private void SetPlaybackVisible(bool visible)
        {
            EnsureCachedComponents();
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
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
