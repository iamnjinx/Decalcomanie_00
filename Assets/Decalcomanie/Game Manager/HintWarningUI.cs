using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HintWarningUI : BaseUI
{
    [SerializeField] private ButtonUI yesButton;
    [SerializeField] private TextMeshProUGUI yesButtonText;
    [SerializeField] private ButtonUI noButton;
    [SerializeField] private TextMeshProUGUI noButtonText;
    [SerializeField] private TextMeshProUGUI warningTMP;

    [FormerlySerializedAs("remainingStarText")]
    [SerializeField] private GameObject remainingStampObj;
    [SerializeField] private TextMeshProUGUI remainingStampText;

    [SerializeField] private HintManager hintManager;

    protected override void Awake()
    {
        base.Awake();

        yesButton.OnSingleClick += OnYesClicked;
        noButton.OnSingleClick += OnNoClicked;

        yesButtonText.text = GameManager.Instance.CurrentLocalizedData.yesText;
        yesButtonText.font = GameManager.Instance.CurrentLocalizedData.fontAsset;
        noButtonText.text = GameManager.Instance.CurrentLocalizedData.noText;
        noButtonText.font = GameManager.Instance.CurrentLocalizedData.fontAsset;
    }

    private void OnYesClicked()
    {
        hintManager.UnlockHint();
        HideUI();
    }

    private void OnNoClicked()
    {
        HideUI();
    }

    public void SetRemainingStampText(int remainingStampCount)
    {
        remainingStampText.text = "= " + remainingStampCount.ToString();
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
            warningTMP.font = GameManager.Instance.CurrentLocalizedData.fontAsset;
        }
    }

    public void ShowNormalMode()
    {
        yesButton.ShowUI();
        noButton.ShowUI();

        remainingStampObj.SetActive(true);
    }
}
