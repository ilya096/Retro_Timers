using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RetroTimers.TimeRewind
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerActor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpVelocity = 12f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.12f;

        private readonly List<ActorFrame> recording = new();
        private Rigidbody2D body;
        private bool controlEnabled;
        private bool recordingEnabled;
        private bool jumpRequested;
        private float horizontalInput;
        private float iterationTime;

        public bool IsAlive { get; private set; } = true;
        public bool HasControl => controlEnabled;
        public IReadOnlyList<ActorFrame> Recording => recording;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (groundCheck == null)
            {
                groundCheck = transform;
            }
        }

        private void Update()
        {
            if (!controlEnabled || !IsAlive)
            {
                horizontalInput = 0f;
                return;
            }

            horizontalInput = ReadHorizontalInput();
            if (WasJumpPressed())
            {
                jumpRequested = true;
            }
        }

        private void FixedUpdate()
        {
            if (controlEnabled && IsAlive)
            {
                ApplyMovement();
            }

            if (recordingEnabled)
            {
                recording.Add(new ActorFrame(
                    iterationTime,
                    body.position,
                    body.linearVelocity,
                    GetMovementDirection(),
                    GetMovementState()));
            }

            iterationTime += Time.fixedDeltaTime;
        }

        public void BeginIteration(bool enableRecording, float initialIterationTime = 0f)
        {
            IsAlive = true;
            iterationTime = Mathf.Max(0f, initialIterationTime);
            recordingEnabled = enableRecording;
            recording.Clear();
        }

        public void SetControlEnabled(bool enabled)
        {
            controlEnabled = enabled && IsAlive;
            if (!controlEnabled)
            {
                horizontalInput = 0f;
                jumpRequested = false;
            }
        }

        public List<ActorFrame> FinishRecording()
        {
            recordingEnabled = false;
            SetControlEnabled(false);
            return new List<ActorFrame>(recording);
        }

        public void Kill()
        {
            IsAlive = false;
            SetControlEnabled(false);
        }

        private void ApplyMovement()
        {
            body.linearVelocity = new Vector2(horizontalInput * moveSpeed, body.linearVelocity.y);

            if (jumpRequested && IsGrounded())
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
            }

            jumpRequested = false;
        }

        private bool IsGrounded()
        {
            return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask) != null;
        }

        private float GetMovementDirection()
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                return Mathf.Sign(horizontalInput);
            }

            if (Mathf.Abs(body.linearVelocity.x) > 0.01f)
            {
                return Mathf.Sign(body.linearVelocity.x);
            }

            return 0f;
        }

        private ActorFrameMovementState GetMovementState()
        {
            if (!IsGrounded())
            {
                return body.linearVelocity.y > 0f ? ActorFrameMovementState.Jumping : ActorFrameMovementState.Falling;
            }

            return Mathf.Abs(body.linearVelocity.x) > 0.01f ? ActorFrameMovementState.Moving : ActorFrameMovementState.Idle;
        }

        private static float ReadHorizontalInput()
        {
            float horizontal = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                {
                    horizontal -= 1f;
                }

                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    horizontal += 1f;
                }
            }

            if (Gamepad.current != null)
            {
                horizontal += Gamepad.current.leftStick.ReadValue().x;
            }

            return Mathf.Clamp(horizontal, -1f, 1f);
        }

        private static bool WasJumpPressed()
        {
            bool keyboardPressed = Keyboard.current != null &&
                                   (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame);
            bool gamepadPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

            return keyboardPressed || gamepadPressed;
        }

        private void OnDrawGizmosSelected()
        {
            Transform check = groundCheck != null ? groundCheck : transform;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(check.position, groundCheckRadius);
        }
    }
}
