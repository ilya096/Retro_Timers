using UnityEngine;

namespace RetroTimers.TimeRewind
{
    public sealed class PressurePlateDoor : MonoBehaviour, IRewindResettable
    {
        [SerializeField] private Collider2D detectionArea;
        [SerializeField] private GameObject door;
        [SerializeField] private Collider2D doorCollider;
        [SerializeField] private float activeCheckPadding = 0.02f;

        private bool startDoorActive;

        private void Awake()
        {
            if (detectionArea == null)
            {
                detectionArea = GetComponent<Collider2D>();
            }

            CaptureRewindStartState();
        }

        private void Update()
        {
            bool pressed = IsPressed();
            SetDoorOpen(pressed);
        }

        public void CaptureRewindStartState()
        {
            startDoorActive = door == null || door.activeSelf;
        }

        public void ResetForRewind()
        {
            if (door != null)
            {
                door.SetActive(startDoorActive);
            }

            if (doorCollider != null)
            {
                doorCollider.enabled = startDoorActive;
            }
        }

        private bool IsPressed()
        {
            if (detectionArea == null)
            {
                return false;
            }

            Bounds bounds = detectionArea.bounds;
            Vector2 size = new Vector2(bounds.size.x + activeCheckPadding, bounds.size.y + activeCheckPadding);
            Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, size, 0f);

            foreach (Collider2D hit in hits)
            {
                if (hit == detectionArea)
                {
                    continue;
                }

                if (hit.GetComponentInParent<PlayerActor>() != null || hit.GetComponentInParent<TimeClonePlayback>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetDoorOpen(bool open)
        {
            if (door != null)
            {
                door.SetActive(!open);
            }

            if (doorCollider != null)
            {
                doorCollider.enabled = !open;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (detectionArea == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(detectionArea.bounds.center, detectionArea.bounds.size);
        }
    }
}
