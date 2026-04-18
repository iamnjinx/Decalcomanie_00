using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class StageUI : MonoBehaviour
{
    public ButtonUI switchButton;
    public ButtonUI resetButton;

    public ButtonUI flipHorizontalButton;
    public ButtonUI flipVerticalButton;


    [Header("Stage Cleared UI")]
    public BaseUI stageClearedUI;
    public List<BaseUI> stars;
    public ButtonUI nextStageButton;

    public async void ShowStageCleared(bool isCleared, bool obtainedStar, bool minMoves)
    {
        await UniTask.Delay(1000);

        await stageClearedUI.ShowUI(1f);

        await ShowAchivementStars(isCleared, obtainedStar, minMoves);

        // 화면 터치 시, 다음 스테이지로 넘어가게.
        nextStageButton.ShowUI();
    }

    public async UniTask ShowAchivementStars(bool isCleared, bool obtainedStar, bool minMoves)
    {
        // Show stars based on achievements

        if (isCleared)
        {
            stars[0].ShowUI(1f).Forget();
        }
        if (obtainedStar)
        {
            stars[1].ShowUI(1f).Forget();
        }
        if (minMoves)
        {
            stars[2].ShowUI(1f).Forget();
        }

        if(isCleared || obtainedStar || minMoves)
        {
            await UniTask.Delay(1500); // Simulate delay for showing stars
        }
    }
}
