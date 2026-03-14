using System;
using TowerBreakers.Player.Data.SO;
using UnityEngine;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 층 클리어 이벤트입니다.
    /// </summary>
    public struct OnFloorCleared
    {
        public int FloorIndex;

        public OnFloorCleared(int floorIndex)
        {
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 현재 층의 적을 모두 처치하여 다음 층으로 갈 준비가 되었음을 알리는 이벤트입니다.
    /// HUD에서 'GO' UI를 표시하는 데 사용됩니다.
    /// </summary>
    public struct OnFloorReadyForNext { }

    /// <summary>
    /// [설명]: 타워의 마지막 층을 클리어하여 타워 돌파에 성공했음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnTowerCleared { }

    /// <summary>
    /// [설명]: 현재 층의 모든 적이 처치되었음을 알리는 이벤트입니다.
    /// 보상 상자가 있다면 이 이벤트를 통해 활성화됩니다.
    /// </summary>
    public struct OnFloorEnemiesCleared
    {
        public int FloorIndex;
        public OnFloorEnemiesCleared(int floorIndex) => FloorIndex = floorIndex;
    }

    /// <summary>
    /// [설명]: 보상 상자가 씬에 존재함을 알리는 이벤트입니다 (TowerManager 등록용).
    /// </summary>
    public struct OnRewardChestRegistered
    {
        public int FloorIndex;
        public OnRewardChestRegistered(int floorIndex) => FloorIndex = floorIndex;
    }

    /// <summary>
    /// [설명]: 상자가 열리고 실제 보상 아이템이 결정되었을 때 연출을 위해 발행됩니다.
    /// </summary>
    public struct OnRewardSpawned
    {
        public string RewardKey;
        public UnityEngine.Vector3 Position;
        public int FloorIndex;

        public OnRewardSpawned(string rewardKey, UnityEngine.Vector3 position, int floorIndex)
        {
            RewardKey = rewardKey;
            Position = position;
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 특정 층의 전투가 시작됨을 알리는 이벤트입니다.
    /// 선스폰된 적들이 이 이벤트를 수신하여 진격을 시작합니다.
    /// </summary>
    public struct OnFloorStarted
    {
        public int FloorIndex;

        public OnFloorStarted(int floorIndex)
        {
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 플레이어가 벽에 압착되어 데미지를 받았을 때 발행되는 이벤트입니다.
    /// 모든 적은 이 이벤트를 수신하여 동결(Frozen) 상태로 전환됩니다.
    /// </summary>
    public struct OnWallCrushOccurred
    {
        public int Damage;
        public int FloorIndex;

        public OnWallCrushOccurred(int damage, int floorIndex)
        {
            Damage = damage;
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 보스 등장연출이 시작되었음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnBossIntroStarted { }

    /// <summary>
    /// [설명]: 보스 등장연출이 종료되었음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnBossIntroEnded { }

    /// <summary>
    /// [설명]: 보상 상자가 특정 위치에 스폰되었음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnRewardChestSpawned
    {
        public UnityEngine.Vector3 Position;
        public int FloorIndex;

        public OnRewardChestSpawned(UnityEngine.Vector3 position, int floorIndex)
        {
            Position = position;
            FloorIndex = floorIndex;
        }
    }

    /// <summary>
    /// [설명]: 보상 상자가 플레이어에 의해 열렸음을 알리는 이벤트입니다.
    /// </summary>
    public struct OnRewardChestOpened
    {
        public UnityEngine.Vector3 Position;
        public int FloorIndex;
        public RewardTableData RewardTable;

        public OnRewardChestOpened(UnityEngine.Vector3 position, int floorIndex, RewardTableData rewardTable)
        {
            Position = position;
            FloorIndex = floorIndex;
            RewardTable = rewardTable;
        }
    }
}
