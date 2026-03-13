using UnityEngine;
using System.Collections.Generic;
using TowerBreakers.UI.Effects.View;

namespace TowerBreakers.UI.Effects.Logic
{
    /// <summary>
    /// [설명]: DamageTextView 객체의 생성과 재사용을 관리하는 오브젝트 풀 클래스입니다.
    /// 메모리 할당을 줄이고 성능을 최적화하기 위해 사용됩니다.
    /// </summary>
    public class DamageTextPool
    {
        #region 내부 필드
        private readonly DamageTextView m_prefab;
        private readonly Transform m_parent;
        private readonly Stack<DamageTextView> m_pool = new Stack<DamageTextView>();
        #endregion

        public DamageTextPool(DamageTextView prefab, Transform parent)
        {
            m_prefab = prefab;
            m_parent = parent;
        }

        #region 공개 API
        /// <summary>
        /// [설명]: 풀에서 데미지 텍스트 객체를 가져옵니다. 풀이 비어있으면 새로 생성합니다.
        /// </summary>
        /// <returns>사용 가능한 DamageTextView 객체</returns>
        public DamageTextView Get()
        {
            DamageTextView instance;
            if (m_pool.Count > 0)
            {
                instance = m_pool.Pop();
                instance.gameObject.SetActive(true);
            }
            else
            {
                instance = Object.Instantiate(m_prefab, m_parent);
            }

            return instance;
        }

        /// <summary>
        /// [설명]: 사용이 끝난 데미지 텍스트 객체를 풀로 반환합니다.
        /// </summary>
        /// <param name="instance">반환할 DamageTextView 객체</param>
        public void Return(DamageTextView instance)
        {
            if (instance == null) return;

            instance.gameObject.SetActive(false);
            m_pool.Push(instance);
        }
        #endregion
    }
}
