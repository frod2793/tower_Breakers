using System;
using System.Collections.Generic;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Core.Events;
using TowerBreakers.Enemy.View;
using TowerBreakers.Player.Logic;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적 타입별 상태 생성을 담당하는 팩토리 구현체입니다.
    /// OCP 원칙을 적용하여 새로운 적 타입 추가 시 코드 변경 없이 확장 가능합니다.
    /// </summary>
    public sealed class EnemyStateFactory : IEnemyStateFactory
    {
        private readonly Dictionary<EnemyType, Action<EnemyStateMachine, EnemyView, EnemyData, EnemyPushLogic, IEventBus, int, ProjectileFactory, EnemyController>> m_stateCreators;
        private readonly Dictionary<EnemyType, Type> m_initialStateTypes;
        private readonly Dictionary<EnemyType, Type> m_returnStateTypes;

        public EnemyStateFactory()
        {
            m_stateCreators = new Dictionary<EnemyType, Action<EnemyStateMachine, EnemyView, EnemyData, EnemyPushLogic, IEventBus, int, ProjectileFactory, EnemyController>>
            {
                { EnemyType.SupportBuffer, CreateSupportBufferStates },
                { EnemyType.SupportShooter, CreateSupportShooterStates },
                { EnemyType.Boss, CreateBossStates },
                { EnemyType.Normal, CreateNormalStates },
                { EnemyType.Tank, CreateNormalStates },
                { EnemyType.Elite, CreateNormalStates }
            };

            m_initialStateTypes = new Dictionary<EnemyType, Type>
            {
                { EnemyType.SupportBuffer, typeof(EnemySupportPushState) },
                { EnemyType.SupportShooter, typeof(EnemySupportPushState) },
                { EnemyType.Boss, null },
                { EnemyType.Normal, typeof(EnemyPushState) },
                { EnemyType.Tank, typeof(EnemyPushState) },
                { EnemyType.Elite, typeof(EnemyPushState) }
            };

            m_returnStateTypes = new Dictionary<EnemyType, Type>
            {
                { EnemyType.SupportBuffer, typeof(EnemySupportPushState) },
                { EnemyType.SupportShooter, typeof(EnemySupportPushState) },
                { EnemyType.Boss, null },
                { EnemyType.Normal, typeof(EnemyPushState) },
                { EnemyType.Tank, typeof(EnemyPushState) },
                { EnemyType.Elite, typeof(EnemyPushState) }
            };
        }

        public void CreateStates(EnemyStateMachine stateMachine, EnemyView view, EnemyData data, EnemyPushLogic pushLogic, IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory, EnemyController controller)
        {
            if (m_stateCreators.TryGetValue(data.Type, out var creator))
            {
                creator(stateMachine, view, data, pushLogic, eventBus, floorIndex, projectileFactory, controller);
            }
            else
            {
                CreateNormalStates(stateMachine, view, data, pushLogic, eventBus, floorIndex, projectileFactory, controller);
            }
        }

        public Type GetInitialStateType(EnemyType type)
        {
            return m_initialStateTypes.TryGetValue(type, out var stateType) ? stateType : typeof(EnemyPushState);
        }

        public Type GetReturnStateType(EnemyType type)
        {
            return m_returnStateTypes.TryGetValue(type, out var stateType) ? stateType : typeof(EnemyPushState);
        }

        private void CreateSupportBufferStates(EnemyStateMachine stateMachine, EnemyView view, EnemyData data, EnemyPushLogic pushLogic, IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory, EnemyController controller)
        {
            stateMachine.AddState(new EnemySupportPushState(view, data, pushLogic, stateMachine, typeof(EnemyBuffState), null));
            stateMachine.AddState(new EnemyBuffState(view, data, stateMachine, eventBus, floorIndex));
        }

        private void CreateSupportShooterStates(EnemyStateMachine stateMachine, EnemyView view, EnemyData data, EnemyPushLogic pushLogic, IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory, EnemyController controller)
        {
            var playerTarget = pushLogic?.PlayerReceiver;
            stateMachine.AddState(new EnemySupportPushState(view, data, pushLogic, stateMachine, typeof(EnemyShootState), null));
            stateMachine.AddState(new EnemyShootState(view, data, stateMachine, projectileFactory, playerTarget));
        }

        private void CreateBossStates(EnemyStateMachine stateMachine, EnemyView view, EnemyData data, EnemyPushLogic pushLogic, IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory, EnemyController controller)
        {
        }

        private void CreateNormalStates(EnemyStateMachine stateMachine, EnemyView view, EnemyData data, EnemyPushLogic pushLogic, IEventBus eventBus, int floorIndex, ProjectileFactory projectileFactory, EnemyController controller)
        {
            stateMachine.AddState(new EnemyPushState(view, data, pushLogic, controller));
        }
    }
}
