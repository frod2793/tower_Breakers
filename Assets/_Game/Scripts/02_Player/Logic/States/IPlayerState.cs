using Cysharp.Threading.Tasks;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 개별 상태를 정의하는 인터페이스입니다.
    /// </summary>
    public interface IPlayerState
    {
        void OnEnter();
        void OnExit();
        void OnTick();
    }
}
