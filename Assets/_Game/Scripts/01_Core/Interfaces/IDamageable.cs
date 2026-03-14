using System;

namespace TowerBreakers.Core.Interfaces
{
    /// <summary>
    /// [설명]: 데미지를 입을 수 있는 객체를 나타내는 인터페이스입니다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// [설명]: 객체가 죽었는지 여부를 나타냅니다.
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// [설명]: 객체에 데미지를 입힙니다.
        /// </summary>
        /// <param name="damage">입힐 데미지 양</param>
        /// <param name="knockbackForce">밀어낼 힘 (선택 사항)</param>
        void TakeDamage(int damage, float knockbackForce = 0f);
    }
}