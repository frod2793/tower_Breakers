using System;
using System.Collections.Generic;
using UnityEngine;
using TowerBreakers.Player.Model;
using TowerBreakers.Player.Service;
using EasyTransition;

namespace TowerBreakers.Core.Scene
{
    /// <summary>
    /// [기능]: 전투 결과 처리 및 씬 전환 서비스
    /// </summary>
    public class BattleResultService
    {
        private readonly UserSessionModel m_userSession;
        private readonly IEquipmentService m_equipmentService;
        private readonly SceneTransitionService m_transitionService;

        public event Action<SceneContextDTO> OnBattleCompleted;

        public BattleResultService(
            UserSessionModel userSession, 
            IEquipmentService equipmentService,
            SceneTransitionService transitionService)
        {
            m_userSession = userSession;
            m_equipmentService = equipmentService;
            m_transitionService = transitionService;
        }

        public void ProcessBattleResult(SceneContextDTO context, TransitionSettings transition)
        {
            if (context == null)
            {
                Debug.LogWarning("[BattleResultService] 컨텍스트가 null입니다.");
                return;
            }

            ProcessRewards(context);

            OnBattleCompleted?.Invoke(context);

            if (transition != null)
            {
                m_transitionService.LoadInGameWithRewards(transition, context);
            }
            else
            {
                m_transitionService.LoadLobby(null);
            }
        }

        public void ProcessBattleResult(SceneContextDTO context)
        {
            ProcessBattleResult(context, null);
        }

        private void ProcessRewards(SceneContextDTO context)
        {
            if (context.PlayerGold > 0)
            {
                m_userSession.Gold += context.PlayerGold;
                Debug.Log($"[BattleResultService] 골드 획득: +{context.PlayerGold}");
            }

            if (context.EarnedItemIds != null && context.EarnedItemIds.Count > 0)
            {
                foreach (var itemId in context.EarnedItemIds)
                {
                    m_userSession.AddItem(itemId);
                    Debug.Log($"[BattleResultService] 아이템 획득: {itemId}");
                }
            }
        }

        public SceneContextDTO CreateDefaultContext()
        {
            return SceneContextDTO.CreateDefault();
        }

        public SceneContextDTO CreateVictoryContext(int stage, int score, int gold, List<string> items)
        {
            return SceneContextDTO.CreateForResult(stage, true, score, gold, items);
        }

        public SceneContextDTO CreateDefeatContext(int stage, int score, int gold)
        {
            return SceneContextDTO.CreateForResult(stage, false, score, gold, null);
        }
    }
}
