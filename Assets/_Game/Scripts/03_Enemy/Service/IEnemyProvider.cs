using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Tower.Service
{
    /// <summary>
    /// [설명]: 활성화된 적 오브젝트들에 대한 접근을 제공하는 인터페이스입니다.
    /// </summary>
    public interface IEnemyProvider
    {
        IReadOnlyList<GameObject> NormalEnemies { get; }
        IReadOnlyList<GameObject> EliteEnemies { get; }
        IReadOnlyList<GameObject> BossEnemies { get; }
    }
}
