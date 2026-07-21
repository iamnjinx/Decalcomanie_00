using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageSelection : MonoBehaviour
{
    [SerializeField] StageSelectionUI stageSelectionUI;

    private int maxChapterIndex;
    private int currentChapterIndex = 0;

    private const int StagesPerChapter = 10;
    private const string ChapterIndexKey = "LastChapterIndex";

    void Awake()
    {
        stageSelectionUI.Navigation.LeftButton.OnSingleClick += () => MoveToPreviousChapter();
        stageSelectionUI.Navigation.RightButton.OnSingleClick += () => MoveToNextChapter();
        stageSelectionUI.Navigation.BackButton.OnSingleClick += () => GameManager.Instance.LoadTitleScene();
        stageSelectionUI.OnChapterChanged += () => RefreshStagePanels();
        stageSelectionUI.OnBookmarkSelected += chapterIndex => MoveToChapter(chapterIndex);
    }

    void Start()
    {
        maxChapterIndex = (GameManager.Instance.TotalStages - 1) / StagesPerChapter;

        stageSelectionUI.Book.SetBookmarksActive(maxChapterIndex);

        currentChapterIndex = Mathf.Clamp(SaveManager.Instance.Load(ChapterIndexKey, 0), 0, maxChapterIndex);
        int startChapter = GameManager.Instance.CurrentStageIndex / StagesPerChapter;
        UpdateChapterDisplay(startChapter, true, true);

        stageSelectionUI.Demo.UpdateVisibility(currentChapterIndex, maxChapterIndex);

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
        var panels = stageSelectionUI.Book.StagePanels;
        bool firstHalfComplete = true;
        bool secondHalfComplete = true;

        for(int i = 0; i < panels.Length; i++)
        {
            int index = i + currentChapterIndex * StagesPerChapter; // Adjust index based on current chapter

            var achievement = progress.GetAchievement(index);
            var stageData = GameManager.Instance.GetStageData(index);
            panels[i].SetStagePanel(index, achievement.isCleared, achievement.obtainedStar, achievement.achievedMinMoves, progress.highestUnlockedStage >= index, stageData?.screenshot);

            panels[i].button.OnSingleClick = () => OnStageSelected(index);

            bool is3Star = achievement.isCleared && achievement.obtainedStar && achievement.achievedMinMoves;
            if (i < StagesPerChapter / 2) firstHalfComplete &= is3Star;
            else secondHalfComplete &= is3Star;
        }

        stageSelectionUI.Book.SetChapterStamps(firstHalfComplete, secondHalfComplete);

        bool awarded = false;
        if (firstHalfComplete) awarded |= progress.TryAwardChapterStamp(currentChapterIndex * 2);
        if (secondHalfComplete) awarded |= progress.TryAwardChapterStamp(currentChapterIndex * 2 + 1);

        if (awarded)
        {
            SaveManager.Instance.Save(GameProgressData.SaveKey, progress);
            stageSelectionUI.Book.RefreshStampCountText();
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

    public void MoveToChapter(int chapterIndex, bool is_instant = false)
    {
        if (chapterIndex == currentChapterIndex || chapterIndex < 0 || chapterIndex > maxChapterIndex) return;

        UpdateChapterDisplay(chapterIndex, chapterIndex > currentChapterIndex, is_instant);
    }

    private void UpdateChapterDisplay(int chapterIndex, bool is_right, bool is_instant = false)
    {
        if(stageSelectionUI.IsChangingChapter) return;
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
