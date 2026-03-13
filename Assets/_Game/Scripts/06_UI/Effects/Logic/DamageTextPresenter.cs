using UnityEngine;
using TowerBreakers.Core.Events;
using TowerBreakers.UI.Effects.View;
using VContainer;
using System;
using VContainer.Unity;

namespace TowerBreakers.UI.Effects.Logic
{
    /// <summary>
    /// [설명]: 데미지 텍스트 생성 요청 이벤트를 수신하여 실제 연출을 실행하는 프리젠터 클래스입니다.
    /// 비즈니스 로직(이벤트 수신)과 시각적 표현(풀링 및 뷰 호출) 사이의 중계 역할을 합니다.
    /// </summary>
    public class DamageTextPresenter : IInitializable, IDisposable
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly DamageTextPool m_pool;
        private Camera m_mainCamera;
        #endregion

        #region 초기화
        [Inject]
        public DamageTextPresenter(IEventBus eventBus, DamageTextPool pool)
        {
            m_eventBus = eventBus;
            m_pool = pool;
        }

        public void Initialize()
        {
            m_mainCamera = Camera.main;
            m_eventBus.Subscribe<OnDamageTextRequested>(OnDamageTextRequested);
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 데미지 텍스트 생성 요청이 왔을 때 실행됩니다.
        /// 월드 좌표를 스크린 좌표로 변환하여 Overlay Canvas 상에 올바르게 표시합니다.
        /// </summary>
        private void OnDamageTextRequested(OnDamageTextRequested evt)
        {
            if (m_pool == null) return;
            if (m_mainCamera == null) m_mainCamera = Camera.main;

            var view = m_pool.Get();
            if (view != null)
            {
                // 월드 좌표를 스크린 좌표로 변환 (Overlay Canvas 대응)
                Vector3 screenPos = m_mainCamera.WorldToScreenPoint(evt.Position);
                
                // 약간의 랜덤 오프셋을 주어 겹침 방지
                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-20f, 20f), UnityEngine.Random.Range(0f, 20f), 0);
                view.transform.position = screenPos + randomOffset;
                
                view.Show(evt.Damage, evt.IsCritical, OnEffectComplete);
            }
        }

        /// <summary>
        /// [설명]: 텍스트 연출이 끝났을 때 풀로 반환합니다.
        /// </summary>
        private void OnEffectComplete(DamageTextView view)
        {
            m_pool?.Return(view);
        }
        #endregion

        #region 해제
        public void Dispose()
        {
            m_eventBus?.Unsubscribe<OnDamageTextRequested>(OnDamageTextRequested);
        }
        #endregion
    }
}
