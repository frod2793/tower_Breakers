using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.Enemy.Logic;

namespace TowerBreakers.Enemy.Boss.AI.BT
{
    /// <summary>
    /// [설명]: 보스 스킬 선택기입니다. FSM의 패턴 선택 로직을 BT로 변환합니다.
    /// </summary>
    public class BossSkillSelector
    {
        private readonly BossSkillContext m_context;
        private BTNode m_root;

        public BossSkillSelector(BossSkillContext context)
        {
            m_context = context;
            RebuildTree();
        }

        public void RebuildTree()
        {
            m_root = BuildSkillTree();
        }

        public IBossPattern Evaluate()
        {
            var result = m_root.Evaluate();
            if (result == BTNodeResult.Success)
            {
                var selectedPattern = FindSelectedAction(m_root);
                if (selectedPattern != null)
                {
                    return selectedPattern;
                }
                m_context.AdvancePatternIndex();
                return m_context.GetCurrentPattern();
            }
            return null;
        }

        private IBossPattern FindSelectedAction(BTNode node)
        {
            if (node is BTAction action)
            {
                return action.GetSelectedPattern();
            }

            if (node is BTComposite composite)
            {
                foreach (var child in composite.Children)
                {
                    var result = FindSelectedAction(child);
                    if (result != null) return result;
                }
            }

            if (node is BTDecorator decorator)
            {
                return FindSelectedAction(decorator.Child);
            }

            return null;
        }

        private BTNode BuildSkillTree()
        {
            string bossName = m_context.Controller.Data.EnemyName;

            if (bossName.Contains("Kraken"))
            {
                return BuildKrakenTree();
            }
            else if (bossName.Contains("Goblin"))
            {
                return BuildGoblinChiefTree();
            }
            else
            {
                return BuildDefaultTree();
            }
        }

        private BTNode BuildKrakenTree()
        {
            return new BTSelector(
                new BTSequence(
                    new BTCondition(() => m_context.CurrentPhaseIndex == 1),
                    new BTCondition(() => m_context.ShouldSummonTentacle()),
                    new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Summon Tentacle"))
                ),
                new BTSequence(
                    new BTCondition(() => m_context.CurrentPhaseIndex == 0),
                    new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Falling"))
                ),
                new BTSequence(
                    new BTCondition(() => m_context.CurrentPhaseIndex == 1),
                    new BTCondition(() => m_context.IsPlayerInRange(3f)),
                    new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Strike"))
                ),
                new BTSequence(
                    new BTCondition(() => !m_context.IsPlayerInRange(5f)),
                    new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Artillery"))
                ),
                new BTRandomSelector(m_context)
            );
        }

        private BTNode BuildGoblinChiefTree()
        {
            var patterns = m_context.Phases[m_context.CurrentPhaseIndex].Patterns;
            if (patterns == null || patterns.Count == 0)
            {
                return new BTAction(null);
            }

            return new BTSelector(
                new BTSequence(
                    new BTCondition(() => m_context.IsPlayerInRange(2.5f)),
                    new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Swing"))
                ),
                new BTSequence(
                    new BTCondition(() => m_context.CurrentPhaseIndex == 1),
                    new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Jump"))
                ),
                new BTDynamicAction(m_context, m_context.GetPatternIndexByName("Totem"))
            );
        }

        private BTNode BuildDefaultTree()
        {
            var patterns = m_context.Phases[m_context.CurrentPhaseIndex].Patterns;
            if (patterns == null || patterns.Count == 0)
            {
                return new BTAction(null);
            }

            return new BTRandomSelector(m_context);
        }
    }

    /// <summary>
    /// [설명]: BT 실행을 위한 컨텍스트입니다.
    /// </summary>
    public class BossSkillContext
    {
        private readonly EnemyController m_controller;
        private readonly List<IBossPhase> m_phases;
        private int m_currentPhaseIndex;
        private int m_currentPatternIndex;
        private Dictionary<string, int> m_patternNameToIndex;

        public int CurrentPhaseIndex => m_currentPhaseIndex;
        public int TotalPhases => m_phases?.Count ?? 0;
        public List<IBossPhase> Phases => m_phases;
        public EnemyController Controller => m_controller;

        public BossSkillContext(EnemyController controller, List<IBossPhase> phases)
        {
            m_controller = controller;
            m_phases = phases;
            m_currentPhaseIndex = 0;
            m_currentPatternIndex = 0;
            BuildPatternLookup();
        }

        private void BuildPatternLookup()
        {
            m_patternNameToIndex = new Dictionary<string, int>();
            for (int phaseIdx = 0; phaseIdx < m_phases.Count; phaseIdx++)
            {
                var patterns = m_phases[phaseIdx].Patterns;
                if (patterns == null) continue;

                for (int patternIdx = 0; patternIdx < patterns.Count; patternIdx++)
                {
                    var patternName = patterns[patternIdx].PatternName;
                    if (!string.IsNullOrEmpty(patternName))
                    {
                        m_patternNameToIndex[patternName] = patternIdx;
                    }
                }
            }
        }

        public void RebuildLookup()
        {
            BuildPatternLookup();
        }

        public void AdvancePatternIndex()
        {
            if (m_phases.Count == 0) return;
            var patterns = m_phases[m_currentPhaseIndex].Patterns;
            if (patterns != null && patterns.Count > 0)
            {
                m_currentPatternIndex = (m_currentPatternIndex + 1) % patterns.Count;
            }
        }

        public IBossPattern GetCurrentPattern()
        {
            if (m_phases.Count == 0) return null;
            var patterns = m_phases[m_currentPhaseIndex].Patterns;
            if (patterns == null || patterns.Count == 0) return null;
            return patterns[m_currentPatternIndex];
        }

        public IBossPattern GetPatternByIndex(int index)
        {
            if (m_phases.Count == 0) return null;
            var patterns = m_phases[m_currentPhaseIndex].Patterns;
            if (patterns == null || patterns.Count == 0) return null;

            int safeIndex = Mathf.Clamp(index, 0, patterns.Count - 1);
            return patterns[safeIndex];
        }

        public int GetPatternIndexByName(string patternName)
        {
            if (string.IsNullOrEmpty(patternName)) return 0;

            if (m_patternNameToIndex.TryGetValue(patternName, out int index))
            {
                return index;
            }

            var patterns = m_phases[m_currentPhaseIndex].Patterns;
            for (int i = 0; i < patterns.Count; i++)
            {
                if (patterns[i].PatternName.Contains(patternName))
                {
                    return i;
                }
            }
            return 0;
        }

        public IBossPattern GetPatternByName(string patternName)
        {
            return GetPatternByIndex(GetPatternIndexByName(patternName));
        }

        public bool ShouldSummonTentacle()
        {
            return m_currentPhaseIndex == 1;
        }

        public bool IsPlayerInRange(float range)
        {
            var pushLogic = m_controller.CachedPushLogic;
            if (pushLogic == null) return false;

            var player = pushLogic.PlayerReceiver;
            if (player == null) return false;

            float distance = Vector3.Distance(m_controller.transform.position, player.transform.position);
            return distance <= range;
        }

        public bool ShouldChangePhase()
        {
            if (m_currentPhaseIndex >= m_phases.Count) return false;
            return m_phases[m_currentPhaseIndex].ShouldChangePhase(m_controller.CurrentHp, m_controller.MaxHp);
        }

        public bool TryChangePhase()
        {
            if (m_currentPhaseIndex >= m_phases.Count) return false;

            if (m_phases[m_currentPhaseIndex].ShouldChangePhase(m_controller.CurrentHp, m_controller.MaxHp))
            {
                if (m_currentPhaseIndex + 1 < m_phases.Count)
                {
                    m_currentPhaseIndex++;
                    m_currentPatternIndex = 0;
                    RebuildLookup();
                    return true;
                }
            }
            return false;
        }

        public void ForceChangePhase(int phaseIndex)
        {
            if (phaseIndex >= 0 && phaseIndex < m_phases.Count)
            {
                m_currentPhaseIndex = phaseIndex;
                m_currentPatternIndex = 0;
                Debug.Log($"[BossSkillContext] 강제 페이즈 전환: {phaseIndex + 1}");
            }
        }

        public void ExecutePatternImmediate(int patternIndex)
        {
            Debug.Log($"[BossSkillContext] 디버그 패턴 실행: 인덱스 {patternIndex}");
        }
    }
}