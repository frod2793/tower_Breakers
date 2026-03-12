using Cysharp.Threading.Tasks;

namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임 상태 인터페이스입니다.
    /// </summary>
    public interface IGameState
    {
        UniTask OnEnter();
        UniTask OnExit();
        void OnUpdate();
    }
}
