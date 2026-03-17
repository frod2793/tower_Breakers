using System;
using UnityEngine;

namespace TowerBreakers.Player.Data
{
    public enum PlayerActionState
    {
        Idle,
        Moving,
        Dashing,
        Parrying,
        Attacking,
        Retreating
    }

    public class PlayerStateDTO
    {
        public Vector3 Position { get; set; }
        public PlayerActionState ActionState { get; set; }
        public float LastDashTime { get; set; }
        public float LastParryTime { get; set; }
        public float LastAttackTime { get; set; }
        public Vector3 DashTargetPosition { get; set; }
        public Vector3 RetreatTargetPosition { get; set; }
        public bool IsActionLocked { get; set; }

        public PlayerStateDTO()
        {
            Position = Vector3.zero;
            ActionState = PlayerActionState.Idle;
            LastDashTime = -100f;
            LastParryTime = -100f;
            LastAttackTime = -100f;
            DashTargetPosition = Vector3.zero;
            RetreatTargetPosition = Vector3.zero;
            IsActionLocked = false;
        }

        public void Reset()
        {
            ActionState = PlayerActionState.Idle;
            IsActionLocked = false;
        }
    }
}
