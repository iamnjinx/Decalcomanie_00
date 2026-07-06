using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;

public class HintWarningUI : BaseUI
{
    [SerializeField] private ButtonUI yesButton;
    [SerializeField] private ButtonUI noButton;

    [SerializeField] private TextMeshProUGUI remainingStarText;

    [SerializeField] private HintManager hintManager;

    protected override void Awake()
    {
        base.Awake();

        yesButton.OnSingleClick += OnYesClicked;
        noButton.OnSingleClick += OnNoClicked;
    }

    private void OnYesClicked()
    {
        hintManager.UnlockLastHint();
        HideUI();
    }

    private void OnNoClicked()
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
}
