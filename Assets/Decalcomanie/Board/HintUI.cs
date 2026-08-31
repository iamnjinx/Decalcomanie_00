using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintUI : MonoBehaviour
{
    public HintButton hintButton;

    [Header("Hint Space 2")]
    [SerializeField] private HintSpace2[] hint2Images = new HintSpace2[2];

    [SerializeField] private TextMeshProUGUI remainingStampText;

    [SerializeField] private HintWarningUI lastHintWarningUI;

    public void ShowSecondHint(HintPos hintPos, Sprite hintSprite)
    {
        hint2Images[(int)hintPos].SetHintImage(hintSprite);
        hint2Images[(int)hintPos].ShowUI();
    }

    public void HideSecondHint(HintPos hintPos)
    {
        hint2Images[(int)hintPos].HideUI();
    }

    public void SetRemainingStampText(int remainingStampCount)
    {
        if (remainingStampText != null) remainingStampText.text = remainingStampCount.ToString();
    }

    public void ShowHintWarning(int remainingStampCount, bool canAffordHint, string warningText)
    {
        if (lastHintWarningUI == null) return;

        lastHintWarningUI.SetRemainingStampText(remainingStampCount);
        lastHintWarningUI.SetYesButtonInteractable(canAffordHint);
        lastHintWarningUI.SetWarningText(warningText);
        lastHintWarningUI.ShowNormalMode();
        lastHintWarningUI.ShowUI();
    }
}
