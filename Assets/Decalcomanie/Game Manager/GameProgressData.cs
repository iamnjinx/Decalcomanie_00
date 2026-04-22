using System.Collections.Generic;

[System.Serializable]
public class GameProgressData
{
    public const string SaveKey = "game_progress";
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
            highestUnlockedStage = stageIndex + 1;
    }
}

[System.Serializable]
public class StageAchievementData
{
    public bool isCleared;
    public bool obtainedStar;
    public bool achievedMinMoves;
}
