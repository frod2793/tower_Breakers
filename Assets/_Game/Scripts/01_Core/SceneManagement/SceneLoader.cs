using UnityEngine;
using UnityEngine.SceneManagement;
using EasyTransition;
using VContainer;
using VContainer.Unity;
using TowerBreakers.Player.Data;

namespace TowerBreakers.Core.SceneManagement
{
    /// <summary>
    /// [설명]: 씬 로딩 로직을 구현하며, VContainer를 통해 데이터를 다음 씬으로 전달합니다.
    /// </summary>
    public class SceneLoader : ISceneLoader
    {
        #region 내부 변수
        private readonly IObjectResolver m_resolver;
        private bool m_isTransitioning;
        #endregion

        #region 초기화
        public SceneLoader(IObjectResolver resolver)
        {
            m_resolver = resolver;
            m_isTransitioning = false;
            
            // [설명]: 전역 싱글톤으로 유지되므로, 새로운 씬이 로드되면 플래그를 초기화
            SceneManager.sceneLoaded += (scene, mode) => m_isTransitioning = false;
        }
        #endregion

        #region 공개 API
        public void LoadScene(string sceneName, SceneContextDTO context, TransitionSettings settings = null)
        {
            if (m_isTransitioning)
            {
                Debug.LogWarning($"[SceneLoader] 씬 전환 진행 중: {sceneName}");
                return;
            }

            Debug.Log($"[SceneLoader] 씬 전환 시작: {sceneName}");
            m_isTransitioning = true;

            // [설명]: 데이터 무결성 보장: 전달된 context가 null이거나 Equipment가 null인 경우
            // UserSessionModel에서 현재 데이터를 가져와서 사용
            EquipmentDTO equipmentToTransfer = null;

            if (context?.Equipment != null)
            {
                // 외부에서 전달된 DTO 우선 사용
                equipmentToTransfer = context.Equipment;
                Debug.Log($"[SceneLoader] 외부 DTO 사용: 무기 {equipmentToTransfer.OwnedWeaponIds.Count}개, 갑주 {equipmentToTransfer.OwnedArmorIds.Count}개");
            }
            else
            {
                // [설명]: context가 null이거나 Equipment가 null인 경우 UserSessionModel에서 데이터 가져오기 시도
                if (m_resolver.TryResolve<UserSessionModel>(out var sessionModel))
                {
                    equipmentToTransfer = sessionModel.ExportDTO();
                    Debug.Log($"[SceneLoader] UserSessionModel에서 데이터 추출: 무기 {equipmentToTransfer.OwnedWeaponIds.Count}개, 갑주 {equipmentToTransfer.OwnedArmorIds.Count}개");
                }
                else
                {
                    Debug.LogWarning("[SceneLoader] UserSessionModel을 찾을 수 없음, 빈 DTO 사용");
                    equipmentToTransfer = new EquipmentDTO();
                }
            }

            // [설명]: 전역 SceneContextDTO 인스턴스를 찾아 데이터를 업데이트합니다.
            if (m_resolver.TryResolve<SceneContextDTO>(out var globalContext))
            {
                globalContext.Equipment = equipmentToTransfer;

                // ExtraData 동기화 (전달된 context가 있는 경우)
                if (context != null)
                {
                    foreach (var kvp in context.ExtraData)
                    {
                        globalContext.ExtraData[kvp.Key] = kvp.Value;
                    }
                }
                
                Debug.Log($"[SceneLoader] SceneContextDTO 업데이트 완료: 무기 {equipmentToTransfer.OwnedWeaponIds.Count}개, 갑주 {equipmentToTransfer.OwnedArmorIds.Count}개");
            }
            else
            {
                Debug.LogWarning("[SceneLoader] 전역 SceneContextDTO를 찾을 수 없음");
            }

            // [설명]: TransitionManager.Instance()는 없으면 내부적으로 LogError를 발생시키므로 직접 검색
            var transitionManager = Object.FindFirstObjectByType<TransitionManager>();
            if (settings != null && transitionManager != null)
            {
                transitionManager.Transition(sceneName, settings, 0f);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        #endregion
    }
}
