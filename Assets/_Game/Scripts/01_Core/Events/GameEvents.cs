using System;
using UnityEngine;

namespace TowerBreakers.Core.Events
{
    /// <summary>
    /// [설명]: 게임 오버 이벤트입니다.
    /// </summary>
    public struct OnGameOver { }

    /// <summary>
    /// [설명]: 게임 시작 이벤트입니다.
    /// </summary>
    public struct OnGameStart
    {
        public int TowerId;

        public OnGameStart(int towerId)
        {
            TowerId = towerId;
        }
    }

    /// <summary>
    /// [설명]: 게임 일시정지 이벤트입니다.
    /// </summary>
    public struct OnGamePause { }

    /// <summary>
    /// [설명]: 게임 재개 이벤트입니다.
    /// </summary>
    public struct OnGameResume { }
}
