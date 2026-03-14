using System.Collections.Generic;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 고블린 족장의 1페이즈입니다. (HP 50% 이상)
    /// </summary>
    public class GoblinChiefPhase1 : BossPhaseBase
    {
        public override int PhaseIndex => 0;

        public GoblinChiefPhase1(ProjectileFactory factory, IEventBus eventBus)
        {
            Patterns.Add(new GoblinChiefSwingPattern(eventBus));
            Patterns.Add(new GoblinChiefTotemPattern(factory, eventBus));
        }

        public override bool ShouldChangePhase(int currentHp, int maxHp)
        {
            return currentHp <= maxHp * 0.5f;
        }
    }

    /// <summary>
    /// [설명]: 고블린 족장의 2페이즈입니다. (HP 50% 이하)
    /// </summary>
    public class GoblinChiefPhase2 : BossPhaseBase
    {
        public override int PhaseIndex => 1;

        public GoblinChiefPhase2(ProjectileFactory factory, IEventBus eventBus)
        {
            Patterns.Add(new GoblinChiefJumpPattern(eventBus));
            Patterns.Add(new GoblinChiefSwingPattern(eventBus));
            Patterns.Add(new GoblinChiefTotemPattern(factory, eventBus));
        }

        public override bool ShouldChangePhase(int currentHp, int maxHp)
        {
            // 마지막 페이즈이므로 false 반환
            return false;
        }
    }
}
