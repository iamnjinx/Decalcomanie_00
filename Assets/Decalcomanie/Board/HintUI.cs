using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintUI : MonoBehaviour
{
    public HintButton[] hintButtons = new HintButton[3];

    [Header("Hint Space 1")]
    [SerializeField] private HintSpace1[] hint1Images = new HintSpace1[2];
    [Header("Hint Space 2")]
    [SerializeField] private HintSpace2[] hint2Images = new HintSpace2[2];
    [Header("Hint Space 3")]
    [SerializeField] private HintSpace3 hint3Image;
    
    //[SerializeField] private TextMeshProUGUI remainingStarText;

    [SerializeField] private HintWarningUI lastHintWarningUI;

    public void ShowFirstHint(HintPos hintPos, int hintNum)
    {
        hint1Images[(int)hintPos].SetHintNumText(hintNum);
        hint1Images[(int)hintPos].ShowUI();
    }

    public void ShowSecondHint(HintPos hintPos, Sprite hintSprite)
    {
        hint2Images[(int)hintPos].SetHintImage(hintSprite);
        hint2Images[(int)hintPos].ShowUI();
    }

    public void ShowLastHint(Sprite hintSprite)
    {
        hint3Image.SetHintImage(hintSprite);
        hint3Image.ShowUI();
    }

    public void HideFirstHint(HintPos hintPos)
    {
        hint1Images[(int)hintPos].HideUI();
    }

    public void HideSecondHint(HintPos hintPos)
    {
        hint2Images[(int)hintPos].HideUI();
    }

    public void HideLastHint()
    {
        hint3Image.HideUI();
    }

    public void SetRemainingStampText(int remainingStampCount)
    {
        //if (remainingStampText != null) remainingStampText.text = "= " + remainingStampCount.ToString();
    }

    public void ShowHintWarning(HintWarningUI.HintUnlockTarget unlockTarget, int remainingStampCount, bool canAffordHint, string warningText)
    {
        if (lastHintWarningUI == null) return;

        lastHintWarningUI.SetUnlockTarget(unlockTarget);
        lastHintWarningUI.SetRemainingStampText(remainingStampCount);
        lastHintWarningUI.SetYesButtonInteractable(canAffordHint);
        lastHintWarningUI.SetWarningText(warningText);
        lastHintWarningUI.ShowNormalMode();
        lastHintWarningUI.ShowUI();
    }

    public void ShowHintWaitWarning(string waitText)
    {
        if (lastHintWarningUI == null) return;

        lastHintWarningUI.SetWarningText(waitText);
        lastHintWarningUI.ShowWaitMode();
        lastHintWarningUI.ShowUI();
    }
}
