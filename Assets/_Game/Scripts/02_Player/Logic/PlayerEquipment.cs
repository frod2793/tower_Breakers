using UnityEngine;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.View;
using VContainer;
using System;
using System.Collections.Generic;

namespace TowerBreakers.Player.Logic
{
    /// <summary>
    /// [설명]: 플레이어의 무기 장착 및 외형/애니메이션 교체를 담당하는 컴포넌트입니다.
    /// PlayerModel의 무기 변경 이벤트를 구독하여 실시간으로 반영합니다.
    /// </summary>
    public class PlayerEquipment : MonoBehaviour
    {
        #region 내부 필드
        private PlayerModel m_model;
        private PlayerView m_view;

        // 렌더러 캐시 (Awake 후 한 번만 탐색)
        private SpriteRenderer m_mainWeaponRenderer;
        private List<SpriteRenderer> m_otherWeaponRenderers = new List<SpriteRenderer>();
        private bool m_isRendererCached;
        #endregion

        #region 초기화
        /// <summary>
        /// [설명]: 의존성을 주입받아 무기 변경 이벤트를 구독합니다.
        /// </summary>
        [Inject]
        public void Initialize(PlayerModel model, PlayerView view)
        {
            m_model = model;
            m_view = view;

            CacheWeaponRenderers();

            if (m_model != null)
            {
                m_model.OnWeaponChanged += UpdateWeaponVisuals;

                // 현재 이미 장착된 무기가 있다면 즉시 시각화 갱신
                if (m_model.CurrentWeapon != null)
                {
                    UpdateWeaponVisuals(m_model.CurrentWeapon);
                }
            }
        }

        /// <summary>
        /// [설명]: 무기 관련 SpriteRenderer를 캐싱합니다. 최초 1회만 수행됩니다.
        /// </summary>
        private void CacheWeaponRenderers()
        {
            if (m_isRendererCached || m_view == null) return;

            m_mainWeaponRenderer = null;
            m_otherWeaponRenderers.Clear();

            var allRenderers = m_view.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in allRenderers)
            {
                string objName = renderer.gameObject.name;

                if (objName.Contains("Weapon", StringComparison.OrdinalIgnoreCase))
                {
                    if (objName.Contains("R_Weapon", StringComparison.OrdinalIgnoreCase))
                    {
                        m_mainWeaponRenderer = renderer;
                    }
                    else
                    {
                        m_otherWeaponRenderers.Add(renderer);
                    }
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
            if (m_view == null)
            {
                Debug.LogError("[PlayerEquipment] PlayerView가 null입니다.");
                return;
            }

            if (m_view.SpumPrefabs == null)
            {
                Debug.LogError("[PlayerEquipment] PlayerView.SpumPrefabs가 null입니다.");
                return;
            }

            if (weapon == null)
            {
                Debug.LogWarning("[PlayerEquipment] 전달된 WeaponData가 null입니다.");
                return;
            }

            var spum = m_view.SpumPrefabs;

            // 1. 스프라이트(외형) 교체
            if (m_mainWeaponRenderer != null)
            {
                m_mainWeaponRenderer.sprite = weapon.WeaponSprite;
                Debug.Log($"[PlayerEquipment] 주무기 렌더러 교체 완료: {m_mainWeaponRenderer.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[PlayerEquipment] 'Weapon' 이름이 포함된 SpriteRenderer를 찾지 못했습니다. 계층 구조를 확인해주세요.");
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
                Debug.Log($"[PlayerEquipment] 애니메이션 클립 교체 완료: {weapon.WeaponName}");
            }
            else
            {
                Debug.LogWarning("[PlayerEquipment] SPUM.ATTACK_List가 비어있어 애니메이션 오버라이드를 건너뜁니다.");
            }
        }
        #endregion
    }
}
