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
    public List<StageAchievementData> stageAchievements = new List<StageAchievementData>();

    public StageAchievementData GetAchievement(int stageIndex)
    {
        if (stageIndex < stageAchievements.Count)
            return stageAchievements[stageIndex];
        return new StageAchievementData();
    }

    public bool IsStageUnlocked(int stageIndex) => stageIndex <= highestUnlockedStage;

    public void RecordStageCleared(int stageIndex, bool obtainedStar, bool achievedMinMoves)
    {
        while (stageAchievements.Count <= stageIndex)
            stageAchievements.Add(new StageAchievementData());

        var data = stageAchievements[stageIndex];
        data.isCleared = true;
        data.obtainedStar = data.obtainedStar || obtainedStar;
        data.achievedMinMoves = data.achievedMinMoves || achievedMinMoves;

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
}
