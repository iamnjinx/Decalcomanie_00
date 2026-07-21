using System;
using Njinx.UI;
using UnityEngine;

/// <summary>
/// "Buttons" 그룹: 챕터 이동(좌/우) 및 타이틀 복귀(뒤로) 버튼을 담당한다.
/// </summary>
[Serializable]
public class StageNavigation
{
    [SerializeField] private ButtonUI leftButton;
    [SerializeField] private ButtonUI rightButton;
    [SerializeField] private ButtonUI backButton;

    public ButtonUI LeftButton => leftButton;
    public ButtonUI RightButton => rightButton;
    public ButtonUI BackButton => backButton;

    /// <summary>챕터 전환 애니메이션 도중 좌/우 버튼을 즉시 감춘다.</summary>
    public void HideChapterButtons()
    {
        leftButton.HideUI();
        rightButton.HideUI();
    }

    /// <summary>현재 챕터 위치에 맞춰 좌/우 버튼의 표시 여부를 갱신한다.</summary>
    public void RefreshChapterButtons(int chapterIndex, int maxChapterIndex)
    {
        leftButton.SetUI(chapterIndex > 0);
        rightButton.SetUI(chapterIndex < maxChapterIndex);
    }
}
