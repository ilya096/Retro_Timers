using UnityEngine;

namespace RetroTimers.TimeRewind
{
    public sealed class LevelExit : MonoBehaviour
    {
        [SerializeField] private TimeRewindController controller;

        private void Awake()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<TimeRewindController>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (controller == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerActor>() != null || other.GetComponentInParent<TimeClonePlayback>() != null)
            {
                controller.NotifyExitReached();
            }
        }
    }
}
