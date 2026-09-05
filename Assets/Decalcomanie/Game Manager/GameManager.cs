using TMPro;
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
    public LocalizationData LocalizationData;
    public StageAssetReader StageAssetReader;
    public DecalcomanieSceneManager SceneManager;

    [SerializeField] private bool isDemo;
    [SerializeField] private BuildVariantConfig fullConfig;
    [SerializeField] private BuildVariantConfig demoConfig;

    public bool IsDemo => isDemo;
    public BuildVariantConfig Config => isDemo ? demoConfig : fullConfig;
    public int TotalStages => Config.stages.Count;
    public HintData ActiveHintData => Config.hintData;

    public StageData GetStageData(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= Config.stages.Count) return null;
        return Config.stages[stageIndex];
    }

    public SceneType currentScene;
    public int CurrentStageIndex { get; private set; } = 19;
    public GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;
    public bool IsFullscreen { get; private set; } = true;

    public LocalizedData CurrentLocalizedData => LocalizationData.GetData(CurrentLanguage);

    [SerializeField] private TextMeshProUGUI verText;

    void Start()
    {
        verText.text = $"ver {Application.version}";

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
            isMuted = AudioManager.Instance.IsMuted,
            isFullscreen = IsFullscreen
        });
    }

    public GameLanguage ToggleLanguage()
    {
        var languages = (GameLanguage[])System.Enum.GetValues(typeof(GameLanguage));
        int nextIndex = (System.Array.IndexOf(languages, CurrentLanguage) + 1) % languages.Length;
        CurrentLanguage = languages[nextIndex];
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

        IsFullscreen = settings.isFullscreen;
        ApplyDisplayMode();
    }

    public void ToggleDisplayMode()
    {
        IsFullscreen = !IsFullscreen;
        ApplyDisplayMode();
        SaveSettings();
    }

    private void ApplyDisplayMode()
    {
        if (IsFullscreen)
        {
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        }
    }

    public void LoadIntroScene()
    {
        SceneManager.MoveSceneTo("Intro Scene");
    }

    public void LoadOutroScene()
    {
        SceneManager.MoveSceneTo("Outro Scene");
    }

    public void LoadCreditScene()
    {
        currentScene = SceneType.Credit;
        SceneManager.MoveSceneTo(SceneNames.Credit);
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
        else if(currentScene == SceneType.Title) CurrentStageIndex = GameProgressData.Load().highestUnlockedStage;
        currentScene = SceneType.StageSelection;
        SceneManager.MoveSceneTo(SceneNames.StageSelection);
    }

    public void LoadTutorialScene()
    {
        LoadStage(0);
    }

    public void LoadSkinScene()
    {
        if (currentScene == SceneType.Skin) return;
        currentScene = SceneType.Skin;
        SceneManager.MoveSceneTo(SceneNames.Skin);
    }

    public void LoadStage(int stageIndex)
    {
        if (stageIndex >= TotalStages)
        {
            CurrentStageIndex = TotalStages;
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
    public const string Credit = "Credit Scene";
    public const string Skin = "Skin Scene";
}

public enum SceneType
{
    Title, StageSelection, Game, Tutorial, Credit, Skin
}
