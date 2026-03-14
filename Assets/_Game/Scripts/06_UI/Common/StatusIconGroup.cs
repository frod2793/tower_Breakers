using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace TowerBreakers.UI.Common
{
    /// <summary>
    /// [설명]: 아이콘들의 생성, 삭제 및 공통 애니메이션 연출을 전담하는 컴포넌트입니다.
    /// 플레이어 하트, 적 처치 현황 등 아이콘 기반 UI에서 공통으로 사용됩니다.
    /// </summary>
    public class StatusIconGroup : MonoBehaviour
    {
        #region 에디터 설정
        [Header("배치 설정")]
        [SerializeField, Tooltip("아이콘들이 배치될 부모 컨테이너")]
        private Transform m_container;

        [Header("연출 설정")]
        [SerializeField, Tooltip("아이콘 생성 시 순차적 지연 시간 (초)")]
        private float m_spawnInterval = 0.1f;

        [SerializeField, Tooltip("아이콘 Pop 연출 최대 크기 배율")]
        private float m_popScaleMultiplier = 1.3f;

        [SerializeField, Tooltip("아이콘 소멸 시 흔들림 강도")]
        private float m_shakeStrength = 5f;
        #endregion

        #region 내부 필드
        private readonly List<GameObject> m_activeIcons = new List<GameObject>();
        private Vector3 m_originalScale = Vector3.one;
        #endregion

        #region 초기화
        private void Awake()
        {
            if (m_container == null) m_container = transform;
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: 아이콘들을 순차적으로 생성하여 초기화합니다.
        /// </summary>
        /// <param name="prefab">생성할 아이콘 프리펩</param>
        /// <param name="count">생성할 개수</param>
        public async UniTask SetIcons(GameObject prefab, int count)
        {
            ClearAll();
            
            if (prefab == null) return;
            m_originalScale = prefab.transform.localScale;

            for (int i = 0; i < count; i++)
            {
                CreateIcon(prefab);
                await UniTask.Delay((int)(m_spawnInterval * 1000));
            }
        }

        /// <summary>
        /// [설명]: 서로 다른 프리펩들을 섞어서 순차적으로 생성합니다 (적 현황 UI 등).
        /// </summary>
        /// <param name="prefabs">순서대로 생성할 프리펩 목록</param>
        /// <param name="tags">각 아이콘에 부여할 식별 태그 (선택 사항)</param>
        public async UniTask SetIcons(IReadOnlyList<GameObject> prefabs, IReadOnlyList<object> tags = null)
        {
            ClearAll();

            for (int i = 0; i < prefabs.Count; i++)
            {
                if (prefabs[i] == null) continue;
                
                m_originalScale = prefabs[i].transform.localScale;
                var icon = CreateIcon(prefabs[i]);
                
                if (tags != null && i < tags.Count)
                {
                    var meta = icon.AddComponent<IconMeta>();
                    meta.Tag = tags[i];
                }

                await UniTask.Delay((int)(m_spawnInterval * 1000));
            }
        }

        /// <summary>
        /// [설명]: 가장 마지막에 위치한 아이콘 하나를 제거하며 소멸 연출을 재생합니다.
        /// </summary>
        public void RemoveLast()
        {
            if (m_activeIcons.Count <= 0) return;

            int lastIndex = m_activeIcons.Count - 1;
            var icon = m_activeIcons[lastIndex];
            m_activeIcons.RemoveAt(lastIndex);
            
            PlayRemoveAnimation(icon);
        }

        /// <summary>
        /// [설명]: 특정 태그를 가진 아이콘들 중 하나를 찾아 제거합니다.
        /// </summary>
        /// <param name="tag">제거할 아이콘의 태그</param>
        public void RemoveByTag(object tag)
        {
            for (int i = m_activeIcons.Count - 1; i >= 0; i--)
            {
                var meta = m_activeIcons[i].GetComponent<IconMeta>();
                if (meta != null && Equals(meta.Tag, tag))
                {
                    var icon = m_activeIcons[i];
                    m_activeIcons.RemoveAt(i);
                    PlayRemoveAnimation(icon);
                    return;
                }
            }
        }

        /// <summary>
        /// [설명]: 단일 아이콘을 즉시 생성하고 리스트에 추가합니다.
        /// </summary>
        /// <param name="prefab">생성할 프리펩</param>
        /// <param name="tag">부여할 태그</param>
        public void AddIcon(GameObject prefab, object tag = null)
        {
            if (prefab == null) return;
            
            m_originalScale = prefab.transform.localScale;
            var icon = CreateIcon(prefab);
            
            if (tag != null)
            {
                var meta = icon.AddComponent<IconMeta>();
                meta.Tag = tag;
            }
        }

        /// <summary>
        /// [설명]: 모든 아이콘을 연출 없이 즉시 제거합니다.
        /// </summary>
        public void ClearAll()
        {
            foreach (var icon in m_activeIcons)
            {
                if (icon != null) Destroy(icon);
            }
            m_activeIcons.Clear();
        }
        #endregion

        #region 내부 로직
        private GameObject CreateIcon(GameObject prefab)
        {
            GameObject icon = Instantiate(prefab, m_container);
            icon.transform.localScale = Vector3.zero;
            m_activeIcons.Add(icon);

            // Pop-in 연출
            icon.transform.DOScale(m_originalScale * m_popScaleMultiplier, 0.15f).SetEase(Ease.OutBack)
                .OnComplete(() => icon.transform.DOScale(m_originalScale, 0.1f));

            return icon;
        }

        private void PlayRemoveAnimation(GameObject icon)
        {
            if (icon == null) return;

            icon.transform.DOKill();
            
            // Shake & Fade-out (Scale down) 연출
            icon.transform.DOShakePosition(0.3f, m_shakeStrength, 10, 90, false, true);
            icon.transform.DOScale(m_originalScale * 1.2f, 0.1f).SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    icon.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                        .OnComplete(() => {
                            if (icon != null) Destroy(icon);
                        });
                });
        }
        #endregion

        #region 내부 클래스
        private class IconMeta : MonoBehaviour
        {
            public object Tag;
        }
        #endregion
    }
}
