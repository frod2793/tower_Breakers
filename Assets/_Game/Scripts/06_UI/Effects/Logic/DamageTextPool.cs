using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using TowerBreakers.UI.Effects.View;

namespace TowerBreakers.UI.Effects.Logic
{
    /// <summary>
    /// [설명]: DamageTextView 객체의 생성과 재사용을 관리하는 오브젝트 풀 클래스입니다.
    /// UnityEngine.Pool API를 사용하여 메모리 할당을 줄이고 성능을 최적화합니다.
    /// </summary>
    public class DamageTextPool
    {
        #region 내부 필드
        private readonly DamageTextView m_prefab;
        private readonly Transform m_parent;
        private readonly IObjectPool<DamageTextView> m_pool;
        #endregion

        public DamageTextPool(DamageTextView prefab, Transform parent)
        {
            m_prefab = prefab;
            m_parent = parent;

            // [최적화]: UnityEngine.Pool 설정
            m_pool = new ObjectPool<DamageTextView>(
                createFunc: OnCreateInstance,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReleaseToPool,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: 100
            );
        }

        #region 풀 콜백
        private DamageTextView OnCreateInstance()
        {
            return Object.Instantiate(m_prefab, m_parent);
        }

        private void OnGetFromPool(DamageTextView instance)
        {
            if (instance != null)
                instance.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(DamageTextView instance)
        {
            if (instance != null)
                instance.gameObject.SetActive(false);
        }

        private void OnDestroyInstance(DamageTextView instance)
        {
            if (instance != null)
                Object.Destroy(instance.gameObject);
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 풀에서 데미지 텍스트 객체를 가져옵니다.
        /// </summary>
        /// <returns>사용 가능한 DamageTextView 객체</returns>
        public DamageTextView Get()
        {
            return m_pool.Get();
        }

        /// <summary>
        /// [설명]: 사용이 끝난 데미지 텍스트 객체를 풀로 반환합니다.
        /// </summary>
        /// <param name="instance">반환할 DamageTextView 객체</param>
        public void Return(DamageTextView instance)
        {
            if (instance != null)
                m_pool.Release(instance);
        }
        #endregion
    }
}
