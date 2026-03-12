using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TowerBreakers.Core.GameState
{
    /// <summary>
    /// [설명]: 게임 초기 로딩 상태입니다.
    /// </summary>
    public class LoadingState : IGameState
    {
        public async UniTask OnEnter()
        {
            Debug.Log("[LoadingState] 진입: 에셋 및 데이터 로딩 시작");
            // TODO: 실제 로딩 로직 구현 (Addressables 등)
            await UniTask.Delay(500); // 임시 딜레이
        }

        public UniTask OnExit()
        {
            Debug.Log("[LoadingState] 퇴출");
            return UniTask.CompletedTask;
        }

        public void OnUpdate() { }
    }
}
