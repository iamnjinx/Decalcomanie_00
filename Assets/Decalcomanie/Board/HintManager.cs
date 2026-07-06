using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [SerializeField] private HintUI hintUI;
    [SerializeField] private HintData hintData;
    [SerializeField] private int lastHintCost = 1;

    private HintElement CurrentHintElement => hintData.hintElements[GameManager.Instance.CurrentStageIndex];

    private void Awake()
    {
        if (GameManager.Instance.CurrentStageIndex < 5)
            hintUI.gameObject.SetActive(false);

        hintUI.hintButtons[0].OnPressed += OnFirstHintPressed;
        hintUI.hintButtons[0].OnReleased += OnFirstHintReleased;

        hintUI.hintButtons[1].OnPressed += OnSecondHintPressed;
        hintUI.hintButtons[1].OnReleased += OnSecondHintReleased;

        hintUI.hintButtons[2].OnPressed += OnLastHintPressed;
        hintUI.hintButtons[2].OnReleased += OnLastHintReleased;
    }

    private void OnFirstHintPressed()
    {
        Debug.Log(GameManager.Instance.CurrentStageIndex);
        hintUI.ShowFirstHint(CurrentHintElement.Hint1Pos, CurrentHintElement.Hint1Num);
    }
    private void OnFirstHintReleased() => hintUI.HideFirstHint(CurrentHintElement.Hint1Pos);

    private void OnSecondHintPressed() => hintUI.ShowSecondHint(CurrentHintElement.Hint2Pos, CurrentHintElement.Hint2Sprite);
    private void OnSecondHintReleased() => hintUI.HideSecondHint(CurrentHintElement.Hint2Pos);

    private void OnLastHintPressed()
    {
        if (GameProgressData.Load().IsLastHintUnlocked(GameManager.Instance.CurrentStageIndex))
            hintUI.ShowLastHint(CurrentHintElement.Hint3Sprite);
        else
            ShowLastHintWarning();
    }

    private void OnLastHintReleased() => hintUI.HideLastHint();

    private void ShowLastHintWarning()
    {
        int remainingStarCount = GameProgressData.Load().remainingStarCount;
        hintUI.ShowLastHintWarning(remainingStarCount, remainingStarCount >= lastHintCost);
    }

    public void UnlockLastHint()
    {
        var progress = GameProgressData.Load();
        if (progress.remainingStarCount < lastHintCost) return;

        progress.remainingStarCount -= lastHintCost;
        progress.SetLastHintUnlocked(GameManager.Instance.CurrentStageIndex);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        hintUI.ShowLastHint(CurrentHintElement.Hint3Sprite);
    }
}
