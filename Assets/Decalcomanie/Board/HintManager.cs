using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [SerializeField] private HintUI hintUI;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private int hintCost = 1;

    private HintElement CurrentHintElement => GameManager.Instance.ActiveHintData.hintElements[GameManager.Instance.CurrentStageIndex];

    private void Awake()
    {
        //Debug.Log($"Current Stage Index: {GameManager.Instance.CurrentStageIndex}, {GameManager.Instance.CurrentStageIndex < 5}");
        if (GameManager.Instance.CurrentStageIndex < 5)
            hintUI.gameObject.SetActive(false);

        if (stageManager != null)
            stageManager.OnGameStateChanged += OnGameStateChanged;

        if (GameProgressData.Load().IsSecondHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.hintButton.UnlockHintButton();

        hintUI.hintButton.OnPressed += OnHintPressed;
        hintUI.hintButton.OnReleased += OnHintReleased;
    }

    void Start()
    {
        hintUI.SetRemainingStampText(GameProgressData.Load().remainingStampCount);
    }

    private void OnHintPressed()
    {
        if (GameProgressData.Load().IsSecondHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.ShowSecondHint(CurrentHintElement.Hint2Pos, CurrentHintElement.Hint2Sprite);
        else
            ShowHintUnlockWarning();
    }

    private void OnHintReleased()
    {
        if (GameProgressData.Load().IsSecondHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.HideSecondHint(CurrentHintElement.Hint2Pos);
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (GameManager.Instance.CurrentStageIndex < 5) return;
        hintUI.gameObject.SetActive(newState == GameState.Paint);
    }

    private void ShowHintUnlockWarning()
    {
        int remainingStampCount = GameProgressData.Load().remainingStampCount;
        hintUI.ShowHintWarning(remainingStampCount, remainingStampCount >= hintCost, GameManager.Instance.CurrentLocalizedData.hintWarningText);
    }

    public void UnlockHint()
    {
        var progress = GameProgressData.Load();
        if (progress.remainingStampCount < hintCost) return;

        progress.remainingStampCount -= hintCost;
        progress.SetSecondHintUnlocked(GameManager.Instance.CurrentStageIndex);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        hintUI.hintButton.UnlockHintButton();

        hintUI.SetRemainingStampText(GameProgressData.Load().remainingStampCount);
    }
}
