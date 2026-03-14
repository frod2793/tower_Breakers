using UnityEngine;
using System.Collections.Generic;

namespace TowerBreakers.Effects
{
    /// <summary>
    /// [설명]: 이펙트 타입과 프리팹을 매핑하는 데이터 단위입니다.
    /// </summary>
    [System.Serializable]
    public struct EffectData
    {
        [Tooltip("이펙트 식별 타입")]
        public EffectType Type;
        
        [Tooltip("생성할 이펙트 프리팹")]
        public GameObject Prefab;
        
        [Tooltip("기본 풀 생성 개수 (0이면 매니저 기본값 사용)")]
        public int PoolSize;

        [Header("연출 설정 (타격 시)")]
        [Tooltip("카메라 흔들림 강도")]
        public float ShakeIntensity;

        [Tooltip("카메라 흔들림 지속 시간")]
        public float ShakeDuration;

        [Tooltip("역경직(HitStop) 지속 시간")]
        public float HitStopDuration;
    }

    /// <summary>
    /// [설명]: 모든 이펙트 프리팹 정보를 중앙에서 관리하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EffectDatabase", menuName = "TowerBreakers/Database/EffectDatabase")]
    public class EffectDatabase : ScriptableObject
    {
        #region 에디터 설정
        [SerializeField, Tooltip("관리할 이펙트 데이터 리스트")]
        private List<EffectData> m_effects = new List<EffectData>();
        #endregion

        #region 프로퍼티
        public IReadOnlyList<EffectData> Effects => m_effects;
        #endregion
        
        #region 공개 메서드
        /// <summary>
        /// [설명]: 특정 타입의 이펙트 데이터를 찾아 반환합니다.
        /// </summary>
        public EffectData? GetEffectData(EffectType type)
        {
            for (int i = 0; i < m_effects.Count; i++)
            {
                if (m_effects[i].Type == type) return m_effects[i];
            }
            return null;
        }
        #endregion
    }
}
