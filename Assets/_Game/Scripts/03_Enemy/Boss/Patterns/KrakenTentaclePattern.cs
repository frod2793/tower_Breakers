using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.View;
using System.Threading;
using TowerBreakers.Core.Events;

namespace TowerBreakers.Enemy.Logic
{
    /// <summary>
    /// [설명]: 크라켄의 촉수 공격 패턴입니다. (소환 또는 소환물 활용)
    /// </summary>
    public class KrakenTentaclePattern : IBossPattern
    {
        #region 내부 필드
        private readonly IEventBus m_eventBus;
        private readonly KrakenBossState m_krakenState;
        private readonly bool m_isFalling;
        #endregion

        #region 공개 프로퍼티
        public string PatternName => m_isFalling ? "Falling Tentacle (Summon/Utilize)" : "Strike Tentacle (Summon/Utilize)";
        #endregion

        #region 초기화
        public KrakenTentaclePattern(IEventBus eventBus, KrakenBossState krakenState, bool isFalling)
        {
            m_eventBus = eventBus;
            m_krakenState = krakenState;
            m_isFalling = isFalling;
        }
        #endregion

        #region 비즈니스 로직
        public async UniTask ExecuteAsync(EnemyController controller, CancellationToken ct)
        {
            var view = controller.CachedView;
            if (view == null) return;

            // 공격 타겟 층은 플레이어가 있는 층으로 고정
            int targetFloor = m_krakenState != null ? m_krakenState.PlayerFloorIndex : Random.Range(0, 3);
            
            // 플레이어의 현재 월드 위치 정보를 가져옴 (X축 위치 동기화를 위함)
            Vector3 spawnPosition = Vector3.zero;
            var pushLogic = controller.CachedPushLogic;
            if (pushLogic != null && pushLogic.PlayerReceiver != null)
            {
                spawnPosition = pushLogic.PlayerReceiver.transform.position;
                Debug.Log($"[KRAKEN_DIAGNOSTIC] 플레이어 위치 감지 성공: {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"[KRAKEN_DIAGNOSTIC] 플레이어 PushReceiver를 찾을 수 없어 기본 위치(0,0,0)를 사용합니다.");
            }

            var summonType = m_isFalling ? OnKrakenSummonRequested.SummonType.FallingTentacle : OnKrakenSummonRequested.SummonType.StrikeTentacle;
            Debug.Log($"[KRAKEN_DIAGNOSTIC] 1. 패턴 실행 시작: {PatternName}, 타겟 층: {targetFloor}, 요청 위치: {spawnPosition}, 타입: {summonType}");
            
            // 보스는 명령/공격 애니메이션 수행 (Attack #3)
            view.PlayAnimation(global::PlayerState.ATTACK, 3);
            
            if (m_eventBus != null)
            {
                Debug.Log($"[KRAKEN_DIAGNOSTIC] 2. 이벤트 발행: OnKrakenSummonRequested (Type={summonType}, Floor={targetFloor})");
                // EnvironmentManager가 해당 층에서 공격 전용 프리팹을 소환하도록 이벤트 발행
                m_eventBus.Publish(new OnKrakenSummonRequested(summonType, targetFloor, spawnPosition));
            }
            else
            {
                Debug.LogError("[KRAKEN_DIAGNOSTIC] 이벤트 버스가 null입니다! 요청을 처리할 수 없습니다.");
            }

            await UniTask.Delay(1000, cancellationToken: ct);
            view.PlayAnimation(global::PlayerState.IDLE);
        }
        #endregion
    }
}
