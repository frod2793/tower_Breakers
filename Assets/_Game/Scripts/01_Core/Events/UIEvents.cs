using System;
using UnityEngine;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 데미지 텍스트 생성을 요청하는 이벤트입니다.
    /// </summary>
    public struct OnDamageTextRequested
    {
        public UnityEngine.Vector3 Position;
        public int Damage;
        public bool IsCritical;

        public OnDamageTextRequested(UnityEngine.Vector3 position, int damage, bool isCritical = false)
        {
            Position = position;
            Damage = damage;
            IsCritical = isCritical;
        }
    }

    /// <summary>
    /// [설명]: 보물상자(보상) 획득 이벤트입니다.
    /// </summary>
    public struct OnChestCollected
    {
        public int Count;

        public OnChestCollected(int count = 1)
        {
            Count = count;
        }
    }
}
