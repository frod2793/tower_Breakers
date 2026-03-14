using System;
using UnityEngine;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 플레이어 데미지 수신 이벤트입니다.
    /// </summary>
    public struct OnPlayerDamaged
    {
        public int Damage;
        public int RemainingLifeCount;

        public OnPlayerDamaged(int damage, int remainingLifeCount)
        {
            Damage = damage;
            RemainingLifeCount = remainingLifeCount;
        }
    }

    /// <summary>
    /// [설명]: 플레이어 밀림 발생 이벤트입니다.
    /// </summary>
    public struct OnPlayerPushed
    {
        public float PushDistance;

        public OnPlayerPushed(float pushDistance)
        {
            PushDistance = pushDistance;
        }
    }

    /// <summary>
    /// [설명]: 플레이어 동작 실행 이벤트입니다.
    /// </summary>
    public struct OnPlayerActionStarted
    {
        public PlayerActionType ActionType;

        public OnPlayerActionStarted(PlayerActionType actionType)
        {
            ActionType = actionType;
        }
    }

    /// <summary>
    /// [설명]: 방어(디펜스) 액션이 실행되었음을 알리는 이벤트입니다.
    /// 모든 적 군집은 이 이벤트를 수신하여 경직 상태로 전환됩니다.
    /// </summary>
    public struct OnDefendActionTriggered
    {
        public float StunDuration;
        public float PushbackDistance;
        public float DefendRange;
        public int FloorIndex;
        public UnityEngine.Vector3 PlayerPosition;

        public OnDefendActionTriggered(float duration, float pushbackDistance, float defendRange, int floorIndex, UnityEngine.Vector3 playerPosition)
        {
            StunDuration = duration;
            PushbackDistance = pushbackDistance;
            DefendRange = defendRange;
            FloorIndex = floorIndex;
            PlayerPosition = playerPosition;
        }
    }

    /// <summary>
    /// [설명]: 플레이어의 층이 변경되었음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnPlayerFloorChanged
    {
        public int NewFloorIndex;
        public OnPlayerFloorChanged(int index) => NewFloorIndex = index;
    }

    /// <summary>
    /// [설명]: 플레이어의 공격이 적에게 명중했을 때 발생하는 이벤트입니다.
    /// </summary>
    public struct OnPlayerAttackLanded
    {
        public int TargetEnemyId;
        public float StunDuration;
        public float PushbackDistance;

        public OnPlayerAttackLanded(int id, float stun, float push)
        {
            TargetEnemyId = id;
            StunDuration = stun;
            PushbackDistance = push;
        }
    }
}
