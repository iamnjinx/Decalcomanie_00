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
    public int remainingStampCount = 0;
    public List<StageAchievementData> stageAchievements = new List<StageAchievementData>();
    public List<bool> secondHintUnlockedStages = new List<bool>();
    public List<bool> chapterStampGroupAwarded = new List<bool>();

    public StageAchievementData GetAchievement(int stageIndex)
    {
        if (stageIndex < stageAchievements.Count)
            return stageAchievements[stageIndex];
        return new StageAchievementData();
    }

    public bool IsStageUnlocked(int stageIndex) => stageIndex <= highestUnlockedStage;

    public bool IsStampObtained(int stageIndex) => GetAchievement(stageIndex).stampObtained;

    public bool IsSecondHintUnlocked(int stageIndex) =>
        stageIndex < secondHintUnlockedStages.Count && secondHintUnlockedStages[stageIndex];

    public void SetSecondHintUnlocked(int stageIndex)
    {
        while (secondHintUnlockedStages.Count <= stageIndex)
            secondHintUnlockedStages.Add(false);

        secondHintUnlockedStages[stageIndex] = true;
    }

    public void RecordStageCleared(int stageIndex, bool obtainedStar, bool achievedMinMoves)
    {
        while (stageAchievements.Count <= stageIndex)
            stageAchievements.Add(new StageAchievementData());

        var data = stageAchievements[stageIndex];
        data.isCleared = true;
        data.obtainedStar = data.obtainedStar || obtainedStar;
        data.achievedMinMoves = data.achievedMinMoves || achievedMinMoves;

        bool stampObtained = data.isCleared && data.obtainedStar && data.achievedMinMoves;
        if (stampObtained && !data.stampObtained)
            remainingStampCount++;
        data.stampObtained = data.stampObtained || stampObtained;

        if (stageIndex + 1 > highestUnlockedStage)
            highestUnlockedStage = System.Math.Min(stageIndex + 1, GameManager.Instance.TotalStages);
    }

    // 챕터를 5스테이지 단위(앞/뒤)로 나눈 그룹에서 전부 별 3개를 달성하면 최초 1회만 보너스 도장을 지급합니다.
    public bool TryAwardChapterStamp(int groupIndex)
    {
        while (chapterStampGroupAwarded.Count <= groupIndex)
            chapterStampGroupAwarded.Add(false);

        if (chapterStampGroupAwarded[groupIndex]) return false;

        chapterStampGroupAwarded[groupIndex] = true;
        remainingStampCount++;
        return true;
    }
}

[System.Serializable]
public class StageAchievementData
{
    public bool isCleared;
    public bool obtainedStar;
    public bool achievedMinMoves;
    public bool stampObtained;
}
