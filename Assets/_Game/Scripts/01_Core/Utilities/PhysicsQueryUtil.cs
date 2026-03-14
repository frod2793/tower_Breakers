using UnityEngine;

namespace TowerBreakers.Core
{
    /// <summary>
    /// [설명]: 물리 판정 관련 유틸리티 클래스입니다. GC 할당 방지를 위한 정적 필드를 제공합니다.
    /// </summary>
    public static class PhysicsQueryUtil
    {
        public static readonly Collider2D[] SharedBuffer = new Collider2D[32];
        public static readonly Collider2D[] SingleTargetBuffer = new Collider2D[1];

        private static ContactFilter2D s_enemyAndObjectFilter;
        private static ContactFilter2D s_enemyOnlyFilter;

        static PhysicsQueryUtil()
        {
            CreateFilters();
        }

        private static void CreateFilters()
        {
            s_enemyAndObjectFilter = new ContactFilter2D();
            s_enemyAndObjectFilter.SetLayerMask(LayerMask.GetMask("Enemy", "Object"));
            s_enemyAndObjectFilter.useLayerMask = true;
            s_enemyAndObjectFilter.useTriggers = true;

            s_enemyOnlyFilter = new ContactFilter2D();
            s_enemyOnlyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            s_enemyOnlyFilter.useLayerMask = true;
        }

        public static ContactFilter2D EnemyAndObjectFilter => s_enemyAndObjectFilter;
        public static ContactFilter2D EnemyOnlyFilter => s_enemyOnlyFilter;

        public static ContactFilter2D CreateFilter(params string[] layers)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask(layers));
            filter.useLayerMask = true;
            filter.useTriggers = true;
            return filter;
        }
    }
}
