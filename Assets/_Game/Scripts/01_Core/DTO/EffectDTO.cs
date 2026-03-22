using UnityEngine;
using System;
using System.Collections.Generic;

namespace TowerBreakers.Core.DTO
{
    #region 데이터 모델 (DTO)
    /// <summary>
    /// [설명]: 개별 이펙트의 프리팹 매핑 및 설정을 담는 DTO입니다.
    /// </summary>
    [Serializable]
    public class EffectDataDTO
    {
        public string EffectId;
        public GameObject Prefab;
        public int InitialPoolSize = 5;
    }

    /// <summary>
    /// [설명]: 이펙트 시스템 전체 설정을 관리하는 DTO입니다.
    /// </summary>
    [Serializable]
    public class EffectConfigDTO
    {
        [Header("카메레 쉐이크 기본값")]
        public float DefaultShakeDuration = 0.2f;
        public float DefaultShakeStrength = 0.5f;

        [Header("이펙트 등록 리스트")]
        public List<EffectDataDTO> Effects = new List<EffectDataDTO>();
    }
    #endregion
}
