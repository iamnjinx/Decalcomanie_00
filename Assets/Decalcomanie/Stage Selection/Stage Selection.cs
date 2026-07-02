using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageSelection : MonoBehaviour
{
    [SerializeField] StageSelectionUI stageSelectionUI;

    private int maxChapterIndex = 4; // Example maximum chapter index
    private int currentChapterIndex = 0;

    private const string ChapterIndexKey = "LastChapterIndex";

    private List<Sprite> stageScreenshots = new();

    void Awake()
    {
        stageSelectionUI.LeftButton.OnSingleClick += () => MoveToPreviousChapter();
        stageSelectionUI.RightButton.OnSingleClick += () => MoveToNextChapter();
        stageSelectionUI.OnChapterChanged += () => RefreshStagePanels();
    }

    void Start()
    {
        int j = 0;
        while (true)
        {
            var sprite = Resources.Load<Sprite>($"Stage SS/StageSS_{j}");
            if (sprite == null) break;
            stageScreenshots.Add(sprite);
            j++;
        }

        currentChapterIndex = Mathf.Clamp(SaveManager.Instance.Load(ChapterIndexKey, 0), 0, maxChapterIndex);
        int d = GameManager.Instance.CurrentStageIndex / 10;
        UpdateChapterDisplay(d, true, true);

        stageSelectionUI.SetStagePanel();

        StartCoroutine(InitializePanels());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            MoveToPreviousChapter();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            MoveToNextChapter();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.LoadTitleScene();
        }
    }

    private IEnumerator InitializePanels()
    {
        yield return null; // wait for all Start() calls to finish

        RefreshStagePanels();
    }

    public void RefreshStagePanels()
    {
        var progress = GameProgressData.Load();
        for(int i = 0; i < stageSelectionUI.stagePanels.Length; i++)
        {
            int index = i + currentChapterIndex * 10; // Adjust index based on current chapter

            var achievement = progress.GetAchievement(index);
            var ss = index < stageScreenshots.Count ? stageScreenshots[index] : null;
            Debug.Log($"Highest Unlocked Stage: {progress.highestUnlockedStage}, Current Index: {index}");
            stageSelectionUI.stagePanels[i].SetStagePanel(index, achievement.isCleared, achievement.obtainedStar, achievement.achievedMinMoves, progress.highestUnlockedStage >= index, ss);

            stageSelectionUI.stagePanels[i].button.OnSingleClick = () => OnStageSelected(index);
        }
    }

    public void MoveToPreviousChapter(bool is_instant = false)
    {
        if (currentChapterIndex > 0)
        {
            UpdateChapterDisplay(currentChapterIndex-1, false, is_instant);
        }
    }

    public void MoveToNextChapter(bool is_instant = false)
    {
        if (currentChapterIndex < maxChapterIndex)
        {
            UpdateChapterDisplay(currentChapterIndex+1, true, is_instant);
        }
    }

    private void UpdateChapterDisplay(int chapterIndex, bool is_right, bool is_instant = false)
    {
        if(stageSelectionUI.is_changingChapter) return;
        currentChapterIndex = chapterIndex;
        SaveManager.Instance.Save(ChapterIndexKey, currentChapterIndex);
        stageSelectionUI.ChangeChapterDisplay(chapterIndex, maxChapterIndex, is_right, is_instant);
    }

    public void OnStageSelected(int stageIndex)
    {
        var progress = GameProgressData.Load();
        if (!progress.IsStageUnlocked(stageIndex)) return;

        GameManager.Instance.LoadStage(stageIndex);
    }
}
