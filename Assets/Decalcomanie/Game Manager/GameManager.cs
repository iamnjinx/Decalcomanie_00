using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public GameData GameData;
    public StageAssetReader StageAssetReader;
    public DecalcomanieSceneManager SceneManager;

    private SceneType currentScene;
    public int CurrentStageIndex { get; private set; } = 40;
    [SerializeField] int totalStages = 40;
    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

    void Start()
    {
        currentScene = (SceneType)SceneManager.GetCurrentSceneIndex();
        LoadSettings();
    }

    public void SaveSettings()
    {
        SaveManager.Instance.Save(SettingsData.SaveKey, new SettingsData
        {
            language = CurrentLanguage,
            masterVolume = AudioManager.Instance.MasterVolume,
            bgmVolume = AudioManager.Instance.BgmVolume,
            sfxVolume = AudioManager.Instance.SfxVolume,
            isMuted = AudioManager.Instance.IsMuted
        });
    }

    public GameLanguage ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == GameLanguage.English ? GameLanguage.Korean : GameLanguage.English;
        SaveSettings();
        return CurrentLanguage;
    }

    public void SetLanguage(GameLanguage language)
    {
        CurrentLanguage = language;
        SaveSettings();
    }

    private void LoadSettings()
    {
        var settings = SaveManager.Instance.Load<SettingsData>(SettingsData.SaveKey, new SettingsData());
        CurrentLanguage = settings.language;
        AudioManager.Instance.SetMasterVolume(settings.masterVolume);
        AudioManager.Instance.SetBGMVolume(settings.bgmVolume);
        AudioManager.Instance.SetSFXVolume(settings.sfxVolume);
        AudioManager.Instance.SetMute(settings.isMuted);
    }

    public void LoadIntroScene()
    {
        SceneManager.MoveSceneTo("Intro Scene");
    }

    public void LoadOutroScene()
    {
        SceneManager.MoveSceneTo("Outro Scene");
    }

    public void LoadTitleScene()
    {
        if (currentScene == SceneType.Title) return;
        currentScene = SceneType.Title;
        SceneManager.MoveSceneTo(SceneNames.Title);
    }

    public void LoadSelectScene()
    {
        if (currentScene == SceneType.StageSelection) return;
        currentScene = SceneType.StageSelection;
        SceneManager.MoveSceneTo(SceneNames.StageSelection);
    }

    public void LoadTutorialScene()
    {
        // if (currentScene == SceneType.Tutorial) return;
        // currentScene = SceneType.Tutorial;
        // SceneManager.MoveSceneTo(SceneNames.Tutorial);
        LoadStage(0);
    }

    public void LoadStage(int stageIndex)
    {
        if (stageIndex >= totalStages)
        {
            LoadOutroScene();
            return;
        }

        CurrentStageIndex = stageIndex;
        TextAsset stageData = StageAssetReader.LoadStageAsset(stageIndex);
        if (stageData != null)
        {
            currentScene = SceneType.Game;
            SceneManager.MoveSceneTo(SceneNames.Game);
        }
        else
        {
            Debug.LogError($"Failed to load stage data for stage index: {stageIndex}");
        }
    }

    public TextAsset GetCurrentStageAsset()
    {
        return StageAssetReader.LoadStageAsset(CurrentStageIndex);
    }
}

public static class SceneNames
{
    public const string Title = "Title Scene";
    public const string StageSelection = "Stage Selection";
    public const string Game = "Game Scene";
    public const string Tutorial = "Tutorial Scene";
}

public enum SceneType
{
    Title, StageSelection, Game, Tutorial
}

public enum GameLanguage
{
    English, Korean
}
