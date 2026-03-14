using System;
using UnityEngine;
using TowerBreakers.Enemy.Data;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 적 처치 이벤트입니다.
    /// </summary>
    public struct OnEnemyKilled
    {
        public int EnemyId;
        public int FloorIndex;
        public EnemyType EnemyType;

        public OnEnemyKilled(int enemyId, int floorIndex, EnemyType enemyType)
        {
            EnemyId = enemyId;
            FloorIndex = floorIndex;
            EnemyType = enemyType;
        }
    }

    /// <summary>
    /// [설명]: 적 서포터가 아군에게 버프(회복)를 제공할 때 발행되는 이벤트입니다.
    /// </summary>
    public struct OnEnemyBuffRequested
    {
        public int FloorIndex;
        public int HealAmount;

        public OnEnemyBuffRequested(int floorIndex, int healAmount)
        {
            FloorIndex = floorIndex;
            HealAmount = healAmount;
        }
    }

    /// <summary>
    /// [설명]: 적 캐릭터가 데미지를 받았음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnEnemyDamaged
    {
        public int EnemyId;
        public int Damage;
        public int CurrentHp;
        public int MaxHp;

        public OnEnemyDamaged(int id, int damage, int hp, int max)
        {
            EnemyId = id;
            Damage = damage;
            CurrentHp = hp;
            MaxHp = max;
        }
    }

    /// <summary>
    /// [설명]: 크라켄의 소환 패턴을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnKrakenSummonRequested
    {
        public enum SummonType
        {
            Tentacle,
            FallingTentacle,
            StrikeTentacle
        }

        public SummonType Type;
        public int FloorIndex;
        public UnityEngine.Vector3 Position;

        public OnKrakenSummonRequested(SummonType type, int floorIndex, UnityEngine.Vector3 position)
        {
            Type = type;
            FloorIndex = floorIndex;
            Position = position;
        }
    }

    /// <summary>
    /// [설명]: 크라켄 소환물이 파괴되었음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnKrakenTentacleDestroyed
    {
        public int FloorIndex;
        public OnKrakenTentacleDestroyed(int floorIndex) => FloorIndex = floorIndex;
    }

    /// <summary>
    /// [설명]: 크라켄 소환물이 데미지를 받았을 때 본체로 전이하기 위한 이벤트입니다.
    /// </summary>
    public struct OnKrakenSummonDamaged
    {
        public float Damage;
        public OnKrakenSummonDamaged(float damage) => Damage = damage;
    }

    /// <summary>
    /// [설명]: 특정 층의 촉수에게 동작(공격 등)을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnKrakenTentacleActionRequested
    {
        public enum ActionType { Falling, Strike, Idle }

        public int FloorIndex;
        public ActionType Type;
        public Vector3 TargetPosition;

        public OnKrakenTentacleActionRequested(int floorIndex, ActionType type, Vector3 targetPosition = default)
        {
            FloorIndex = floorIndex;
            Type = type;
            TargetPosition = targetPosition;
        }
    }
}
