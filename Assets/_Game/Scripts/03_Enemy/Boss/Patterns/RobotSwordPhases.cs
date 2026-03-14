using System.Collections.Generic;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    public class RobotSwordPhase1 : BossPhaseBase
    {
        public override int PhaseIndex => 0;
        public RobotSwordPhase1(IEventBus eventBus)
        {
            Patterns.Add(new RobotSwordDashPattern(eventBus));
        }
        public override bool ShouldChangePhase(int currentHp, int maxHp) => false;
    }
}
