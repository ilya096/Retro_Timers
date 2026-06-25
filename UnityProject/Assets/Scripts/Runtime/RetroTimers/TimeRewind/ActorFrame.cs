using System;
using UnityEngine;

namespace RetroTimers.TimeRewind
{
    [Serializable]
    public readonly struct ActorFrame
    {
        public readonly float Time;
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;

        public ActorFrame(float time, Vector2 position, Vector2 velocity)
        {
            Time = time;
            Position = position;
            Velocity = velocity;
        }
    }
}
