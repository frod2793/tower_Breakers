using UnityEngine;
using DG.Tweening;

namespace TowerBreakers.Enemy.Boss
{
    /// <summary>
    /// [설명]: 고블린족장의 애니메이션 이벤트를 처리하는 컴포넌트입니다.
    /// 애니메이션 클립에 정의된 AnimationEvent의 수신 메서드를 제공합니다.
    /// </summary>
    public class GoblinChiefAnimationEvents : MonoBehaviour
    {
        #region 토템 소환 이벤트
        /// <summary>
        /// [설명]: SummonTotem 애니메이션 이벤트입니다.
        /// </summary>
        public void SummonTotem()
        {
            Debug.Log("[GoblinChiefAnimationEvents] SummonTotem 이벤트 수신");
        }
        #endregion

        #region 패턴 종료 이벤트
        /// <summary>
        /// [설명]: 패턴 종료 애니메이션 이벤트입니다.
        /// </summary>
        public void PatternActionOver()
        {
            Debug.Log("[GoblinChiefAnimationEvents] PatternActionOver 이벤트 수신");
        }
        #endregion

        #region 점프 이벤트
        /// <summary>
        /// [설명]: Jumping 애니메이션 이벤트입니다.
        /// </summary>
        public void Jumping()
        {
            Debug.Log("[GoblinChiefAnimationEvents] Jumping 이벤트 수신");
        }
        #endregion

        #region 카메라 쉐이크 이벤트
        /// <summary>
        /// [설명]: 카메라 쉐이크 애니메이션 이벤트입니다.
        /// </summary>
        public void CameraShake()
        {
            Debug.Log("[GoblinChiefAnimationEvents] CameraShake 이벤트 수신");
            if (Camera.main != null)
            {
                Camera.main.transform.DOShakePosition(0.5f, 0.5f, 10, 90, false, true);
            }
        }
        #endregion

        #region 사운드 재생 이벤트
        /// <summary>
        /// [설명]: 사운드 재생 애니메이션 이벤트입니다.
        /// </summary>
        public void PlaySound()
        {
            Debug.Log("[GoblinChiefAnimationEvents] PlaySound 이벤트 수신");
        }
        #endregion
    }
}
