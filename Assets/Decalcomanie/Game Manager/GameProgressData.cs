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

        bool changed = data.MigrateEarlyStageAchievements();
        changed |= data.SyncBlueStarCount();
        if (changed)
            SaveManager.Instance.Save(SaveKey, data);

        return data;
    }

    // 초반 스테이지는 클리어만 하면 별/최소이동이 자동 달성되도록 규칙이 바뀌었습니다(StageManager.GameCleared 참고).
    // 그 이전에 만들어진 세이브에도 같은 보상을 소급 적용합니다.
    // 이미 달성 처리된 항목은 건드리지 않으므로 여러 번 호출해도 중복 지급되지 않습니다.
    // 실제로 바꾼 값이 있으면 true.
    private bool MigrateEarlyStageAchievements()
    {
        bool changed = false;

        for (int i = 0; i < stageAchievements.Count; i++)
        {
            if (!StageRules.IsEarlyStage(i)) continue;

            var data = stageAchievements[i];
            if (!data.isCleared) continue;

            if (!data.obtainedStar)
            {
                data.obtainedStar = true;
                changed = true;
            }

            if (!data.achievedMinMoves)
            {
                data.achievedMinMoves = true;
                changed = true;
            }

            if (!data.stampObtained)
            {
                data.stampObtained = true;
                remainingStampCount++;
                changed = true;
            }
        }

        return changed;
    }

    // 블루스타는 소모되지 않으므로 "별을 얻은 스테이지 수"와 항상 같아야 하는 파생값입니다.
    // blueStarCount 필드가 없던 시절의 세이브(0으로 로드됨)를 소급 복구하고,
    // 이후에도 카운터가 어긋나면 로드할 때마다 자동으로 맞춰집니다.
    // 실제로 바꾼 값이 있으면 true.
    private bool SyncBlueStarCount()
    {
        int actual = 0;
        for (int i = 0; i < stageAchievements.Count; i++)
        {
            if (stageAchievements[i].obtainedStar) actual++;
        }

        if (blueStarCount == actual) return false;

        blueStarCount = actual;
        return true;
    }

    public int highestUnlockedStage = 0;
    public int remainingStampCount = 0;
    public int blueStarCount = 0;
    public int skinID = -1;
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

        bool isFirstStarObtained = !data.obtainedStar && obtainedStar;
        if (isFirstStarObtained) blueStarCount++;

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
