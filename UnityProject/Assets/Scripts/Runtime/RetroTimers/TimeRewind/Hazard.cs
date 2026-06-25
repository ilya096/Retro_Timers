using UnityEngine;

namespace RetroTimers.TimeRewind
{
    public sealed class Hazard : MonoBehaviour
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
            PlayerActor player = other.GetComponentInParent<PlayerActor>();
            if (player != null)
            {
                player.Kill();
                controller?.NotifyActorDied();
                return;
            }

            if (other.GetComponentInParent<TimeClonePlayback>() != null)
            {
                controller?.NotifyActorDied();
            }
        }
    }
}
