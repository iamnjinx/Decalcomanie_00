using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageSelection : MonoBehaviour
{
    [SerializeField] StageSelectionUI stageSelectionUI;

    private int maxChapterIndex = 4; // Example maximum chapter index
    private int currentChapterIndex = 0;

    void Awake()
    {
        stageSelectionUI.LeftButton.OnSingleClick += () => MoveToPreviousChapter();
        stageSelectionUI.RightButton.OnSingleClick += () => MoveToNextChapter();
    }

    void Start()
    {
        UpdateChapterDisplay(currentChapterIndex);

        stageSelectionUI.SetStagePanel();
        for(int i = 0; i < stageSelectionUI.stagePanels.Count; i++)
        {
            int index = i; // Capture the current index for the lambda
            stageSelectionUI.stagePanels[i].button.OnSingleClick += () => OnStageSelected(index);
            stageSelectionUI.stagePanels[i].SetStagePanel(i);
        }
    }

    public void MoveToPreviousChapter()
    {
        if (currentChapterIndex > 0)
        {
            currentChapterIndex--;
            UpdateChapterDisplay(currentChapterIndex);
        }
    }

    public void MoveToNextChapter()
    {
        if (currentChapterIndex < maxChapterIndex)
        {
            currentChapterIndex++;
            UpdateChapterDisplay(currentChapterIndex);
        }
    }

    private void UpdateChapterDisplay(int chapterIndex)
    {
        stageSelectionUI.ChangeChapterDisplay(chapterIndex, maxChapterIndex);
    }

    public void OnStageSelected(int stageIndex)
    {
        Debug.Log($"Stage {stageIndex} selected!");
        // Implement stage loading logic here

        GameManager.Instance.LoadStage(stageIndex);
    }
}
