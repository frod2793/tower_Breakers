using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using TowerBreakers.Enemy.Data;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.UI.HUD
{
    /// <summary>
    /// [설명]: 남은 적의 수를 아이콘 기반으로 표시하는 뷰 클래스입니다.
    /// 일반 적과 특수 개체를 구분하여 아이콘을 배치하고 처치 시 연출을 수행합니다.
    /// </summary>
    public class EnemyStatusView : MonoBehaviour
    {
        #region 에디터 설정
        [Header("컴포넌트")]
        [SerializeField, Tooltip("특수 적 아이콘 그룹 (첫 번째 줄)")]
        private TowerBreakers.UI.Common.StatusIconGroup m_specialIconGroup;

        [SerializeField, Tooltip("일반 적 아이콘 그룹 (두 번째 줄)")]
        private TowerBreakers.UI.Common.StatusIconGroup m_normalIconGroup;

        [Header("프리팹 설정")]
        [SerializeField, Tooltip("일반 적 아이콘 프리팹")]
        private GameObject m_normalEnemyPrefab;

        [SerializeField, Tooltip("특수 적(탱커, 보스 등) 아이콘 프리팹")]
        private GameObject m_specialEnemyPrefab;
        #endregion

        #region 내부 필드
        private readonly List<EnemyType> m_currentEnemyTypes = new List<EnemyType>();
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 새로운 층의 적 구성에 맞춰 특수/일반 층별로 아이콘들을 초기화합니다.
        /// </summary>
        /// <param name="enemyTypes">해당 층의 전체 적 타입 목록</param>
        public void InitializeFloor(IReadOnlyList<EnemyType> enemyTypes)
        {
            m_currentEnemyTypes.Clear();
            m_currentEnemyTypes.AddRange(enemyTypes);

            // 특수 적 리스트 구성
            var specialPrefabs = new List<GameObject>();
            var specialTags = new List<object>();

            // 일반 적 리스트 구성
            var normalPrefabs = new List<GameObject>();
            var normalTags = new List<object>();

            foreach (var type in enemyTypes)
            {
                if (IsSpecial(type))
                {
                    specialPrefabs.Add(m_specialEnemyPrefab);
                    specialTags.Add(type);
                }
                else
                {
                    normalPrefabs.Add(m_normalEnemyPrefab);
                    normalTags.Add(type);
                }
            }

            // 각 그룹별 초기화
            if (m_specialIconGroup != null)
                m_specialIconGroup.SetIcons(specialPrefabs, specialTags).Forget();

            if (m_normalIconGroup != null)
                m_normalIconGroup.SetIcons(normalPrefabs, normalTags).Forget();
        }

        /// <summary>
        /// [설명]: 남은 적 목록이 변경되었을 때 (처치 또는 등록 시) UI를 갱신합니다.
        /// </summary>
        /// <param name="remainingTypes">현재 남은 적 타입 목록</param>
        public void UpdateStatus(IReadOnlyList<EnemyType> remainingTypes)
        {
            if (m_currentEnemyTypes.Count == remainingTypes.Count) return;

            // 1. 적이 줄어든 경우 (처치)
            if (remainingTypes.Count < m_currentEnemyTypes.Count)
            {
                var typesToRemove = new List<EnemyType>(m_currentEnemyTypes);
                foreach (var type in remainingTypes)
                {
                    typesToRemove.Remove(type);
                }

                foreach (var removedType in typesToRemove)
                {
                    if (IsSpecial(removedType))
                        m_specialIconGroup?.RemoveByTag(removedType);
                    else
                        m_normalIconGroup?.RemoveByTag(removedType);
                }
            }
            // 2. 적이 늘어난 경우 (초기 등록 중)
            else if (remainingTypes.Count > m_currentEnemyTypes.Count)
            {
                var typesToAdd = new List<EnemyType>(remainingTypes);
                foreach (var type in m_currentEnemyTypes)
                {
                    typesToAdd.Remove(type);
                }

                foreach (var addedType in typesToAdd)
                {
                    bool isSpec = IsSpecial(addedType);
                    var group = isSpec ? m_specialIconGroup : m_normalIconGroup;
                    var prefab = isSpec ? m_specialEnemyPrefab : m_normalEnemyPrefab;
                    
                    if (group != null && prefab != null)
                    {
                        group.AddIcon(prefab, addedType);
                    }
                }
            }

            m_currentEnemyTypes.Clear();
            m_currentEnemyTypes.AddRange(remainingTypes);
        }
        #endregion

        #region 내부 로직
        private bool IsSpecial(EnemyType type)
        {
            // 'Normal'을 제외한 모든 타입(Tank, SupportBuffer, SupportShooter, Elite, Boss)을 특수 개체로 간주
            return type != EnemyType.Normal;
        }
        #endregion
    }
}
