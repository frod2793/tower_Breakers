using UnityEngine;
using VContainer;
using TowerBreakers.Core.SceneManagement;
using TowerBreakers.Player.Data;
using TowerBreakers.UI.Screens;

namespace TowerBreakers.Core.DI
{
    /// <summary>
    /// [설명]: 아웃게임 씬의 초기 진입점입니다.
    /// </summary>
    public class OutGameSceneInitializer : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField] private OutGameView m_outGameView;
        #endregion

        #region 초기화 로직
        [Inject]
        public void Initialize(UserSessionModel sessionModel, OutGameViewModel outGameVM, IObjectResolver resolver)
        {
            if (resolver.TryResolve<SceneContextDTO>(out var context))
            {
                if (context != null && context.Equipment != null)
                {
                    Debug.Log("[OutGameSceneInitializer] 주입된 DTO로부터 세션 동기화");
                    sessionModel.UpdateEquipment(context.Equipment);
                }
            }

            if (m_outGameView != null)
            {
                m_outGameView.Initialize(outGameVM);
            }
        }
        #endregion
    }
}
