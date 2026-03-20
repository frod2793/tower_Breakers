using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TowerBreakers.UI.View
{
    /// <summary>
    /// [클래스]: 여러 개의 아이콘(적 수량, 체력 등)을 시각화하고 애니메이션을 관리하는 범용 컴포넌트입니다.
    /// 프리팹의 기본 스케일을 유지하며 생성/파괴 애니메이션을 수행합니다.
    /// </summary>
    public class BattleIconGroup : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("아이콘 프리팹")]
        private GameObject m_iconPrefab;

        [SerializeField, Tooltip("최대 수용 가능 아이콘 수 (풀링용)")]
        private int m_poolSize = 20;
        #endregion

        #region 내부 필드
        private readonly List<GameObject> m_iconPool = new List<GameObject>();
        private readonly List<GameObject> m_activeIcons = new List<GameObject>();
        private Vector3 m_baseScale = Vector3.one; // [추가]: 프리팹의 기본 스케일 캐시
        private int m_currentCount = 0;
        #endregion

        #region 초기화
        private void Awake()
        {
            InitializePool();
        }

        private void InitializePool()
        {
            if (m_iconPrefab == null) return;

            // [핵심]: 프리팹의 기본 스케일을 미리 저장
            m_baseScale = m_iconPrefab.transform.localScale;

            for (int i = 0; i < m_poolSize; i++)
            {
                var icon = Instantiate(m_iconPrefab, transform);
                icon.SetActive(false);
                m_iconPool.Add(icon);
            }
        }
        #endregion

        #region 공개 API
        public void SetCount(int count)
        {
            if (count == m_currentCount) return;

            if (count > m_currentCount)
            {
                for (int i = m_currentCount; i < count; i++)
                {
                    ActivateIcon(i);
                }
            }
            else
            {
                for (int i = m_currentCount - 1; i >= count; i--)
                {
                    DeactivateIcon(i);
                }
            }

            m_currentCount = count;
        }

        public void Clear()
        {
            foreach (var icon in m_activeIcons)
            {
                icon.transform.DOKill();
                icon.SetActive(false);
            }
            m_activeIcons.Clear();
            m_currentCount = 0;
        }
        #endregion

        #region 내부 로직 및 애니메이션
        private void ActivateIcon(int index)
        {
            if (index >= m_iconPool.Count) return;

            var icon = m_iconPool[index];
            if (icon == null) return;

            icon.SetActive(true);
            m_activeIcons.Add(icon);

            // [리팩토링]: 프리팹의 원래 스케일(m_baseScale)로 애니메이션
            icon.transform.localScale = Vector3.zero;
            icon.transform.DOScale(m_baseScale, 0.3f).SetEase(Ease.OutBack);
            
            var img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.DOFade(1f, 0.2f).From(0f);
            }
        }

        private void DeactivateIcon(int index)
        {
            if (index < 0 || index >= m_activeIcons.Count) return;

            var icon = m_activeIcons[index];
            m_activeIcons.RemoveAt(index);

            if (icon == null) return;

            // [연출]: 파괴 애니메이션
            Sequence seq = DOTween.Sequence();
            seq.Append(icon.transform.DOShakePosition(0.3f, 5f, 10)); // 떨림 강도 조절
            seq.Join(icon.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
            
            var img = icon.GetComponent<Image>();
            if (img != null)
            {
                seq.Join(img.DOFade(0f, 0.3f));
            }

            seq.OnComplete(() => icon.SetActive(false));
        }
        #endregion
    }
}
