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
    /// [이벤트]: 적이 사망했을 때 발행됩니다.
    /// </summary>
    public struct OnEnemyKilled
    {
        public UnityEngine.GameObject EnemyObject;
    }
    #endregion
}
