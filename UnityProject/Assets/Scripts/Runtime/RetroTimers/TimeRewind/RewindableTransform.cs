using UnityEngine;

namespace RetroTimers.TimeRewind
{
    public sealed class RewindableTransform : MonoBehaviour, IRewindResettable
    {
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector3 startScale;
        private Rigidbody2D body;
        private Vector2 startVelocity;
        private float startAngularVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            CaptureRewindStartState();
        }

        public void CaptureRewindStartState()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
            startScale = transform.localScale;

            if (body == null)
            {
                return;
            }

            startVelocity = body.linearVelocity;
            startAngularVelocity = body.angularVelocity;
        }

        public void ResetForRewind()
        {
            transform.SetPositionAndRotation(startPosition, startRotation);
            transform.localScale = startScale;

            if (body == null)
            {
                return;
            }

            body.linearVelocity = startVelocity;
            body.angularVelocity = startAngularVelocity;
        }
    }
}
