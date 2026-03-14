using System;
using TowerBreakers.Player.Data.SO;

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
    /// [설명]: 현재 층의 적을 모두 처치하여 다음 층으로 갈 준비가 되었음을 알리는 이벤트입니다.
    /// HUD에서 'GO' UI를 표시하는 데 사용됩니다.
    /// </summary>
    public struct OnFloorReadyForNext { }

    /// <summary>
    /// [설명]: 현재 층의 모든 적이 처치되었음을 알리는 이벤트입니다.
    /// 보상 상자가 있다면 이 이벤트를 통해 활성화됩니다.
    /// </summary>
    public struct OnFloorEnemiesCleared
    {
        public int FloorIndex;
        public OnFloorEnemiesCleared(int floorIndex) => FloorIndex = floorIndex;
    }

    /// <summary>
    /// [설명]: 보상 상자가 씬에 존재함을 알리는 이벤트입니다 (TowerManager 등록용).
    /// </summary>
    public struct OnRewardChestRegistered
    {
        public int FloorIndex;
        public OnRewardChestRegistered(int floorIndex) => FloorIndex = floorIndex;
    }

    /// <summary>
    /// [설명]: 상자가 열리고 실제 보상 아이템이 결정되었을 때 연출을 위해 발행됩니다.
    /// </summary>
    public struct OnRewardSpawned
    {
        public string RewardKey;
        public UnityEngine.Vector3 Position;
        public int FloorIndex;

        public OnRewardSpawned(string rewardKey, UnityEngine.Vector3 position, int floorIndex)
        {
            RewardKey = rewardKey;
            Position = position;
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 특정 층의 전투가 시작됨을 알리는 이벤트입니다.
    /// 선스폰된 적들이 이 이벤트를 수신하여 진격을 시작합니다.
    /// </summary>
    public struct OnFloorStarted
    {
        public int FloorIndex;

        public OnFloorStarted(int floorIndex)
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
        public int RemainingLifeCount;

        public OnPlayerDamaged(int damage, int remainingLifeCount)
        {
            Damage = damage;
            RemainingLifeCount = remainingLifeCount;
        }
    }

    /// <summary>
    /// [설명]: 적 처치 이벤트입니다.
    /// </summary>
    public struct OnEnemyKilled
    {
        public int EnemyId;
        public int FloorIndex;
        public TowerBreakers.Enemy.Data.EnemyType EnemyType;

        public OnEnemyKilled(int enemyId, int floorIndex, TowerBreakers.Enemy.Data.EnemyType enemyType)
        {
            EnemyId = enemyId;
            FloorIndex = floorIndex;
            EnemyType = enemyType;
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
    /// [설명]: 플레이어가 벽에 압착되어 데미지를 받았을 때 발행되는 이벤트입니다.
    /// 모든 적은 이 이벤트를 수신하여 동결(Frozen) 상태로 전환됩니다.
    /// </summary>
    public struct OnWallCrushOccurred
    {
        public int Damage;
        public int FloorIndex;

        public OnWallCrushOccurred(int damage, int floorIndex)
        {
            Damage = damage;
            FloorIndex = floorIndex;
        }
    }

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
        public TowerBreakers.Player.Logic.PlayerActionType ActionType;

        public OnPlayerActionStarted(TowerBreakers.Player.Logic.PlayerActionType actionType)
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

    /// <summary>
    /// [설명]: 데미지 텍스트 생성을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnDamageTextRequested
    {
        public UnityEngine.Vector3 Position;
        public int Damage;
        public bool IsCritical;

        public OnDamageTextRequested(UnityEngine.Vector3 position, int damage, bool isCritical = false)
        {
            Position = position;
            Damage = damage;
            IsCritical = isCritical;
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
    /// [설명]: 보물상자(보상) 획득 이벤트입니다.
    /// </summary>
    public struct OnChestCollected
    {
        public int Count;

        public OnChestCollected(int count = 1)
        {
            Count = count;
        }
    }

    /// <summary>
    /// [설명]: 보상 상자가 특정 위치에 스폰되었음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnRewardChestSpawned
    {
        public UnityEngine.Vector3 Position;
        public int FloorIndex;

        public OnRewardChestSpawned(UnityEngine.Vector3 position, int floorIndex)
        {
            Position = position;
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 보상 상자가 플레이어에 의해 열렸음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnRewardChestOpened
    {
        public UnityEngine.Vector3 Position;
        public int FloorIndex;
        public RewardTableData RewardTable;

        public OnRewardChestOpened(UnityEngine.Vector3 position, int floorIndex, RewardTableData rewardTable)
        {
            Position = position;
            FloorIndex = floorIndex;
            RewardTable = rewardTable;
        }
    }
}
