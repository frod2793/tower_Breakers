using UnityEngine;
using TowerBreakers.Enemy.Data;
using TowerBreakers.Enemy.View;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 보스의 복합적인 패턴과 페이즈를 관리하는 상태입니다.
    /// </summary>
    public class EnemyBossPhaseState : IEnemyState
    {
        #region 내부 필드
        private readonly EnemyController m_controller;
        private readonly EnemyView m_view;
        private readonly EnemyData m_data;
        private readonly List<IBossPattern> m_patterns = new List<IBossPattern>();
        
        private int m_currentPatternIndex = 0;
        private bool m_isExecuting = false;
        #endregion

        public EnemyBossPhaseState(EnemyController controller, EnemyView view, EnemyData data)
        {
            m_controller = controller;
            m_view = view;
            m_data = data;
        }

        public void AddPattern(IBossPattern pattern)
        {
            m_patterns.Add(pattern);
        }

        public void OnEnter()
        {
            m_currentPatternIndex = 0;
            m_isExecuting = false;
            m_view.PlayAnimation(global::PlayerState.IDLE);
        }

        public void OnExit()
        {
            m_isExecuting = false;
        }

        public void OnTick()
        {
            if (m_isExecuting || m_patterns.Count == 0) return;

            ExecuteNextPattern().Forget();
        }

        private async UniTaskVoid ExecuteNextPattern()
        {
            m_isExecuting = true;

            var pattern = m_patterns[m_currentPatternIndex];
            Debug.Log($"[BossPhaseState] 패턴 시작: {pattern.PatternName}");

            await pattern.ExecuteAsync(m_controller, m_view.GetCancellationTokenOnDestroy());

            // 다음 패턴 준비 (대기 시간)
            await UniTask.Delay(2000, cancellationToken: m_view.GetCancellationTokenOnDestroy());

            m_currentPatternIndex = (m_currentPatternIndex + 1) % m_patterns.Count;
            m_isExecuting = false;
        }
    }
}
