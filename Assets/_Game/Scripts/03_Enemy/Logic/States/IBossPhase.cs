using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 보스의 특정 페이즈를 정의하는 인터페이스입니다.
    /// </summary>
    public interface IBossPhase
    {
        int PhaseIndex { get; }
        List<IBossPattern> Patterns { get; }
        bool ShouldChangePhase(int currentHp, int maxHp);
    }

    /// <summary>
    /// [설명]: 보스 페이즈의 기본 구현체입니다.
    /// </summary>
    public abstract class BossPhaseBase : IBossPhase
    {
        public abstract int PhaseIndex { get; }
        public List<IBossPattern> Patterns { get; } = new List<IBossPattern>();
        public abstract bool ShouldChangePhase(int currentHp, int maxHp);
    }
}
