using System;
using UnityEngine;

namespace RetroTimers.TimeRewind
{
    public enum ActorFrameMovementState
    {
        Idle = 0,
        Moving = 1,
        Jumping = 2,
        Falling = 3
    }

    [Serializable]
    public readonly struct ActorFrame
    {
        public readonly float Time;
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;
        public readonly float MovementDirection;
        public readonly ActorFrameMovementState MovementState;

        public ActorFrame(
            float time,
            Vector2 position,
            Vector2 velocity,
            float movementDirection = 0f,
            ActorFrameMovementState movementState = ActorFrameMovementState.Idle)
        {
            Time = time;
            Position = position;
            Velocity = velocity;
            MovementDirection = movementDirection;
            MovementState = movementState;
        }
    }
}
