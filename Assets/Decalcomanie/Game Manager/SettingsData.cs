[System.Serializable]
public class SettingsData
{
    public const string SaveKey = "settings";

    public GameLanguage language = GameLanguage.English;
    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
    public bool isMuted = false;
    public bool isFullscreen = true;
}
