namespace TowerBreakers.Core.Events
{
    #region 게임 상태 이벤트
    /// <summary>
    /// [이벤트]: 게임이 시작될 때 발행됩니다.
    /// </summary>
    public struct OnGameStart { }

    /// <summary>
    /// [이벤트]: 게임 오버 시 발행됩니다.
    /// </summary>
    public struct OnGameOver 
    { 
        public bool IsVictory; 
    }
    #endregion

    #region 플레이어 관련 이벤트
    /// <summary>
    /// [이벤트]: 플레이어가 데미지를 입었을 때 발행됩니다.
    /// </summary>
    public struct OnPlayerDamaged
    {
        public int Damage;
        public int CurrentHealth;
        public int MaxHealth;
    }

    /// <summary>
    /// [이벤트]: 플레이어가 벽에 압착되었을 때 발행됩니다.
    /// </summary>
    public struct OnWallCrushOccurred { }

    /// <summary>
    /// [이벤트]: 플레이어가 패링을 성공적으로 수행했을 때 발행됩니다.
    /// </summary>
    public struct OnParryPerformed { }
    #endregion

    #region 타워 및 적 관련 이벤트
    /// <summary>
    /// [이벤트]: 새로운 층의 전투가 시작될 때 발행됩니다.
    /// </summary>
    public struct OnFloorStarted
    {
        public int FloorNumber;
    }

    /// <summary>
    /// [이벤트]: 현재 층의 모든 적을 처치했을 때 발행됩니다.
    /// </summary>
    public struct OnFloorCleared
    {
        public int FloorNumber;
    }

    /// <summary>
    /// [이벤트]: 적 사망 시 발행됩니다.
    /// </summary>
    public struct OnEnemyKilled
    {
        public UnityEngine.GameObject EnemyObject;
        public TowerBreakers.Tower.Data.EnemyType EnemyType; // [추가]
    }

    /// <summary>
    /// [이벤트]: 남은 적 개수가 변경되었을 때 발행됩니다. (상세 아이콘 표시용)
    /// </summary>
    public struct OnEnemyCountChanged
    {
        public int NormalRemaining;
        public int NormalTotal;
        public int EliteRemaining;
        public int EliteTotal;
        public int BossRemaining;
        public int BossTotal;

        public int TotalRemaining => NormalRemaining + EliteRemaining + BossRemaining;
        public int TotalTotal => NormalTotal + EliteTotal + BossTotal;
    }
    #endregion
    }

