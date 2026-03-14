using UnityEngine;
using TowerBreakers.Enemy.Logic;

namespace TowerBreakers.Enemy.Boss.AI.BT
{
    /// <summary>
    /// [설명]: BT 노드 실행 결과입니다.
    /// </summary>
    public enum BTNodeResult
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// [설명]: BT (Behavior Tree) 노드의 기본 클래스입니다.
    /// </summary>
    public abstract class BTNode
    {
        public virtual BTNodeResult Evaluate() => BTNodeResult.Failure;
    }

    /// <summary>
    /// [설명]:Composite 노드 - 여러 자식 노드를 순차적으로 실행합니다.
    /// </summary>
    public abstract class BTComposite : BTNode
    {
        public BTNode[] Children { get; set; }
        
        protected BTComposite(params BTNode[] children)
        {
            Children = children;
        }
    }

    /// <summary>
    /// [설명]: Selector 노드 - 자식 노드를 순차적으로 평가하여 첫 성공을 반환합니다.
    /// </summary>
    public class BTSelector : BTComposite
    {
        public BTSelector(params BTNode[] children) : base(children) { }

        public override BTNodeResult Evaluate()
        {
            foreach (var child in Children)
            {
                var result = child.Evaluate();
                if (result == BTNodeResult.Success)
                    return BTNodeResult.Success;
                if (result == BTNodeResult.Running)
                    return BTNodeResult.Running;
            }
            return BTNodeResult.Failure;
        }
    }

    /// <summary>
    /// [설명]: Sequence 노드 - 자식 노드를 순차적으로 실행하여 모두 성공하면 성공을 반환합니다.
    /// </summary>
    public class BTSequence : BTComposite
    {
        public BTSequence(params BTNode[] children) : base(children) { }

        public override BTNodeResult Evaluate()
        {
            foreach (var child in Children)
            {
                var result = child.Evaluate();
                if (result == BTNodeResult.Failure)
                    return BTNodeResult.Failure;
                if (result == BTNodeResult.Running)
                    return BTNodeResult.Running;
            }
            return BTNodeResult.Success;
        }
    }

    /// <summary>
    /// [설명]:Decorator 노드 - 단일 자식 노드의 결과를 변환합니다.
    /// </summary>
    public abstract class BTDecorator : BTNode
    {
        public BTNode Child { get; }
        
        protected BTDecorator(BTNode child)
        {
            Child = child;
        }
    }

    /// <summary>
    /// [설명]: Inverter 노드 - 자식 노드의 결과를 반전시킵니다.
    /// </summary>
    public class BTInverter : BTDecorator
    {
        public BTInverter(BTNode child) : base(child) { }

        public override BTNodeResult Evaluate()
        {
            var result = Child.Evaluate();
            return result == BTNodeResult.Success ? BTNodeResult.Failure : 
                   result == BTNodeResult.Failure ? BTNodeResult.Success : BTNodeResult.Running;
        }
    }

    /// <summary>
    /// [설명]: Leaf 노드 - 실제 동작을 수행하는 노드입니다.
    /// </summary>
    public abstract class BTLeaf : BTNode
    {
    }

    /// <summary>
    /// [설명]: Condition 노드 - 조건을 평가합니다.
    /// </summary>
    public class BTCondition : BTLeaf
    {
        private readonly System.Func<bool> m_condition;

        public BTCondition(System.Func<bool> condition)
        {
            m_condition = condition;
        }

        public override BTNodeResult Evaluate()
        {
            return m_condition() ? BTNodeResult.Success : BTNodeResult.Failure;
        }
    }

    /// <summary>
    /// [설명]: Action 노드 - 패턴 실행을 위한 노드입니다.
    /// </summary>
    public class BTAction : BTLeaf
    {
        private readonly IBossPattern m_pattern;
        private IBossPattern m_selectedPattern;

        public BTAction(IBossPattern pattern)
        {
            m_pattern = pattern;
        }

        public override BTNodeResult Evaluate()
        {
            m_selectedPattern = m_pattern;
            return m_pattern != null ? BTNodeResult.Success : BTNodeResult.Failure;
        }

        public IBossPattern GetSelectedPattern() => m_selectedPattern;
    }

    /// <summary>
    /// [설명]: 동적 인덱스 기반 패턴 조회 노드입니다. 페이즈 전환 시에도 올바른 패턴을 참조합니다.
    /// </summary>
    public class BTDynamicAction : BTLeaf
    {
        private readonly BossSkillContext m_context;
        private readonly int m_patternIndex;
        private IBossPattern m_selectedPattern;

        public BTDynamicAction(BossSkillContext context, int patternIndex)
        {
            m_context = context;
            m_patternIndex = patternIndex;
        }

        public override BTNodeResult Evaluate()
        {
            m_selectedPattern = m_context.GetPatternByIndex(m_patternIndex);
            return m_selectedPattern != null ? BTNodeResult.Success : BTNodeResult.Failure;
        }

        public IBossPattern GetSelectedPattern() => m_selectedPattern;
    }

    /// <summary>
    /// [설명]: 무작위로 패턴을 선택하는 Selector 노드입니다.
    /// </summary>
    public class BTRandomSelector : BTComposite
    {
        public BTRandomSelector(BossSkillContext context)
        {
            var patterns = context.Phases[context.CurrentPhaseIndex].Patterns;
            if (patterns == null || patterns.Count == 0)
            {
                Children = new BTNode[0];
                return;
            }

            var actions = new BTNode[patterns.Count];
            for (int i = 0; i < patterns.Count; i++)
            {
                actions[i] = new BTAction(patterns[i]);
            }
            Children = actions;
        }

        public BTRandomSelector(params BTNode[] children) : base(children) { }

        public override BTNodeResult Evaluate()
        {
            if (Children.Length == 0) return BTNodeResult.Failure;

            int randomIndex = Random.Range(0, Children.Length);
            return Children[randomIndex].Evaluate();
        }
    }
}
