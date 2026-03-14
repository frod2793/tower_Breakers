using UnityEngine;
using TowerBreakers.Player.Data.SO;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.View;
using VContainer;
using System;
using System.Collections.Generic;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 무기/갑주 장착 및 외형/애니메이션 교체를 담당하는 컴포넌트입니다.
    /// PlayerModel의 장비 변경 이벤트를 구독하여 실시간으로 반영합니다.
    /// </summary>
    public class PlayerEquipment : MonoBehaviour
    {
        #region 내부 필드
        private PlayerModel m_model;
        private PlayerView m_view;

        // 렌더러 캐시
        private SpriteRenderer m_mainWeaponRenderer;
        private List<SpriteRenderer> m_otherWeaponRenderers = new List<SpriteRenderer>();
        
        // 갑주 전용 렌더러 캐시
        private SpriteRenderer m_bodyArmorRenderer;
        private SpriteRenderer m_leftShoulderRenderer;
        private SpriteRenderer m_rightShoulderRenderer;
        private SpriteRenderer m_hairRenderer; // [추가]: 헬멧 장착 시 숨길 머리카락
        private List<SpriteRenderer> m_helmetRenderers = new List<SpriteRenderer>();

        private bool m_isRendererCached;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성을 주입받아 장비 변경 이벤트를 구독합니다.
        /// </summary>
        [Inject]
        public void Initialize(PlayerModel model, PlayerView view)
        {
            m_model = model;
            m_view = view;

            CacheAllRenderers();

            if (m_model != null)
            {
                m_model.OnWeaponChanged += UpdateWeaponVisuals;
                m_model.OnHelmetChanged += UpdateHelmetVisuals;
                m_model.OnBodyArmorChanged += UpdateBodyArmorVisuals;

                // 현재 이미 장착된 무기가 있다면 즉시 시각화 갱신
                if (m_model.CurrentWeapon != null)
                {
                    UpdateWeaponVisuals(m_model.CurrentWeapon);
                }

                // 장착된 헬멧/흉갑이 있다면 갱신
                if (m_model.CurrentHelmet != null)
                {
                    UpdateHelmetVisuals(m_model.CurrentHelmet);
                }

                if (m_model.CurrentBodyArmor != null)
                {
                    UpdateBodyArmorVisuals(m_model.CurrentBodyArmor);
                }
            }
        }

        /// <summary>
        /// [설명]: 장비 관련 SpriteRenderer들을 캐싱합니다. 최초 1회만 수행됩니다.
        /// SPUM_MatchingList가 있다면 이를 우선 활용하고, 없으면 이름 기반으로 찾습니다.
        /// </summary>
        private void CacheAllRenderers()
        {
            if (m_view == null || m_isRendererCached) return;

            // 초기화
            m_otherWeaponRenderers.Clear();
            m_helmetRenderers.Clear();

            // SPUM_MatchingList 활용 시도
            var matchingList = m_view.GetComponentInChildren<SPUM_MatchingList>();
            if (matchingList != null && matchingList.matchingTables != null && matchingList.matchingTables.Count > 0)
            {
                foreach (var element in matchingList.matchingTables)
                {
                    if (element.renderer == null) continue;

                    string partType = element.PartType;
                    string structure = element.Structure;

                    switch (partType)
                    {
                        case "Armor":
                            if (structure == "Body") m_bodyArmorRenderer = element.renderer;
                            else if (structure == "Left") m_leftShoulderRenderer = element.renderer;
                            else if (structure == "Right") m_rightShoulderRenderer = element.renderer;
                            break;
                        case "Helmet":
                            m_helmetRenderers.Add(element.renderer);
                            break;
                        case "Hair":
                            m_hairRenderer = element.renderer;
                            break;
                        case "Weapons":
                            if (element.Dir == "Right") m_mainWeaponRenderer = element.renderer;
                            else m_otherWeaponRenderers.Add(element.renderer);
                            break;
                    }
                }
            }
            else
            {
                // MatchingList가 없을 경우 이름 기반 검색 (Fallback)
                SpriteRenderer[] allRenderers = m_view.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var renderer in allRenderers)
                {
                    string objName = renderer.gameObject.name;

                    if (objName.Contains("Weapon", StringComparison.OrdinalIgnoreCase))
                    {
                        if (objName.Contains("R_") || objName.Contains("Right")) m_mainWeaponRenderer = renderer;
                        else m_otherWeaponRenderers.Add(renderer);
                    }
                    else if (objName.Equals("BodyArmor", StringComparison.OrdinalIgnoreCase)) m_bodyArmorRenderer = renderer;
                    else if (objName.Contains("Shoulder"))
                    {
                        if (objName.Contains("L_") || objName.Contains("Left")) m_leftShoulderRenderer = renderer;
                        else m_rightShoulderRenderer = renderer;
                    }
                    else if (objName.Contains("Helmet")) m_helmetRenderers.Add(renderer);
                    else if (objName.Contains("Hair") && !objName.Contains("Face")) m_hairRenderer = renderer;
                }
            }

            // R_Weapon이 없을 경우 첫 번째를 주무기로 사용
            if (m_mainWeaponRenderer == null && m_otherWeaponRenderers.Count > 0)
            {
                m_mainWeaponRenderer = m_otherWeaponRenderers[0];
                m_otherWeaponRenderers.RemoveAt(0);
            }

            m_isRendererCached = true;
        }

        private void OnDestroy()
        {
            if (m_model != null)
            {
                m_model.OnWeaponChanged -= UpdateWeaponVisuals;
                m_model.OnHelmetChanged -= UpdateHelmetVisuals;
                m_model.OnBodyArmorChanged -= UpdateBodyArmorVisuals;
            }
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// [설명]: 무기 데이터를 기반으로 SPUM의 스프라이트와 공격 애니메이션 클립을 교체합니다.
        /// </summary>
        /// <param name="weapon">새로 장착할 무기 데이터</param>
        private void UpdateWeaponVisuals(WeaponData weapon)
        {
            if (m_view == null || m_view.SpumPrefabs == null || weapon == null) return;

            var spum = m_view.SpumPrefabs;

            // 1. 스프라이트(외형) 교체
            if (m_mainWeaponRenderer != null)
            {
                m_mainWeaponRenderer.sprite = weapon.WeaponSprite;
            }

            // 보조 무기 비우기
            foreach (var renderer in m_otherWeaponRenderers)
            {
                if (renderer != null)
                {
                    renderer.sprite = null;
                }
            }

            // 2. 애니메이션 클립(모션) 교체
            if (spum.ATTACK_List != null && spum.ATTACK_List.Count > 0)
            {
                spum.ATTACK_List[0] = weapon.AttackClip;
                spum.OverrideControllerInit();
                Debug.Log($"[PlayerEquipment] 무기 교체 완료: {weapon.WeaponName}");
            }
        }

        /// <summary>
        /// [설명]: 헬멧 데이터를 기반으로 머리 부위 외형을 교체합니다.
        /// </summary>
        /// <param name="helmet">새로 장착할 헬멧 데이터</param>
        private void UpdateHelmetVisuals(ArmorData helmet)
        {
            if (m_view == null || helmet == null) return;

            // 헬멧 스프라이트 갱신
            foreach (var helmetRenderer in m_helmetRenderers)
            {
                if (helmetRenderer != null)
                    helmetRenderer.sprite = helmet.HelmetSprite;
            }

            // 헬멧 장착 시 머리카락 숨기기
            if (m_hairRenderer != null)
            {
                m_hairRenderer.gameObject.SetActive(helmet.HelmetSprite == null);
            }

            Debug.Log($"[PlayerEquipment] 헬멧 교체 완료: {helmet.ArmorName}");
        }

        /// <summary>
        /// [설명]: 흉갑 데이터를 기반으로 몸체 스프라이트와 피격/사망 애니메이션을 교체합니다.
        /// </summary>
        /// <param name="bodyArmor">새로 장착할 흉갑 데이터</param>
        private void UpdateBodyArmorVisuals(ArmorData bodyArmor)
        {
            if (m_view == null || m_view.SpumPrefabs == null || bodyArmor == null) return;

            // 흉갑 스프라이트 갱신
            if (m_bodyArmorRenderer != null) 
                m_bodyArmorRenderer.sprite = bodyArmor.BodyArmorSprite;

            // 어깨 스프라이트 갱신
            if (m_leftShoulderRenderer != null)
                m_leftShoulderRenderer.sprite = bodyArmor.LeftShoulderSprite;

            if (m_rightShoulderRenderer != null)
                m_rightShoulderRenderer.sprite = bodyArmor.RightShoulderSprite;
            
            Debug.Log($"[PlayerEquipment] 흉갑 교체 완료: {bodyArmor.ArmorName}");
        }
        #endregion
    }
}
