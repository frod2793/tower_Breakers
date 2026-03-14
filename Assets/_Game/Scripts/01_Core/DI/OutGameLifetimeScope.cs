using UnityEngine;
using VContainer;
using VContainer.Unity;
using TowerBreakers.UI.Screens;
using TowerBreakers.UI.Equipment;
using TowerBreakers.Player.Data;
using TowerBreakers.Player.Data.Models;
using TowerBreakers.Player.Data.SO;

/// <summary>
/// [설명]: 아웃게임 씬의 DI 컨테이너를 구성하는 LifetimeScope입니다.
/// 장비 UI 파이프라인(InventoryModel → EquipmentViewModel → EquipmentView)을 포함합니다.
/// </summary>
public class OutGameLifetimeScope : LifetimeScope
{
    #region 에디터 설정
    [SerializeField, Tooltip("아웃게임 메인 뷰")]
    private OutGameView m_outGameView;

    [Header("장비 UI")]
    [SerializeField, Tooltip("장비 인벤토리 뷰 (씬에 배치된 컴포넌트)")]
    private EquipmentView m_equipmentView;

    [Header("데이터베이스 및 설정")]
    [SerializeField, Tooltip("장비 데이터베이스 (ID → SO 변환용)")]
    private EquipmentDatabase m_equipmentDatabase;

    [SerializeField, Tooltip("플레이어 데이터 (기본 장비 확인용)")]
    private PlayerData m_playerData;
    #endregion

    protected override void Configure(IContainerBuilder builder)
    {
        // 1. 세션 모델 등록 (생성자에서 PlayerPrefs 자동 로드)
        builder.Register<UserSessionModel>(Lifetime.Singleton);

        // 2. 장비 데이터베이스 및 플레이어 데이터 등록
        if (m_equipmentDatabase != null)
        {
            builder.RegisterInstance(m_equipmentDatabase);
        }

        if (m_playerData != null)
        {
            builder.RegisterInstance(m_playerData);
        }

        // 3. 장비 UI에 필요한 데이터 모델 등록 (아웃게임 전용 인스턴스)
        builder.Register<InventoryModel>(Lifetime.Singleton);
        builder.Register<PlayerModel>(Lifetime.Singleton);

        // 4. 뷰모델 등록
        builder.Register<OutGameViewModel>(Lifetime.Singleton);
        builder.Register<EquipmentViewModel>(Lifetime.Singleton);

        // 5. 씬에 배치된 뷰 등록
        if (m_outGameView != null)
        {
            builder.RegisterComponent(m_outGameView);
        }

        // 6. 장비 뷰 자동 검색 (Inspector 미할당 시 씬에서 탐색)
        if (m_equipmentView == null)
        {
            m_equipmentView = FindFirstObjectByType<EquipmentView>();
        }

        // 7. 빌드 완료 후 모든 뷰 초기화
        builder.RegisterBuildCallback(resolver =>
        {
            InitializeViews(resolver);
        });
    }

    /// <summary>
    /// [설명]: DI 빌드 완료 후 뷰에 뷰모델을 주입하고 초기화합니다.
    /// </summary>
    private void InitializeViews(IObjectResolver resolver)
    {
        // 아웃게임 메인 뷰 초기화
        if (m_outGameView != null)
        {
            var outGameVM = resolver.Resolve<OutGameViewModel>();
            m_outGameView.Initialize(outGameVM);
        }

        // 장비 인벤토리 뷰 초기화 (세션 데이터 → InventoryModel 동기화 포함)
        if (m_equipmentView != null)
        {
            var equipmentVM = resolver.Resolve<EquipmentViewModel>();
            m_equipmentView.Initialize(equipmentVM);
            Debug.Log("[OutGameLifetimeScope] 장비 UI 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[OutGameLifetimeScope] EquipmentView를 찾을 수 없습니다. 장비 UI가 표시되지 않습니다.");
        }

        // 세션 데이터 확인 로그
        var sessionModel = resolver.Resolve<UserSessionModel>();
        var dto = sessionModel.CurrentEquipment;
        if (dto != null)
        {
            Debug.Log($"[OutGameLifetimeScope] 세션 로드 - 보유 무기: {dto.OwnedWeaponIds.Count}개, 보유 갑주: {dto.OwnedArmorIds.Count}개");
        }
    }
}
