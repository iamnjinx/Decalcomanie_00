using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocalizedData
{
    public GameLanguage language;

    [Header("Stage UI - Short Key")]
    public Sprite paintShortKeySprite;
    public Sprite platformerShortKeySprite;

    [Header("Stage UI - Objective Texts")]
    public string stageClearText;
    public string starEarnedText;
    public string starMovesFormat;

    [Header("Stage Cleared - Achievement")]
    public Sprite achievementSprite;

    [Header("Hint")]
    public Sprite hintWarningSprite;
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
