using Cysharp.Threading.Tasks;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 적의 개별 상태 인터페이스입니다.
    /// </summary>
    public interface IEnemyState
    {
        void OnEnter();
        void OnExit();
        void OnTick();
    }
}
