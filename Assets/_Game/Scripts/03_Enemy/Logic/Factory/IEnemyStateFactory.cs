using System;
using System.Collections.Generic;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.View;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적 타입별 상태 생성을 담당하는 팩토리 인터페이스입니다.
    /// </summary>
    public interface IEnemyStateFactory
    {
        void CreateStates(EnemyStateMachine stateMachine, EnemyView view, EnemyData data, EnemyPushLogic pushLogic, IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory, EnemyController controller);
        Type GetInitialStateType(EnemyType type);
        Type GetReturnStateType(EnemyType type);
    }
}
