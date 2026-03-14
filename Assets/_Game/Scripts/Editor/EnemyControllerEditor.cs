using UnityEngine;
using UnityEditor;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Enemy.Boss.AI.FSM;
using TowerBreakers.Enemy.Boss.AI.BT;
using System.Linq;
using System.Collections.Generic;

namespace TowerBreakers.Editor
{
    /// <summary>
    /// [설명]: EnemyController 컴포넌트를 위한 커스텀 에디터입니다.
    /// FSM + BT 기반 보스 패턴 정보를 Inspector에서 확인할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(EnemyController))]
    public class EnemyControllerEditor : UnityEditor.Editor
    {
        #region 내부 필드
        private EnemyController m_target;
        private bool m_showBossInfo = true;
        #endregion

        #region 유니티 생명주기
        private void OnEnable()
        {
            m_target = (EnemyController)target;
        }

        public override void OnInspectorGUI()
        {
            if (m_target == null) return;

            base.OnInspectorGUI();

            EditorGUILayout.Space(10);
            DrawBossPatternInfo();
        }
        #endregion

        #region Boss 패턴 정보 그리기
        private void DrawBossPatternInfo()
        {
            if (m_target.Data == null || m_target.Data.Type != TowerBreakers.Enemy.Data.EnemyType.Boss)
            {
                return;
            }

            var bossFSM = m_target.BossPhaseState as TowerBreakers.Enemy.Boss.AI.FSM.BossFSM;
            if (bossFSM == null)
            {
                EditorGUILayout.HelpBox("보스 FSM을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            m_showBossInfo = EditorGUILayout.Foldout(m_showBossInfo, "🔮 전역 보스 스킬/패턴 디버거", true, EditorStyles.foldoutHeader);
            if (m_showBossInfo)
            {
                EditorGUI.indentLevel++;
                
                // 1. 상태 정보
                DrawBossStatusHeader(bossFSM);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("플레이 모드에서 패턴 실행 및 전환 기능을 사용할 수 있습니다.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.Space(5);
                    
                    // 2. 페이즈 제어
                    DrawPhaseControl(bossFSM);

                    EditorGUILayout.Space(10);

                    // 3. 패턴(스킬) 실행 버튼
                    DrawPatternExecutionButtons(bossFSM);
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawBossStatusHeader(TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("📊 실시간 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"현재 상태: {fsm.CurrentStateName}");
            EditorGUILayout.LabelField($"현재 페이즈: Phase {fsm.CurrentPhaseIndex + 1}");
            EditorGUILayout.LabelField($"동작 실행 중: {(fsm.IsExecuting ? "YES (Busy)" : "NO (Idle)")}");
            
            // 크라켄 전용 상태 정보 표시 (있을 경우)
            DrawKrakenSpecificStatus(fsm);

            float hpPercent = m_target.MaxHp > 0 ? (float)m_target.CurrentHp / m_target.MaxHp : 0;
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, hpPercent, $"HP {m_target.CurrentHp} / {m_target.MaxHp} ({hpPercent * 100:F1}%)");
            EditorGUILayout.EndVertical();
        }

        private void DrawKrakenSpecificStatus(TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm)
        {
            // 패턴 중 하나에서 KrakenBossState를 추출하려 시도 (구조상 가장 쉬운 방법)
            if (fsm.Phases == null || fsm.Phases.Count == 0) return;
            
            // KrakenPhase2 등에서 필드를 reflection으로 가져오거나, 
            // KrakenBossState를 BossFSM에 직접 노출하는 것이 정석이나 
            // 여기서는 UI용 탐색
            foreach (var phase in fsm.Phases)
            {
                foreach (var pattern in phase.Patterns)
                {
                    if (pattern is KrakenSummonTentaclePattern summonPattern)
                    {
                        // 리플렉션으로 m_krakenState 접근 (디버거용)
                        var field = typeof(KrakenSummonTentaclePattern).GetField("m_krakenState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var state = field?.GetValue(summonPattern) as KrakenBossState;
                        
                        if (state != null)
                        {
                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                            EditorGUILayout.LabelField("🐙 크라켄 실시간 소환물 상태", EditorStyles.miniBoldLabel);
                            EditorGUILayout.LabelField($"총 촉수 개수: {state.TotalTentacleCount} / 5");
                            
                            EditorGUILayout.BeginHorizontal();
                            for (int i = 0; i < 3; i++)
                            {
                                EditorGUILayout.LabelField($"[{i}층: {state.GetTentacleCount(i)}]", GUILayout.Width(60));
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            return;
                        }
                    }
                }
            }
        }

        private void DrawPhaseControl(TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm)
        {
            EditorGUILayout.LabelField("🔄 페이즈 제어", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < fsm.TotalPhases; i++)
            {
                GUI.enabled = (fsm.CurrentPhaseIndex != i);
                if (GUILayout.Button($"Phase {i + 1} 강제 전환", GUILayout.Height(25)))
                {
                    fsm.ForceChangePhase(i);
                }
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPatternExecutionButtons(TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm)
        {
            EditorGUILayout.LabelField("⚔️ 스킬/패턴 실행 (현재 페이즈 기준)", EditorStyles.boldLabel);

            if (fsm.Phases == null || fsm.Phases.Count <= fsm.CurrentPhaseIndex)
            {
                EditorGUILayout.HelpBox("현재 페이즈의 패턴 데이터를 가져올 수 없습니다.", MessageType.Error);
                return;
            }

            var currentPhase = fsm.Phases[fsm.CurrentPhaseIndex];
            var patterns = currentPhase.Patterns;

            if (patterns == null || patterns.Count == 0)
            {
                EditorGUILayout.HelpBox("사용 가능한 패턴이 없습니다.", MessageType.None);
                return;
            }

            GUI.enabled = !fsm.IsExecuting;
            
            // 2개씩 한 줄에 배치
            for (int i = 0; i < patterns.Count; i += 2)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 첫 번째 버튼
                DrawPatternButton(fsm, patterns[i]);

                // 두 번째 버튼 (있을 경우)
                if (i + 1 < patterns.Count)
                {
                    DrawPatternButton(fsm, patterns[i + 1]);
                }
                else
                {
                    GUILayout.Space(EditorGUIUtility.currentViewWidth * 0.45f); // 자리 맞춤용 스페이스
                }

                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = true;

            if (fsm.IsExecuting)
            {
                EditorGUILayout.HelpBox("동작이 실행 중일 때는 버튼이 비활성화됩니다.", MessageType.None);
            }
        }

        private void DrawPatternButton(TowerBreakers.Enemy.Boss.AI.FSM.BossFSM fsm, IBossPattern pattern)
        {
            if (pattern == null) return;
            
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 0.8f, 1f); // 스킬 버튼 색상 강조
            
            if (GUILayout.Button($"▶ {pattern.PatternName}", GUILayout.Height(30)))
            {
                fsm.TriggerDebugPattern(pattern);
                Debug.Log($"[Debugger] 버튼 클릭: {pattern.PatternName} 실행 예약");
            }
            
            GUI.backgroundColor = oldColor;
        }
        #endregion
    }

    /// <summary>
    /// [설명]: EnemyData 에셋을 위한 커스텀 에디터입니다.
    /// Boss 타입일 경우 사용할 수 있는 FSM + BT 패턴 템플릿을 보여줍니다.
    /// </summary>
    [CustomEditor(typeof(TowerBreakers.Enemy.Data.EnemyData))]
    public class EnemyDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var data = (TowerBreakers.Enemy.Data.EnemyData)target;

            if (data == null) return;

            EditorGUILayout.Space(10);

            if (data.Type == TowerBreakers.Enemy.Data.EnemyType.Boss && data.EnemyName != null && data.EnemyName.Contains("Goblin"))
            {
                DrawGoblinBossTemplate();
            }
            else if (data.Type == TowerBreakers.Enemy.Data.EnemyType.Boss && data.EnemyName != null && data.EnemyName.Contains("Kraken"))
            {
                DrawKrakenBossTemplate();
            }
        }

        private void DrawKrakenBossTemplate()
        {
            EditorGUILayout.LabelField("📋 크라켄 FSM + 패턴 템플릿", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox(
                "=== FSM 상태 ===\n" +
                "Idle → Attack → Idle (반복/상태가중치)\n" +
                "HP 50% ↓\n" +
                "PhaseChange → Phase2\n\n" +
                "=== Phase 1 (HP ≥ 50%) ===\n" +
                "• Tentacle - 가로 휘두르기 (Attack #1)\n" +
                "• Artillery - 포격 공격 (Attack #2)\n\n" +
                "=== Phase 2 (HP < 50%) ===\n" +
                "• Summon Tentacle - 촉수 소환 (Attack #3)\n" +
                "• Summon Sea Monster - 몬스터 소환 (Attack #4)\n" +
                "• Tentacle / Artillery 유지\n\n" +
                "=== 요약 제약 조건 ===\n" +
                "• 촉수 최대 5개 유지 (초과 시 소환 스킵)\n" +
                "• 해저 몬스터 최대 3세트 유지\n" +
                "• 촉수 파괴 시 보스 체력 전이 (레거시 구현 로직)\n\n" +
                "=== 애니메이션 매핑 ===\n" +
                "• #1 = Tentacle Attack\n" +
                "• #2 = Artillery\n" +
                "• #3 = Summon Tentacle\n" +
                "• #4 = Summon SeaMonster",
                MessageType.Info);

            EditorGUI.indentLevel--;
        }

        private void DrawGoblinBossTemplate()
        {
            EditorGUILayout.LabelField("📋 고블린족장 FSM + BT 패턴 템플릿", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox(
                "=== FSM 상태 ===\n" +
                "Idle → Attack → Idle (반복)\n" +
                "HP 50% ↓\n" +
                "PhaseChange → Phase2\n\n" +
                "=== Phase 1 (HP ≥ 50%) ===\n" +
                "• Swing - 대시 공격 (Attack #4)\n" +
                "• Totem - 무작위 (Bomb/Lightning/Buff)\n\n" +
                "=== Phase 2 (HP < 50%) ===\n" +
                "• Jump - 점프 공격 (Attack #5, #6)\n" +
                "• Swing\n" +
                "• Totem\n\n" +
                "=== BT 스킬 선택 ===\n" +
                "BTSelector:\n" +
                "├── Phase2 + 근거리 → Jump\n" +
                "├── Phase2 + 원거리 → Swing\n" +
                "├── Phase1 → Swing\n" +
                "└── Random → Totem\n\n" +
                "=== 애니메이션 매핑 ===\n" +
                "• #4 = Swing\n" +
                "• #5 = Jump Start\n" +
                "• #6 = Landing\n" +
                "• #7 = Totem Skill",
                MessageType.Info);

            EditorGUI.indentLevel--;
        }
    }
}
