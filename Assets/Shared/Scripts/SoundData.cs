using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Sound/SoundData")]
public class SoundData : ScriptableObject
{
    [Header("BGM")]
    [SerializeField] private BGMEntry[] m_bgmEntries;

    [Header("SE")]
    [SerializeField] private SEEntry[] m_seEntries;

    public BGMEntry[] BGMEntries => m_bgmEntries;
    public SEEntry[] SEEntries => m_seEntries;

    [System.Serializable]
    public class BGMEntry
    {
        public BGMType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [System.Serializable]
    public class SEEntry
    {
        public SEType type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }
}

public enum BGMType
{
    Title,
    Result,
    Battle,
    Lobby
}

public enum SEType
{
    ButtonClick,
    ButtonMove,
    Hit,
    Explosion,
    Dig,
    TreasureGet,
    Drumroll1,
    Drumroll2
}