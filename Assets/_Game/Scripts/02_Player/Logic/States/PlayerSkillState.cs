using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Player.View;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Core.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TowerBreakers.Core;
using TowerBreakers.Core.Interfaces;
using TowerBreakers.Player.Logic.Skills;
using TowerBreakers.Effects;
using TowerBreakers.Tower.Logic;
using VContainer;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 스킬 액션을 처리하는 공용 상태입니다.
    /// 전략 패턴(Strategy Pattern)을 사용하여 각 스킬 로직을 개별 Executor 클래스로 분리했습니다.
    /// </summary>
    public class PlayerSkillState : IPlayerState
    {
        #region 내부 필드
        private readonly PlayerView m_view;
        private readonly PlayerStateMachine m_stateMachine;
        private readonly PlayerModel m_model;
        private readonly PlayerData m_data;
        private readonly IEventBus m_eventBus;
        private readonly TowerManager m_towerManager;
        private readonly IEnumerable<ISkillExecutor> m_executorList;

        private int m_skillIndex;
        private PlayerPushReceiver m_pushReceiver;
        private PlayerProjectileFactory m_projectileFactory;
        private EffectManager m_effectManager;
        private readonly CooldownSystem m_cooldownSystem = new CooldownSystem();

        // 전략 패턴: 스킬 실행기 매핑
        private readonly Dictionary<int, ISkillExecutor> m_executors = new Dictionary<int, ISkillExecutor>();
        #endregion

        public PlayerSkillState(
            PlayerView view, 
            PlayerModel model, 
            PlayerStateMachine stateMachine, 
            PlayerData data, 
            IEventBus eventBus, 
            TowerManager towerManager,
            IEnumerable<ISkillExecutor> executorList)
        {
            m_view = view;
            m_model = model;
            m_stateMachine = stateMachine;
            m_data = data;
            m_eventBus = eventBus;
            m_towerManager = towerManager;
            m_executorList = executorList;
        }

        #region 초기화
        /// <summary>
        /// [설명]: 필요한 의존성을 주입하고 실행기들을 초기화합니다.
        /// </summary>
        public void Initialize(PlayerProjectileFactory factory, EffectManager effectManager)
        {
            m_projectileFactory = factory;
            m_effectManager = effectManager;

            // DI로 주입받은 Executor들을 딕셔너리에 매핑
            int index = 0;
            foreach (var executor in m_executorList)
            {
                m_executors[index++] = executor;
            }

            // 각 실행기 초기화
            foreach (var executor in m_executors.Values)
            {
                executor.Initialize(m_view, m_model, m_data, m_eventBus, m_projectileFactory, m_effectManager, m_cooldownSystem, m_towerManager);
            }
        }
        #endregion

        #region 공개 API
        public void SetSkill(int index) => m_skillIndex = index;

        /// <summary>
        /// [설명]: 해당 스킬이 현재 쿨다운 중인지 확인합니다.
        /// </summary>
        public bool IsSkillOnCooldown(int index)
        {
            if (m_executors.TryGetValue(index, out var executor))
            {
                return executor.IsOnCooldown;
            }
            return false;
        }

        /// <summary>
        /// [설명]: 매프레임 쿨다운을 감소시킵니다.
        /// </summary>
        public void TickCooldowns()
        {
            m_cooldownSystem.Update(Time.deltaTime);
        }
        #endregion

        #region 인터페이스 구현
        public void OnEnter()
        {
            // 밀림/벽 압착 방지 처리
            if (m_pushReceiver == null && m_view != null)
            {
                m_view.TryGetComponent(out m_pushReceiver);
            }

            if (m_pushReceiver != null) m_pushReceiver.IsClampingEnabled = false;

            // 트윈 정리
            if (m_view != null) m_view.transform.DOKill();

            // 애니메이션 재생
            if (m_view != null)
            {
                m_view.PlayAnimation(global::PlayerState.OTHER, m_skillIndex);
            }

            // 쿨다운 체크 및 실행
            if (IsSkillOnCooldown(m_skillIndex))
            {
                ReturnToIdle().Forget();
                return;
            }

            // 스킬 실행 (전략 패턴 위임)
            ExecuteSkill(m_skillIndex).Forget();
        }

        public void OnExit()
        {
            if (m_pushReceiver != null)
            {
                m_pushReceiver.IsClampingEnabled = true;
            }
        }

        public void OnTick()
        {
            // 모든 실행기의 쿨다운 업데이트 (CooldownSystem 단일 호출)
            m_cooldownSystem.Update(Time.deltaTime);
        }
        #endregion

        #region 내부 로직
        private async UniTaskVoid ExecuteSkill(int index)
        {
            if (!m_executors.TryGetValue(index, out var executor))
            {
                await ReturnToIdle();
                return;
            }

            // 스킬 실행
            await executor.ExecuteAsync();

            // 실행 후 대기 상태로 복귀
            await ReturnToIdle();
        }

        private async UniTask ReturnToIdle()
        {
            if (m_view == null) return;
            
            // 애니메이션 시간에 맞춘 지연 (필요 시)
            await UniTask.Delay(100, cancellationToken: m_view.GetCancellationTokenOnDestroy());
            
            if (m_stateMachine != null)
            {
                m_stateMachine.ChangeState<PlayerIdleState>();
            }
        }
        #endregion
    }
}
