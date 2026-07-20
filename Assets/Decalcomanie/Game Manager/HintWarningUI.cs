using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintWarningUI : BaseUI
{
    public enum HintUnlockTarget
    {
        Second,
        Last
    }

    [SerializeField] private ButtonUI yesButton;
    [SerializeField] private ButtonUI noButton;
    [SerializeField] private ButtonUI okButton;
    [SerializeField] private TextMeshProUGUI warningTMP;

    [SerializeField] private TextMeshProUGUI remainingStarText;

    [SerializeField] private HintManager hintManager;

    private HintUnlockTarget currentTarget = HintUnlockTarget.Last;

    protected override void Awake()
    {
        base.Awake();

        yesButton.OnSingleClick += OnYesClicked;
        noButton.OnSingleClick += OnNoClicked;
        okButton.OnSingleClick += OnOkClicked;
    }

    private void OnYesClicked()
    {
        if (currentTarget == HintUnlockTarget.Second)
            hintManager.UnlockSecondHint();
        else
            hintManager.UnlockLastHint();

        HideUI();
    }

    private void OnNoClicked()
    {
        HideUI();
    }

    private void OnOkClicked()
    {
        HideUI();
    }

    public void SetRemainingStarText(int remainingStarCount)
    {
        remainingStarText.text = remainingStarCount.ToString();
    }

    public void SetYesButtonInteractable(bool isInteractable)
    {
        yesButton.SetInteractable(isInteractable);
    }

    public void SetWarningText(string warningText)
    {
        if (warningTMP != null)
        {
            warningTMP.text = warningText;
        }
    }

    public void SetUnlockTarget(HintUnlockTarget target)
    {
        currentTarget = target;
    }

    public void ShowNormalMode()
    {
        yesButton.ShowUI();
        noButton.ShowUI();
        okButton.HideUI();
    }

    public void ShowWaitMode()
    {
        yesButton.HideUI();
        noButton.HideUI();
        okButton.ShowUI();
    }
}
