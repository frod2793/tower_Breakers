using System.Collections.Generic;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    #region Robot Gunner Phases
    /// <summary>
    /// [설명]: 로봇(거너)의 1페이즈입니다.
    /// </summary>
    public class RobotGunnerPhase1 : BossPhaseBase
    {
        public override int PhaseIndex => 0;

        public RobotGunnerPhase1(ProjectileFactory factory, IEventBus eventBus)
        {
            Patterns.Add(new RobotGunnerAttackPattern(factory, eventBus));
        }

        public override bool ShouldChangePhase(int currentHp, int maxHp)
        {
            return false; // 단일 페이즈 샘플
        }
    }
    #endregion

    #region Robot Shield Phases
    /// <summary>
    /// [설명]: 로봇(쉴드)의 1페이즈입니다.
    /// </summary>
    public class RobotShieldPhase1 : BossPhaseBase
    {
        public override int PhaseIndex => 0;

        public RobotShieldPhase1(IEventBus eventBus)
        {
            Patterns.Add(new RobotShieldActivePattern(eventBus));
        }

        public override bool ShouldChangePhase(int currentHp, int maxHp)
        {
            return false; // 단일 페이즈 샘플
        }
    }
    #endregion
}
