using UnityEngine;

namespace TowerBreakers.Core.Service
{
    /// <summary>
    /// [설명]: 카메라 쉐이크 및 스프라이트 이펙트 재생을 담당하는 서비스 인터페이스입니다.
    /// </summary>
    public interface IEffectService
    {
        /// <summary>
        /// [설명]: 카메라를 지정된 강도와 시간 동안 흔듭니다.
        /// </summary>
        /// <param name="duration">지속 시간</param>
        /// <param name="strength">흔들림 강도</param>
        void PlayCameraShake(float duration, float strength);

        /// <summary>
        /// [설명]: 지정된 위치와 회전값으로 스프라이트 이펙트를 재생합니다.
        /// </summary>
        /// <param name="effectId">이펙트 식별자 (DTO 등록 기준)</param>
        /// <param name="position">월드 좌표</param>
        /// <param name="rotation">회전값</param>
        void PlaySpriteEffect(string effectId, Vector3 position, Quaternion rotation);

        /// <summary>
        /// [설명]: 기본 설정값으로 카메라 쉐이크를 재생합니다.
        /// </summary>
        void PlayDefaultCameraShake();

        /// <summary>
        /// [설명]: 카메라의 시야각(FOV)을 부드럽게 변경하여 줌 인/아웃 효과를 줍니다.
        /// </summary>
        void PlayCameraZoom(float targetFOV, float duration);

        /// <summary>
        /// [설명]: 특정 대상을 추적하며 카메라 줌을 수행합니다.
        /// </summary>
        /// <param name="target">추적 대상</param>
        /// <param name="targetValue">목표 FOV 또는 OrthoSize</param>
        /// <param name="duration">지속 시간</param>
        void PlayCameraZoomOnTarget(Transform target, float targetValue, float duration);

        /// <summary>
        /// [설명]: 카메라의 위치와 FOV를 원래 상태로 되돌립니다.
        /// </summary>
        void ResetCamera(float duration);
    }
}
