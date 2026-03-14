using UnityEngine;
using TowerBreakers.Player.Logic;
using TowerBreakers.Player.Data.SO;
using System;

namespace TowerBreakers.Player.View
{
    /// <summary>
    /// [설명]: SPUM 캐릭터 프리팹과 플레이어 로직을 연결하는 뷰 클래스입니다.
    /// MVVM의 View 역할을 수행합니다.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("SPUM 프리팹 참조")]
        private SPUM_Prefabs m_spumPrefabs;

        [SerializeField, Tooltip("잔상 효과 관리 컴포넌트")]
        private AfterimageEffect m_afterImage;
        #endregion

        #region 내부 변수
        private PlayerStateMachine m_stateMachine;
        
        // 장비 렌더러 캐시
        private SpriteRenderer m_mainWeaponRenderer;
        private SpriteRenderer m_bodyArmorRenderer;
        private SpriteRenderer m_leftShoulderRenderer;
        private SpriteRenderer m_rightShoulderRenderer;
        private SpriteRenderer m_hairRenderer;
        private System.Collections.Generic.List<SpriteRenderer> m_helmetRenderers = new();
        private bool m_isRendererCached;
        #endregion

        #region 프로퍼티
        public SPUM_Prefabs SpumPrefabs => m_spumPrefabs;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성을 주입받아 초기화합니다.
        /// </summary>
        public void Initialize(PlayerStateMachine stateMachine)
        {
            m_stateMachine = stateMachine;
            
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.OverrideControllerInit();
            }
            else
            {
                global::UnityEngine.Debug.LogError("[PlayerView] SPUM_Prefabs가 설정되지 않았습니다.");
            }

            // [추가]: 잔상 컴포넌트 초기화
            if (m_afterImage != null)
            {
                m_afterImage.Initialize(this);
            }

            CacheRenderers();
        }

        /// <summary>
        /// [설명]: SPUM 캐릭터의 부위별 SpriteRenderer를 캐싱합니다.
        /// </summary>
        private void CacheRenderers()
        {
            if (m_isRendererCached) return;

            var matchingList = GetComponentInChildren<SPUM_MatchingList>();
            if (matchingList != null)
            {
                foreach (var element in matchingList.matchingTables)
                {
                    if (element.renderer == null) continue;

                    string partType = element.PartType;
                    string structure = element.Structure;

                    if (partType == "Weapons" && element.Dir == "Right") m_mainWeaponRenderer = element.renderer;
                    else if (partType == "Armor")
                    {
                        if (structure == "Body") m_bodyArmorRenderer = element.renderer;
                        else if (structure == "Left") m_leftShoulderRenderer = element.renderer;
                        else if (structure == "Right") m_rightShoulderRenderer = element.renderer;
                    }
                    else if (partType == "Helmet") m_helmetRenderers.Add(element.renderer);
                    else if (partType == "Hair") m_hairRenderer = element.renderer;
                }
            }
            m_isRendererCached = true;
        }
        #endregion

        #region 공개 API
        /// <summary>
        /// [설명]: SPUM 애니메이션을 플레이합니다.
        /// </summary>
        public void PlayAnimation(global::PlayerState state, int index = 0)
        {
            if (m_spumPrefabs != null)
            {
                m_spumPrefabs.PlayAnimation(state, index);
            }
        }

        /// <summary>
        /// [설명]: 잔상 효과를 시작하거나 중지합니다.
        /// </summary>
        public void SetAfterImage(bool active)
        {
            if (m_afterImage == null)
            {
                m_afterImage = GetComponent<AfterimageEffect>();
            }

            if (m_afterImage != null)
            {
                // [리팩토링]: 비동기 기반의 새로운 잔상 시스템 호출
                if (active) m_afterImage.StartEffect();
                else m_afterImage.StopEffect();
            }
        }

        /// <summary>
        /// [설명]: 프리뷰 또는 실제 장착 시 무기 외형을 즉시 교체합니다.
        /// </summary>
        public void SetWeapon(WeaponData weapon)
        {
            if (weapon == null) return;
            CacheRenderers();

            if (m_mainWeaponRenderer != null)
            {
                m_mainWeaponRenderer.sprite = weapon.WeaponSprite;
            }

            // 애니메이션 클립 교체
            if (m_spumPrefabs != null && m_spumPrefabs.ATTACK_List != null && m_spumPrefabs.ATTACK_List.Count > 0)
            {
                m_spumPrefabs.ATTACK_List[0] = weapon.AttackClip;
                m_spumPrefabs.OverrideControllerInit();
            }
        }

        /// <summary>
        /// [설명]: 프리뷰 또는 실제 장착 시 갑주 외형을 즉시 교체합니다.
        /// </summary>
        public void SetArmor(ArmorData armor)
        {
            if (armor == null) return;
            CacheRenderers();

            if (armor.Category == ArmorCategory.Helmet)
            {
                foreach (var renderer in m_helmetRenderers)
                {
                    if (renderer != null) renderer.sprite = armor.HelmetSprite;
                }

                if (m_hairRenderer != null)
                {
                    m_hairRenderer.gameObject.SetActive(armor.HelmetSprite == null);
                }
            }
            else if (armor.Category == ArmorCategory.BodyArmor)
            {
                if (m_bodyArmorRenderer != null) m_bodyArmorRenderer.sprite = armor.BodyArmorSprite;
                if (m_leftShoulderRenderer != null) m_leftShoulderRenderer.sprite = armor.LeftShoulderSprite;
                if (m_rightShoulderRenderer != null) m_rightShoulderRenderer.sprite = armor.RightShoulderSprite;
            }
        }
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            // [추가]: 인스펙터에서 할당되지 않았을 경우 자식에서 자동으로 찾음
            if (m_spumPrefabs == null)
            {
                m_spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
                if (m_spumPrefabs != null)
                {
                    global::UnityEngine.Debug.Log("[PlayerView] SPUM_Prefabs를 자식 오브젝트에서 자동 할당했습니다.");
                }
            }

            if (m_afterImage == null)
            {
                m_afterImage = GetComponent<AfterimageEffect>();
            }
        }

        private void Update()
        {
            // 로직 업데이트는 GameController에서 수행되지만, 
            // View와 관련된 애니메이션 동기화 등이 필요할 수 있음
        }
        #endregion
    }
}
