using UnityEngine;
using Cysharp.Threading.Tasks;
using TowerBreakers.Enemy.Logic;
using TowerBreakers.Enemy.Data;
using System.Threading;

namespace TowerBreakers.DevTools
{
    /// <summary>
    /// [설명]: 인게임 보스 디버그 UI입니다. 게임 플레이 중 보스 상태를 확인하고 테스트할 수 있습니다.
    /// </summary>
    public class BossDebugUI : MonoBehaviour
    {
        #region 에디터 설정
        [Header("표시 설정")]
        [SerializeField] private bool m_showOnKeyPress = true;
        [SerializeField] private KeyCode m_toggleKey = KeyCode.F1;
        [SerializeField] private float m_updateInterval = 0.5f;
        #endregion

        #region 내부 필드
        private EnemyController m_currentBoss;
        private float m_lastUpdateTime;
        private bool m_isVisible;
        private Vector2 m_scrollPosition;
        #endregion

        #region 유니티 생명주기
        private void Update()
        {
            if (m_showOnKeyPress && UnityEngine.Input.GetKeyDown(m_toggleKey))
            {
                m_isVisible = !m_isVisible;
            }

            if (m_isVisible && Time.time - m_lastUpdateTime > m_updateInterval)
            {
                FindBoss();
                m_lastUpdateTime = Time.time;
            }
        }

        private void OnGUI()
        {
            if (!m_isVisible || !Application.isPlaying) return;

            DrawDebugUI();
        }
        #endregion

        #region 내부 로직
        private void FindBoss()
        {
            var bosses = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var boss in bosses)
            {
                var data = boss.Data;
                if (data != null && data.Type == EnemyType.Boss)
                {
                    m_currentBoss = boss;
                    break;
                }
            }
        }

        private void DrawDebugUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            GUILayout.Label("═══════════════════════════════════════════");
            GUILayout.Label("보스 디버그 UI", GetStyle(12, FontStyle.Bold));
            GUILayout.Label("═══════════════════════════════════════════");

            if (m_currentBoss == null)
            {
                GUILayout.Label("보스를 찾을 수 없습니다.");
                GUILayout.Label("(F1 키로 토글)");
                GUILayout.EndVertical();
                return;
            }

            var data = m_currentBoss.Data;
            string bossName = data?.EnemyName ?? "Unknown";
            
            GUILayout.Label($"보스: {bossName}");
            GUILayout.Label($"HP: {m_currentBoss.CurrentHp}/{data?.Hp ?? 0}");
            GUILayout.Label($"상태: {m_currentBoss.StateMachine?.CurrentState?.GetType().Name ?? "None"}");

            GUILayout.Label("─────────────────────────────────────────");

            DrawHpControls(data);

            GUILayout.Label("─────────────────────────────────────────");

            DrawSkillInfo();

            GUILayout.Label("─────────────────────────────────────────");
            GUILayout.Label("(F1 키로 숨기기)");

            GUILayout.EndVertical();
        }

        private void DrawHpControls(EnemyData data)
        {
            GUILayout.Label("HP 테스트", GetStyle(11, FontStyle.Bold));
            
            string bossName = data?.EnemyName ?? "";

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("HP -10%"))
            {
                int damage = data.Hp / 10;
                m_currentBoss.TakeDamage(damage);
            }
            
            if (GUILayout.Button("HP -25%"))
            {
                int damage = data.Hp / 4;
                m_currentBoss.TakeDamage(damage);
            }

            if (GUILayout.Button("HP -50%"))
            {
                int damage = data.Hp / 2;
                m_currentBoss.TakeDamage(damage);
            }
            
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (bossName.Contains("Kraken"))
            {
                if (GUILayout.Button("Phase1 (HP 100%)"))
                {
                    m_currentBoss.TakeDamage(m_currentBoss.CurrentHp - data.Hp);
                }

                if (GUILayout.Button("Phase2 전환"))
                {
                    int targetHp = data.Hp / 2;
                    int damage = m_currentBoss.CurrentHp - targetHp;
                    if (damage > 0)
                    {
                        m_currentBoss.TakeDamage(damage);
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSkillInfo()
        {
            var data = m_currentBoss.Data;
            string bossName = data?.EnemyName ?? "";

            if (bossName.Contains("Kraken"))
            {
                int currentHp = m_currentBoss.CurrentHp;
                int maxHp = data.Hp;
                bool isPhase2 = currentHp <= maxHp * 0.5f;

                GUILayout.Label("스킬 정보", GetStyle(11, FontStyle.Bold));
                GUILayout.Label($"현재 페이즈: {(isPhase2 ? "Phase 2" : "Phase 1")}");

                GUILayout.Label(isPhase2 ? "Phase 2 스킬:" : "Phase 1 스킬:");

                if (isPhase2)
                {
                    GUILayout.Label("  - StrikeTentacle (촉수 강타)");
                    GUILayout.Label("  - ArtilleryFire (포격)");
                    GUILayout.Label("  - FallingTentacle (촉수 낙하)");
                    GUILayout.Label("  - SummonTentacle (소환)");
                    GUILayout.Label("  - SummonSeaMonster (해저몬스터)");
                }
                else
                {
                    GUILayout.Label("  - FallingTentacle (촉수 낙하)");
                    GUILayout.Label("  - ArtilleryFire (포격)");
                }
            }
            else if (bossName.Contains("Goblin"))
            {
                GUILayout.Label("고블린族长 스킬:");
                GUILayout.Label("  - Jump (도약)");
                GUILayout.Label("  - Swing (휘두르기)");
                GUILayout.Label("  - SummonTotem (토템 소환)");
            }
            else if (bossName.Contains("Robot"))
            {
                GUILayout.Label("로봇 보스 스킬:");
                if (bossName.Contains("Sword"))
                    GUILayout.Label("  - Dash (돌격)");
                if (bossName.Contains("Gunner"))
                    GUILayout.Label("  - Shoot (발사)");
                if (bossName.Contains("Shield"))
                    GUILayout.Label("  - Shield (방어)");
            }
        }

        private GUIStyle GetStyle(int fontSize, FontStyle fontStyle)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            return style;
        }
        #endregion

        #region 외부 호출
        public void Show()
        {
            m_isVisible = true;
        }

        public void Hide()
        {
            m_isVisible = false;
        }

        public void Toggle()
        {
            m_isVisible = !m_isVisible;
        }
        #endregion
    }
}