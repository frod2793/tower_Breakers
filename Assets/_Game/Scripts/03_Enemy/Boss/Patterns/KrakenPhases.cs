using System.Collections.Generic;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.Logic;

namespace TowerBreakers.Enemy.Logic
{
    public class KrakenPhase1 : BossPhaseBase
    {
        public override int PhaseIndex => 0;
        public KrakenPhase1(ProjectileFactory factory, IEventBus eventBus, KrakenBossState krakenState)
        {
            Patterns.Add(new KrakenTentaclePattern(eventBus, krakenState, true));
            Patterns.Add(new KrakenArtilleryPattern(factory, eventBus));
        }
        public override bool ShouldChangePhase(int currentHp, int maxHp) => currentHp <= maxHp * 0.5f;
    }

    public class KrakenPhase2 : BossPhaseBase
    {
        public override int PhaseIndex => 1;
        public KrakenPhase2(ProjectileFactory factory, IEventBus eventBus, KrakenBossState krakenState)
        {
            Patterns.Add(new KrakenTentaclePattern(eventBus, krakenState, false));
            Patterns.Add(new KrakenArtilleryPattern(factory, eventBus));
            Patterns.Add(new KrakenTentaclePattern(eventBus, krakenState, true));
            Patterns.Add(new KrakenSummonTentaclePattern(eventBus, krakenState));
        }
        public override bool ShouldChangePhase(int currentHp, int maxHp) => false;
    }
}
