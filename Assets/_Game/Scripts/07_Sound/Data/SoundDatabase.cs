using System.Collections.Generic;
using UnityEngine;

namespace TowerBreakers.Sound.Data
{
    /// <summary>
    /// [기능]: 사운드 키와 AudioClip 매핑 데이터베이스
    /// [작성자]: Claude
    /// </summary>
    [CreateAssetMenu(menuName = "TowerBreakers/Sound/SoundDatabase")]
    public class SoundDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct SoundEntry
        {
            [Tooltip("사운드 식별 키")]
            public string Key;
            [Tooltip("재생할 오디오 클립")]
            public AudioClip Clip;
            [Tooltip("기본 볼륨 (0~1)")]
            [Range(0f, 1f)] public float DefaultVolume;
        }

        [SerializeField, Tooltip("등록된 사운드 목록")]
        private SoundEntry[] m_entries;

        [System.NonSerialized]
        private Dictionary<string, SoundEntry> m_lookup;
        [System.NonSerialized]
        private bool m_isInitialized;

        private void Awake()
        {
            BuildLookup();
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            // 도메인 리로드 등으로 인해 m_lookup이 null이 될 수 있으므로 이중 체크
            if (m_isInitialized && m_lookup != null)
                return;

            m_lookup = new Dictionary<string, SoundEntry>();

            if (m_entries == null)
            {
                m_isInitialized = true;
                return;
            }

            for (int i = 0; i < m_entries.Length; i++)
            {
                var entry = m_entries[i];
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    m_lookup[entry.Key] = entry;
                }
            }

            m_isInitialized = true;
        }

        public bool TryGetEntry(string key, out SoundEntry entry)
        {
            if (!m_isInitialized || m_lookup == null)
                BuildLookup();

            if (m_lookup == null)
            {
                entry = default;
                return false;
            }

            return m_lookup.TryGetValue(key, out entry);
        }

        public SoundEntry[] Entries => m_entries;
    }
}
