using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class LocalizedData
{
    public GameLanguage language;
    public TMP_FontAsset fontAsset;

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
    public string stageText;

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

    [Header("Stage UI - Undo")]
    public string undoText;

    [Header("Stage UI - Flip")]
    public string flipHorizontalText;
    public string flipVerticalText;

    [Header("Hint")]
    [TextArea]
    public string hintWarningText;
    [TextArea]
    public string hint3WaitText;

    public string yesText;
    public string noText;

    public string hint1Text;

    [Header("Demo")]
    public Sprite demoSprite;

    [Header("Title - Reset Warning")]
    [TextArea]
    public string resetWarningText;

    [Header("Skin Shop")]
    // DinoSkinSO.dinoSkins와 같은 순서로 스킨 이름을 채워줍니다.
    public string[] skinNames;

    public string GetSkinName(int skinID)
    {
        if (skinNames == null || skinID < 0 || skinID >= skinNames.Length) return string.Empty;
        return skinNames[skinID];
    }
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
