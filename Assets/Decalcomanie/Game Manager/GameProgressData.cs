using System.Collections.Generic;

[System.Serializable]
public class GameProgressData
{
    public const string SaveKey = "game_progress";

    public static GameProgressData Load()
    {
        var data = SaveManager.Instance.Load<GameProgressData>(SaveKey, new());
        if (GameManager.Instance != null)
            data.highestUnlockedStage = System.Math.Min(data.highestUnlockedStage, GameManager.Instance.TotalStages);
        return data;
    }
    public int highestUnlockedStage = 0;
    public int remainingStarCount = 0;
    public List<StageAchievementData> stageAchievements = new List<StageAchievementData>();
    public List<bool> secondHintUnlockedStages = new List<bool>();
    public List<bool> lastHintUnlockedStages = new List<bool>();

    public StageAchievementData GetAchievement(int stageIndex)
    {
        if (stageIndex < stageAchievements.Count)
            return stageAchievements[stageIndex];
        return new StageAchievementData();
    }

    public bool IsStageUnlocked(int stageIndex) => stageIndex <= highestUnlockedStage;

    public bool IsStarObtained(int stageIndex) => GetAchievement(stageIndex).starObtained;

    public bool IsLastHintUnlocked(int stageIndex) =>
        stageIndex < lastHintUnlockedStages.Count && lastHintUnlockedStages[stageIndex];

    public bool IsSecondHintUnlocked(int stageIndex) =>
        stageIndex < secondHintUnlockedStages.Count && secondHintUnlockedStages[stageIndex];

    public void SetSecondHintUnlocked(int stageIndex)
    {
        while (secondHintUnlockedStages.Count <= stageIndex)
            secondHintUnlockedStages.Add(false);

        secondHintUnlockedStages[stageIndex] = true;
    }

    public void SetLastHintUnlocked(int stageIndex)
    {
        while (lastHintUnlockedStages.Count <= stageIndex)
            lastHintUnlockedStages.Add(false);

        lastHintUnlockedStages[stageIndex] = true;
    }

    public void RecordStageCleared(int stageIndex, bool obtainedStar, bool achievedMinMoves, bool starObtained)
    {
        while (stageAchievements.Count <= stageIndex)
            stageAchievements.Add(new StageAchievementData());

        var data = stageAchievements[stageIndex];
        data.isCleared = true;
        data.obtainedStar = data.obtainedStar || obtainedStar;
        data.achievedMinMoves = data.achievedMinMoves || achievedMinMoves;

        if (starObtained && !data.starObtained)
            remainingStarCount++;
        data.starObtained = data.starObtained || starObtained;

        if (stageIndex + 1 > highestUnlockedStage)
            highestUnlockedStage = System.Math.Min(stageIndex + 1, GameManager.Instance.TotalStages);
    }
}

[System.Serializable]
public class StageAchievementData
{
    public bool isCleared;
    public bool obtainedStar;
    public bool achievedMinMoves;
    public bool starObtained;
}
