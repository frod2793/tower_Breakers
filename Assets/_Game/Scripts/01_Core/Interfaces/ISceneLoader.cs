using EasyTransition;

namespace TowerBreakers.Core.SceneManagement
{
    /// <summary>
    /// [설명]: 씬 전환 및 데이터 전달을 담당하는 인터페이스입니다.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// [설명]: 데이터를 포함하여 특정 씬으로 이동합니다.
        /// </summary>
        void LoadScene(string sceneName, SceneContextDTO context, TransitionSettings settings = null);
    }
}
