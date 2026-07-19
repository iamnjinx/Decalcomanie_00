using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [SerializeField] private HintUI hintUI;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private int lastHintCost = 1;

    private HintElement CurrentHintElement => GameManager.Instance.ActiveHintData.hintElements[GameManager.Instance.CurrentStageIndex];

    private void Awake()
    {
        Debug.Log($"Current Stage Index: {GameManager.Instance.CurrentStageIndex}, {GameManager.Instance.CurrentStageIndex < 5}");
        if (GameManager.Instance.CurrentStageIndex < 5)
            hintUI.gameObject.SetActive(false);

        if (stageManager != null)
            stageManager.OnGameStateChanged += OnGameStateChanged;

        if (GameProgressData.Load().IsSecondHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.hintButtons[1].UnlockHintButton();

        if (GameProgressData.Load().IsLastHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.hintButtons[2].UnlockHintButton();

        hintUI.hintButtons[0].OnPressed += OnFirstHintPressed;
        hintUI.hintButtons[0].OnReleased += OnFirstHintReleased;

        hintUI.hintButtons[1].OnPressed += OnSecondHintPressed;
        hintUI.hintButtons[1].OnReleased += OnSecondHintReleased;

        hintUI.hintButtons[2].OnPressed += OnLastHintPressed;
        hintUI.hintButtons[2].OnReleased += OnLastHintReleased;
    }

    void Start()
    {
        hintUI.SetRemainingStarText(GameProgressData.Load().remainingStarCount);       
    }

    private void OnFirstHintPressed()
    {
        hintUI.ShowFirstHint(CurrentHintElement.Hint1Pos, CurrentHintElement.Hint1Num);
    }
    private void OnFirstHintReleased() => hintUI.HideFirstHint(CurrentHintElement.Hint1Pos);

    private void OnSecondHintPressed()
    {
        if (GameProgressData.Load().IsSecondHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.ShowSecondHint(CurrentHintElement.Hint2Pos, CurrentHintElement.Hint2Sprite);
        else
        {
            ShowHintUnlockWarning(HintWarningUI.HintUnlockTarget.Second);
        }
    }

    private void OnSecondHintReleased()
    {
        if (GameProgressData.Load().IsSecondHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.HideSecondHint(CurrentHintElement.Hint2Pos);
    }

    private void OnLastHintPressed()
    {
        if (GameProgressData.Load().IsLastHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.ShowLastHint(CurrentHintElement.Hint3Sprite);
        else
            ShowHintUnlockWarning(HintWarningUI.HintUnlockTarget.Last);
    }

    private void OnLastHintReleased() => hintUI.HideLastHint();

    private void OnGameStateChanged(GameState newState)
    {
        if (GameManager.Instance.CurrentStageIndex < 5) return;
        hintUI.gameObject.SetActive(newState == GameState.Paint);
    }

    private void ShowHintUnlockWarning(HintWarningUI.HintUnlockTarget unlockTarget)
    {
        int remainingStarCount = GameProgressData.Load().remainingStarCount;
        hintUI.ShowHintWarning(unlockTarget, remainingStarCount, remainingStarCount >= lastHintCost, string.Empty);
    }

    public void UnlockLastHint()
    {
        var progress = GameProgressData.Load();
        if (progress.remainingStarCount < lastHintCost) return;

        progress.remainingStarCount -= lastHintCost;
        progress.SetLastHintUnlocked(GameManager.Instance.CurrentStageIndex);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        hintUI.hintButtons[2].UnlockHintButton();

        hintUI.SetRemainingStarText(GameProgressData.Load().remainingStarCount);
    }

    public void UnlockSecondHint()
    {
        var progress = GameProgressData.Load();
        if (progress.remainingStarCount < lastHintCost) return;

        progress.remainingStarCount -= lastHintCost;
        progress.SetSecondHintUnlocked(GameManager.Instance.CurrentStageIndex);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        hintUI.hintButtons[1].UnlockHintButton();

        hintUI.SetRemainingStarText(GameProgressData.Load().remainingStarCount);
    }
}
