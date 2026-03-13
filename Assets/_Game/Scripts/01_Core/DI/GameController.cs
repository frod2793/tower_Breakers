using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Core.GameState;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.View;
using TowerBreakers.Combat;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Tower.Logic;
using Cysharp.Threading.Tasks;
using TowerBreakers.Combat.Logic;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: 게임의 초기 진입점 및 프레임별 업데이트를 관리하는 컨트롤러입니다.
    /// </summary>
    public class GameController : IStartable, ITickable, IDisposable
    {
        #region 내부 필드
        private readonly GameStateMachine m_stateMachine;
        private readonly LoadingState m_loadingState;
        private readonly PlayingState m_playingState;

        // Player 관련
        private readonly PlayerModel m_playerModel;
        private readonly InventoryModel m_inventoryModel;
        private readonly PlayerStateMachine m_playerStateMachine;
        private readonly PlayerIdleState m_playerIdleState;
        private readonly PlayerAttackState m_playerAttackState;
        private readonly PlayerLeapState m_playerLeapState;
        private readonly PlayerDefendState m_playerDefendState;
        private readonly PlayerSkillState m_playerSkillState;
        private PlayerView m_playerView;

        // 시스템 관련
        private readonly CombatSystem m_combatSystem;
        private readonly EnemySpawner m_enemySpawner;
        private readonly TowerManager m_towerManager;
        private readonly PlayerData m_playerData;
        #endregion

        #region 초기화
        [Inject]
        public GameController(
            GameStateMachine stateMachine,
            LoadingState loadingState,
            PlayingState playingState,
            PlayerModel playerModel,
            InventoryModel inventoryModel,
            PlayerStateMachine playerStateMachine,
            PlayerIdleState playerIdleState,
            PlayerAttackState playerAttackState,
            PlayerLeapState playerLeapState,
            PlayerDefendState playerDefendState,
            PlayerSkillState playerSkillState,
            CombatSystem combatSystem,
            EnemySpawner enemySpawner,
            TowerManager towerManager,
            PlayerData playerData,
            PlayerView playerView)
        {
            m_stateMachine = stateMachine;
            m_loadingState = loadingState;
            m_playingState = playingState;
            
            m_playerModel = playerModel;
            m_inventoryModel = inventoryModel;
            m_playerStateMachine = playerStateMachine;
            m_playerIdleState = playerIdleState;
            m_playerAttackState = playerAttackState;
            m_playerLeapState = playerLeapState;
            m_playerDefendState = playerDefendState;
            m_playerSkillState = playerSkillState;

            m_combatSystem = combatSystem;
            m_enemySpawner = enemySpawner;
            m_towerManager = towerManager;
            m_playerData = playerData;

            // PlayerView 주입 및 초기화
            m_playerView = playerView;
            if (m_playerView != null)
            {
                m_playerView.Initialize(m_playerStateMachine);
            }
        }

        public void Start()
        {
            InitializeAsync().Forget();
        }

        /// <summary>
        /// [설명]: 게임 시스템을 비동기로 초기화합니다.
        /// </summary>
        private async UniTaskVoid InitializeAsync()
        {
            Debug.Log("[GameController] 시작: 시스템 초기화");

            // 플레이어 모델 초기화 (스탯 및 기본 무기)
            if (m_playerModel != null && m_playerData != null)
            {
                m_playerModel.Initialize(m_playerData);
            }

            // 초기 무기 지급 (테스트용)
            if (m_playerData != null && m_playerData.DefaultWeapon != null)
            {
                m_inventoryModel.AddWeapon(m_playerData.DefaultWeapon);
            }

            // 상태 등록
            m_stateMachine.AddState(m_loadingState);
            m_stateMachine.AddState(m_playingState);

            // 플레이어 상태 등록
            m_playerStateMachine.AddState(m_playerIdleState);
            m_playerStateMachine.AddState(m_playerAttackState);
            m_playerStateMachine.AddState(m_playerLeapState);
            m_playerStateMachine.AddState(m_playerDefendState);
            m_playerStateMachine.AddState(m_playerSkillState);

            // 초기 상태 시작
            await m_stateMachine.ChangeState<LoadingState>();

            // 로딩 후 플레이 상태로 즉시 전환
            await m_stateMachine.ChangeState<PlayingState>();

            // 플레이어 초기 상태 설정
            m_playerStateMachine.ChangeState<PlayerIdleState>();
        }
        #endregion

        #region 공개 메서드
        /// <summary>
        /// [설명]: 씬에 배치된 PlayerView를 설정합니다. (VContainer 자동 주입이 어려울 경우 호출)
        /// </summary>
        public void SetPlayerView(PlayerView view)
        {
            m_playerView = view;
            if (m_playerView != null)
            {
                m_playerView.Initialize(m_playerStateMachine);
            }
        }
        #endregion

        #region 유니티 라이프사이클 (ITickable)
        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            m_stateMachine?.Tick();
            m_playerStateMachine?.Tick();
            m_playerModel?.Tick(deltaTime);
        }
        #endregion

        #region 해제
        public void Dispose()
        {
            m_stateMachine?.Dispose();
            m_combatSystem?.Dispose();
        }
        #endregion
    }
}
