using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public Image dinoImage;
    public List<Sprite> dinoTutoSpritesKR;
    public List<Sprite> dinoTutoSpritesEN;

    public ButtonUI[] clickButtons = new ButtonUI[7];

    private int curTutoID = -1;

    public GameObject TutoCanvas;

    private async UniTask WaitForCondition(int conditionID)
    {
        // switch (conditionID)
        // {
        //     case 0:
        //         while (!clickButtons[0].IsClicked || !clickButtons[1].IsClicked || !clickButtons[2].IsClicked)
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        //     case 1:
        //         while (!clickButtons[3].IsClicked)
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        //     case 2:
        //         while (!clickButtons[4].IsClicked)
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        //     case 3:
        //         while (!Input.GetMouseButtonDown(0))
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        //     case 4:
        //         while (!Input.GetMouseButtonDown(0))
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        //     case 5:
        //         while (!clickButtons[5].IsClicked)
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        //     case 6:
        //         while (!clickButtons[6].IsClicked)
        //         {
        //             await UniTask.Yield();
        //         }
        //         break;
        // }
    }

    public async void StartTutorial()
    {
        curTutoID++; // 0

        ChangeDinoTutoSprite();

        clickButtons[0].SetUI(true);
        clickButtons[1].SetUI(true);
        clickButtons[2].SetUI(true);

        await WaitForCondition(curTutoID); // 클릭 3개
        curTutoID++; // 1
        ChangeDinoTutoSprite();

        clickButtons[3].SetUI(true);

        await WaitForCondition(curTutoID); // 회색 부분 클릭
        curTutoID++; // 2
        ChangeDinoTutoSprite();

        clickButtons[4].SetUI(true);
        clickButtons[4].transform.DOScale(transform.localScale * 0.6f, 0.5f)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        await WaitForCondition(curTutoID); // 접기.
        curTutoID++; // 3
        ChangeDinoTutoSprite();

        await WaitForCondition(curTutoID); // 화면 아무데나 클릭
        curTutoID++; // 4
        ChangeDinoTutoSprite();

        await WaitForCondition(curTutoID); // 화면 아무데나 클릭
        curTutoID++; // 5
        ChangeDinoTutoSprite();

        // 리셋 버튼 깜빡깜빡.
        clickButtons[5].SetUI(true);
        clickButtons[5].transform.DOScale(transform.localScale * 0.6f, 0.5f)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        await WaitForCondition(curTutoID); // 리셋 버튼 클릭
        curTutoID++; // 6
        ChangeDinoTutoSprite();

        // 스위치 버튼 깜빡깜빡.
        clickButtons[6].SetUI(true);
        clickButtons[6].transform.DOScale(transform.localScale * 0.6f, 0.5f)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        await WaitForCondition(curTutoID); // 스위치 버튼 클릭
        TutoCanvas.SetActive(false);
    }

    private void ChangeDinoTutoSprite()
    {
        if(GameManager.Instance.CurrentLanguage == GameLanguage.Korean)
        {
            dinoImage.sprite = dinoTutoSpritesKR[curTutoID];
        }
        else
        {
            dinoImage.sprite = dinoTutoSpritesEN[curTutoID];
        }
        dinoImage.SetNativeSize();
    }
}
