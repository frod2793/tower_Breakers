using UnityEngine;
using System.Text;

namespace TowerBreakers.Core.Performance
{
    /// <summary>
    /// [설명]: 게임의 실시간 프레임을 모니터링하고 프레임 드랍(Spike) 발생 시 로그를 남기는 클래스입니다.
    /// </summary>
    public class FrameDropMonitor : MonoBehaviour
    {
        #region 에디터 설정
        [SerializeField, Tooltip("프레임 드랍으로 간주할 최소 프레임 타임 (ms). 50.0ms = 20FPS 미만")]
        private float m_spikeThresholdMs = 50.0f;

        [SerializeField, Tooltip("성능 로그 출력 주기 (초)")]
        private float m_reportInterval = 5.0f;

        [SerializeField, Tooltip("상세 로그 출력 여부")]
        private bool m_showDetailedLog = false;
        #endregion

        #region 내부 변수
        private float m_accumulatedDeltaTime = 0f;
        private int m_frameCount = 0;
        private float m_maxFrameTime = 0f;
        private float m_minFrameTime = float.MaxValue;
        private float m_lastReportTime = 0f;

        // FPS 계산용 (1초 주기)
        private float m_fpsTimer = 0f;
        private int m_fpsCount = 0;
        private float m_currentFps = 0f;

        private readonly StringBuilder m_logBuilder = new StringBuilder();
        // 로그 활성화 플래그(런타임에서 PlayerDebugger를 통해 제어 가능)
        public static bool s_loggingEnabled = true;
        /// <summary>
        /// [설명]: 프레임 드랍 로깅 활성화 여부를 설정합니다.
        /// </summary>
        public static void SetLoggingEnabled(bool enabled)
        {
            s_loggingEnabled = enabled;
        }
        #endregion

        #region 유니티 생명주기
        private void Awake()
        {
            // 중복 생성 방지 및 싱글톤 아님 (필요 시 하나만 배치 권장)
            m_lastReportTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            float deltaTimeMs = deltaTime * 1000f;

            // 1. 프레임 스파이크 감지
            if (deltaTimeMs > m_spikeThresholdMs)
            {
                LogFrameSpike(deltaTimeMs);
            }

            // 2. 데이터 누적
            m_accumulatedDeltaTime += deltaTime;
            m_frameCount++;
            m_maxFrameTime = Mathf.Max(m_maxFrameTime, deltaTimeMs);
            m_minFrameTime = Mathf.Min(m_minFrameTime, deltaTimeMs);

            // 3. 실시간 FPS 계산
            m_fpsTimer += deltaTime;
            m_fpsCount++;
            if (m_fpsTimer >= 1.0f)
            {
                m_currentFps = m_fpsCount / m_fpsTimer;
                m_fpsTimer = 0f;
                m_fpsCount = 0;
            }

            // 4. 주기적 성능 보고
            if (Time.realtimeSinceStartup - m_lastReportTime >= m_reportInterval)
            {
                ReportPerformance();
                m_lastReportTime = Time.realtimeSinceStartup;
            }
        }
        #endregion

        #region 내부 로직
        /// <summary>
        /// [설명]: 프레임 스파이크 발생 시 경고 로그를 남깁니다.
        /// </summary>
        private void LogFrameSpike(float spikeTimeMs)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!s_loggingEnabled) return;
            Debug.LogWarning($"<color=yellow>[FrameDropMonitor]</color> <color=red>Spike Detected!</color> " +
                             $"\n- Frame Time: {spikeTimeMs:F2}ms" +
                             $"\n- Target: {m_spikeThresholdMs:F2}ms");
            #endif
        }

        /// <summary>
        /// [설명]: 누적된 성능 데이터를 분석하여 보고합니다.
        /// </summary>
        private void ReportPerformance()
        {
            if (m_frameCount == 0) return;

            float avgFrameTime = (m_accumulatedDeltaTime / m_frameCount) * 1000f;
            float avgFps = m_frameCount / (Time.realtimeSinceStartup - (m_lastReportTime - m_reportInterval));

            m_logBuilder.Clear();
            m_logBuilder.AppendLine("<color=cyan>[FrameDropMonitor Performance Report]</color>");
            m_logBuilder.AppendLine($"- Avg FPS: {m_currentFps:F1}");
            m_logBuilder.AppendLine($"- Avg Frame Time: {avgFrameTime:F2}ms");
            m_logBuilder.AppendLine($"- Max Frame Time: {m_maxFrameTime:F2}ms");
            m_logBuilder.AppendLine($"- Min Frame Time: {m_minFrameTime:F2}ms");

            // [정리]: 상세 로그가 비활성화된 경우 로그를 출력하지 않습니다.
            if (m_showDetailedLog)
            {
                Debug.Log(m_logBuilder.ToString());
            }

            // 초기화
            m_accumulatedDeltaTime = 0f;
            m_frameCount = 0;
            m_maxFrameTime = 0f;
            m_minFrameTime = float.MaxValue;
        }
        #endregion
    }
}
