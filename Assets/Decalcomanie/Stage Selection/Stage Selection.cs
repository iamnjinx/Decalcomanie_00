using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageSelection : MonoBehaviour
{
    [SerializeField] StageSelectionUI stageSelectionUI;

    private int maxChapterIndex;
    private int currentChapterIndex = 0;

    private const string ChapterIndexKey = "LastChapterIndex";

    void Awake()
    {
        stageSelectionUI.LeftButton.OnSingleClick += () => MoveToPreviousChapter();
        stageSelectionUI.RightButton.OnSingleClick += () => MoveToNextChapter();
        stageSelectionUI.OnChapterChanged += () => RefreshStagePanels();
    }

    void Start()
    {
        maxChapterIndex = (GameManager.Instance.TotalStages - 1) / 10;

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
            var stageData = GameManager.Instance.GetStageData(index);
            stageSelectionUI.stagePanels[i].SetStagePanel(index, achievement.isCleared, achievement.obtainedStar, achievement.achievedMinMoves, progress.highestUnlockedStage >= index, stageData?.screenshot);

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
        currentChapterIndex = Mathf.Clamp(chapterIndex, 0, maxChapterIndex);
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
