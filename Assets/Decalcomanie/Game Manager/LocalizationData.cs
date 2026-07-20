using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocalizedData
{
    public GameLanguage language;

    [Header("Setting")]
    public string fullScreenText;
    public string windowedText;
    public string resolutionText;
    public string masterVolumeText;
    public string musicVolumeText;
    public string sfxVolumeText;
    public string backToStageText;
    public string titleText;
    public string resetSaveText;

    [Header("Stage UI - Objective Texts")]
    public string stageClearText;
    public string starEarnedText;
    public string starMovesFormat;

    [Header("Stage Cleared - Achievement")]
    public Sprite achievementSprite;

    [Header("Stage Cleared - After Button Texts")]
    public string nextStageText;
    public string stageSelectionText;
    public string restartStageText;

    [Header("Hint")]
    [TextArea]
    public string hintWarningText;
    [TextArea]
    public string hint3WaitText;

    [Header("Demo")]
    public Sprite demoSprite;

    [Header("Title - Reset Warning")]
    [TextArea]
    public string resetWarningText;
}

[CreateAssetMenu(fileName = "LocalizationData", menuName = "ScriptableObjects/LocalizationData", order = 2)]
public class LocalizationData : ScriptableObject
{
    public List<LocalizedData> localizedDataList;

    public LocalizedData GetData(GameLanguage language)
    {
        return localizedDataList.Find(data => data.language == language);
    }
}

public enum GameLanguage
{
    English, Korean, Japanese
}
