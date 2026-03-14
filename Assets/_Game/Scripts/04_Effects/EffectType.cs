namespace TowerBreakers.Effects
{
    /// <summary>
    /// [설명]: 게임 내 모든 시각 효과(VFX)의 식별 타입입니다.
    /// </summary>
    public enum EffectType
    {
        None = 0,
        
        // 공용 이펙트
        Explosion,
        Hit,
        Dust,
        Slash,
        
        // 플레이어 전용 이펙트
        HeartGain,
        BasicHit,
        SkillActivate,
        LevelUp,
        
        // 적 전용 이펙트
        EnemyDeath,
        BossImpact,
        
        // 환경/오브젝트 이펙트
        ChestOpen
    }
}
