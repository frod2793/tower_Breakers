using TowerBreakers.Core.Events;
using TowerBreakers.Sound.Data;
using TowerBreakers.Sound.View;

namespace TowerBreakers.Sound.Logic
{
    /// <summary>
    /// [기능]: 사운드 이벤트 구독 및 재생 중재자
    /// [작성자]: Claude
    /// </summary>
    public class SoundPresenter : System.IDisposable
    {
        private readonly IEventBus m_eventBus;
        private readonly SoundDatabase m_database;
        private readonly SoundPlayer m_player;

        public SoundPresenter(IEventBus eventBus, SoundDatabase database, SoundPlayer player)
        {
            m_eventBus = eventBus;
            m_database = database;
            m_player = player;

            m_eventBus.Subscribe<OnSoundRequested>(OnSoundRequested);
            m_eventBus.Subscribe<OnBGMRequested>(OnBGMRequested);
            m_eventBus.Subscribe<OnBGMStopRequested>(OnBGMStopRequested);
        }

        private void OnSoundRequested(OnSoundRequested evt)
        {
            if (m_database == null)
            {
                global::UnityEngine.Debug.LogError("[SoundPresenter] SoundDatabase가 null입니다. 의존성 확인이 필요합니다.");
                return;
            }

            if (!m_database.TryGetEntry(evt.SoundKey, out var entry))
            {
                global::UnityEngine.Debug.LogWarning($"[SoundPresenter] 등록되지 않은 사운드 키: {evt.SoundKey}");
                return;
            }

            if (entry.Clip == null)
            {
                global::UnityEngine.Debug.LogWarning($"[SoundPresenter] 사운드 키 {evt.SoundKey}에 클립이 할당되지 않았습니다.");
                return;
            }

            float finalVolume = entry.DefaultVolume * evt.Volume;
            m_player.PlaySFX(entry.Clip, finalVolume, evt.Pitch);
        }

        private void OnBGMRequested(OnBGMRequested evt)
        {
            if (!m_database.TryGetEntry(evt.SoundKey, out var entry))
            {
                global::UnityEngine.Debug.LogWarning($"[SoundPresenter] 등록되지 않은 BGM 키: {evt.SoundKey}");
                return;
            }

            if (entry.Clip == null)
            {
                global::UnityEngine.Debug.LogWarning($"[SoundPresenter] BGM 키 {evt.SoundKey}에 클립이 할당되지 않았습니다.");
                return;
            }

            m_player.PlayBGM(entry.Clip, entry.DefaultVolume, evt.FadeInDuration);
        }

        private void OnBGMStopRequested(OnBGMStopRequested evt)
        {
            m_player.StopBGM(evt.FadeOutDuration);
        }

        public void Dispose()
        {
            m_eventBus.Unsubscribe<OnSoundRequested>(OnSoundRequested);
            m_eventBus.Unsubscribe<OnBGMRequested>(OnBGMRequested);
            m_eventBus.Unsubscribe<OnBGMStopRequested>(OnBGMStopRequested);
        }
    }
}
