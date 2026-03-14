using System.Collections.Generic;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.Logic;

namespace TowerBreakers.Enemy.Boss.AI.FSM
{
    /// <summary>
    /// [설명]: 보스 이름별 페이즈 생성을 담당하는 팩토리 인터페이스입니다.
    /// </summary>
    public interface IBossPhaseFactory
    {
        List<IBossPhase> CreatePhases(EnemyData data, BossFSM controller);
    }
}
