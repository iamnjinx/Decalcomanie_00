using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageSelection : MonoBehaviour
{
    [SerializeField] StageSelectionUI stageSelectionUI;

    private int maxChapterIndex = 3; // Example maximum chapter index
    private int currentChapterIndex = 0;

    private const string ChapterIndexKey = "LastChapterIndex";

    void Awake()
    {
        stageSelectionUI.LeftButton.OnSingleClick += () => MoveToPreviousChapter();
        stageSelectionUI.RightButton.OnSingleClick += () => MoveToNextChapter();
    }

    void Start()
    {
        currentChapterIndex = Mathf.Clamp(SaveManager.Instance.Load(ChapterIndexKey, 0), 0, maxChapterIndex);
        UpdateChapterDisplay(currentChapterIndex, true);

        stageSelectionUI.SetStagePanel();
        for(int i = 0; i < stageSelectionUI.stagePanels.Count; i++)
        {
            int index = i;
            stageSelectionUI.stagePanels[i].button.OnSingleClick += () => OnStageSelected(index);
        }

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

        var progress = SaveManager.Instance.Load<GameProgressData>(GameProgressData.SaveKey, new());
        for(int i = 0; i < stageSelectionUI.stagePanels.Count; i++)
        {
            var achievement = progress.GetAchievement(i);
            stageSelectionUI.stagePanels[i].SetStagePanel(i, achievement.isCleared, achievement.obtainedStar, achievement.achievedMinMoves);
        }
    }

    public void MoveToPreviousChapter(bool is_instant = false)
    {
        if (currentChapterIndex > 0)
        {
            UpdateChapterDisplay(currentChapterIndex-1, is_instant);
        }
    }

    public void MoveToNextChapter(bool is_instant = false)
    {
        if (currentChapterIndex < maxChapterIndex)
        {
            UpdateChapterDisplay(currentChapterIndex+1, is_instant);
        }
    }

    private void UpdateChapterDisplay(int chapterIndex, bool is_instant = false)
    {
        if(stageSelectionUI.is_changingChapter) return;
        currentChapterIndex = chapterIndex;
        SaveManager.Instance.Save(ChapterIndexKey, currentChapterIndex);
        stageSelectionUI.ChangeChapterDisplay(chapterIndex, maxChapterIndex, is_instant);
    }

    public void OnStageSelected(int stageIndex)
    {
        var progress = SaveManager.Instance.Load<GameProgressData>(GameProgressData.SaveKey, new());
        if (!progress.IsStageUnlocked(stageIndex)) return;

        GameManager.Instance.LoadStage(stageIndex);
    }
}
