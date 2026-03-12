using System;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 층 클리어 이벤트입니다.
    /// </summary>
    public struct OnFloorCleared
    {
        public int FloorIndex;

        public OnFloorCleared(int floorIndex)
        {
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 플레이어 데미지 수신 이벤트입니다.
    /// </summary>
    public struct OnPlayerDamaged
    {
        public int Damage;
        public int RemainingHp;

        public OnPlayerDamaged(int damage, int remainingHp)
        {
            Damage = damage;
            RemainingHp = remainingHp;
        }
    }

    /// <summary>
    /// [설명]: 적 처치 이벤트입니다.
    /// </summary>
    public struct OnEnemyKilled
    {
        public int EnemyId;

        public OnEnemyKilled(int enemyId)
        {
            EnemyId = enemyId;
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
    /// [설명]: 게임 오버 이벤트입니다.
    /// </summary>
    public struct OnGameOver { }

    /// <summary>
    /// [설명]: 게임 시작 이벤트입니다.
    /// </summary>
    public struct OnGameStart
    {
        public int TowerId;

        public OnGameStart(int towerId)
        {
            TowerId = towerId;
        }
    }

    /// <summary>
    /// [설명]: 플레이어 동작 실행 이벤트입니다.
    /// </summary>
    public struct OnPlayerActionStarted
    {
        public string ActionName;

        public OnPlayerActionStarted(string actionName)
        {
            ActionName = actionName;
        }
    }

    /// <summary>
    /// [설명]: 방어(디펜스) 액션이 실행되었음을 알리는 이벤트입니다.
    /// 모든 적 군집은 이 이벤트를 수신하여 경직 상태로 전환됩니다.
    /// </summary>
    public struct OnDefendActionTriggered
    {
        public float StunDuration;

        public OnDefendActionTriggered(float duration)
        {
            StunDuration = duration;
        }
    }

    /// <summary>
    /// [설명]: 타격 발생 시 연출(쉐이크, 역경직 등)을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnHitEffectRequested
    {
        public UnityEngine.Vector3 Position;
        public float ShakeIntensity;
        public float ShakeDuration;
        public float HitStopDuration;

        public OnHitEffectRequested(UnityEngine.Vector3 position, float intensity = 0.5f, float duration = 0.2f, float hitStop = 0.05f)
        {
            Position = position;
            ShakeIntensity = intensity;
            ShakeDuration = duration;
            HitStopDuration = hitStop;
        }
    }
}
