using UnityEngine;
using UnityEditor;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Enemy.Data;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Editor
{
    /// <summary>
    /// [설명]: 크라켄 보스 디버그 에디터 스크립트입니다.
    /// 보스 페이즈, 스킬, HP를 에디터에서 테스트할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(EnemyController))]
    public class KrakenBossDebuggerEditor : UnityEditor.Editor
    {
        private EnemyController m_target;
        private bool m_showDebugInfo = true;
        private bool m_showPhaseControl = true;
        private bool m_showSkillControl = true;
        private bool m_showHpControl = true;

        private void OnEnable()
        {
            m_target = (EnemyController)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying == false)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 디버깅이 가능합니다.", MessageType.Info);
                return;
            }

            DrawBossDebugSection();
        }

        private void DrawBossDebugSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("═══════════════════════════════════════════", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("보스 디버그 테스트", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("═══════════════════════════════════════════", EditorStyles.boldLabel);

            var data = m_target.Data;
            if (data == null || !data.EnemyName.Contains("Kraken"))
            {
                EditorGUILayout.HelpBox("크라켄 보스가 아닙니다.", MessageType.Warning);
                return;
            }

            m_showDebugInfo = EditorGUILayout.Foldout(m_showDebugInfo, "디버그 정보 표시");
            if (m_showDebugInfo)
            {
                DrawDebugInfo();
            }

            EditorGUILayout.Space(5);
            DrawPhaseControl();
            EditorGUILayout.Space(5);
            DrawSkillControl();
            EditorGUILayout.Space(5);
            DrawHpControl();
        }

        private void DrawDebugInfo()
        {
            EditorGUI.indentLevel++;
            
            var data = m_target.Data;
            EditorGUILayout.LabelField($"보스 이름: {data?.EnemyName ?? "Unknown"}");
            EditorGUILayout.LabelField($"현재 HP: {m_target.CurrentHp}");
            EditorGUILayout.LabelField($"최대 HP: {data?.Hp}");
            
            // [구조 명확화]: BossFSM에서 정보 가져오기
            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            if (bossFSM != null)
            {
                EditorGUILayout.LabelField($"현재 페이즈: {bossFSM.CurrentPhaseIndex + 1}");
                EditorGUILayout.LabelField($"총 페이즈 수: {bossFSM.TotalPhases}");
                EditorGUILayout.LabelField($"실행 중: {(bossFSM.IsExecuting ? "예" : "아니오")}");
                EditorGUILayout.LabelField($"상태 (FSM): {bossFSM.CurrentStateName}");
            }
            
            var stateMachine = m_target.StateMachine;
            if (stateMachine != null)
            {
                EditorGUILayout.LabelField($"상태: {stateMachine.CurrentState?.GetType().Name}");
            }

            EditorGUI.indentLevel--;
        }

        private void DrawPhaseControl()
        {
            m_showPhaseControl = EditorGUILayout.Foldout(m_showPhaseControl, "─────────────────────────────────────────");
            if (!m_showPhaseControl) return;
            
            EditorGUILayout.LabelField("페이즈 전환 테스트", EditorStyles.boldLabel);

            // [구조 명확화]: BossFSM에서 정보 가져오기
            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            var data = m_target.Data;
            if (data == null || bossFSM == null) return;

            int maxHp = data.Hp;
            int currentHp = m_target.CurrentHp;

            float hpPercent = (float)currentHp / maxHp * 100f;
            int currentPhase = bossFSM.CurrentPhaseIndex + 1;
            int totalPhases = bossFSM.TotalPhases;

            EditorGUILayout.LabelField($"HP: {currentHp}/{maxHp} ({hpPercent:F1}%)");
            EditorGUILayout.LabelField($"현재 페이즈: {currentPhase}/{totalPhases}");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("HP 기반 전환:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Phase 1 (HP 100%)"))
            {
                int damage = m_target.CurrentHp - maxHp;
                if (damage > 0) m_target.TakeDamage(damage);
            }
            
            if (GUILayout.Button("Phase 2 전환 (HP 50%)"))
            {
                int targetHp = maxHp / 2;
                int damage = m_target.CurrentHp - targetHp;
                if (damage > 0) m_target.TakeDamage(damage);
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("강제 전환:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Phase 1 강제"))
            {
                bossFSM.ForceChangePhase(0);
            }
            
            if (m_target.CurrentHp <= maxHp * 0.5f)
            {
                if (GUILayout.Button("Phase 2 강제"))
                {
                    bossFSM.ForceChangePhase(1);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSkillControl()
        {
            m_showSkillControl = EditorGUILayout.Foldout(m_showSkillControl, "─────────────────────────────────────────");
            if (!m_showSkillControl) return;

            EditorGUILayout.LabelField("스킬 실행 테스트", EditorStyles.boldLabel);

            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            if (bossFSM == null)
            {
                EditorGUILayout.LabelField("페이즈 정보를 찾을 수 없습니다.");
                return;
            }

            var data = m_target.Data;
            int maxHp = data.Hp;
            int currentHp = m_target.CurrentHp;
            bool isPhase2 = currentHp <= maxHp * 0.5f;
            int currentPhaseIndex = bossFSM.CurrentPhaseIndex;

            EditorGUILayout.LabelField($"현재 페이즈: {(isPhase2 ? "Phase 2" : "Phase 1")}");

            var phases = bossFSM.Phases;
            if (phases == null || phases.Count == 0)
            {
                EditorGUILayout.LabelField("패턴이 없습니다.");
                return;
            }

            var patterns = phases[currentPhaseIndex].Patterns;
            if (patterns == null || patterns.Count == 0)
            {
                EditorGUILayout.LabelField("패턴이 없습니다.");
                return;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Phase {currentPhaseIndex + 1} 스킬 목록:", EditorStyles.boldLabel);

            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                string patternName = pattern?.PatternName ?? $"스킬 {i}";
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  [{i}] {patternName}", EditorStyles.label);
                if (GUILayout.Button("실행", GUILayout.Width(60)))
                {
                    ExecutePattern(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("빠른 실행:", EditorStyles.boldLabel);

            DrawKrakenQuickButtons();
        }

        private void DrawKrakenQuickButtons()
        {
            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            if (bossFSM == null) return;

            var phases = bossFSM.Phases;
            if (phases == null || phases.Count < 2) return;

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Phase 1 스킬:");
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("  FallingTentacle (촉수 낙하)");
            if (GUILayout.Button("실행", GUILayout.Width(50)))
            {
                ExecutePatternByName("Falling");
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("  ArtilleryFire (포격)");
            if (GUILayout.Button("실행", GUILayout.Width(50)))
            {
                ExecutePatternByName("Artillery");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Phase 2 스킬:");
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("  StrikeTentacle (촉수 강타)");
            if (GUILayout.Button("실행", GUILayout.Width(50)))
            {
                ExecutePatternByName("Strike");
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("  SummonTentacle (촉수 소환)");
            if (GUILayout.Button("실행", GUILayout.Width(50)))
            {
                ExecutePatternByName("Summon Tentacle");
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("  SummonSeaMonster (해저몬스터)");
            if (GUILayout.Button("실행", GUILayout.Width(50)))
            {
                ExecutePatternByName("Summon Sea Monster");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ExecutePattern(int patternIndex)
        {
            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            if (bossFSM == null) return;

            bossFSM.ExecutePatternDebug(patternIndex);
            Repaint();
        }

        private void ExecutePatternByName(string patternName)
        {
            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            if (bossFSM == null) return;

            var phases = bossFSM.Phases;
            int currentPhase = bossFSM.CurrentPhaseIndex;
            
            var patterns = phases[currentPhase].Patterns;
            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i].PatternName.Contains(patternName))
                {
                    ExecutePattern(i);
                    return;
                }
            }
            
            Debug.LogWarning($"[KrakenDebugger] 패턴을 찾을 수 없습니다: {patternName}");
        }

        private void DrawHpControl()
        {
            m_showHpControl = EditorGUILayout.Foldout(m_showHpControl, "─────────────────────────────────────────");
            if (!m_showHpControl) return;

            EditorGUILayout.LabelField("HP 직접 제어", EditorStyles.boldLabel);

            var data = m_target.Data;
            if (data == null) return;

            EditorGUILayout.LabelField($"HP 범위: 1 ~ {data.Hp}");

            int newHp = EditorGUILayout.IntSlider("HP 값", m_target.CurrentHp, 1, data.Hp);
            
            if (newHp != m_target.CurrentHp)
            {
                int damage = m_target.CurrentHp - newHp;
                if (damage > 0)
                {
                    m_target.TakeDamage(damage);
                }
            }
        }
    }
}