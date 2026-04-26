 using System;
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

    [SerializeField] private StageManager stageManager;
    [SerializeField] private PaintManager paintManager;

    private Action paintAction1;
    private Action paintAction2;
    private Action paintAction3;

    private Action foldAction;
    [SerializeField] private GameObject realFoldButton;
    private Action resetAction;
    private Action switchAction;

    void Awake()
    {
        paintAction1 = () => paintManager.Paint(19);
        paintAction2 = () => paintManager.Paint(10);
        paintAction3 = () => paintManager.Paint(1);

        foldAction = () => paintManager.FoldHorizontal();
        resetAction = () => stageManager.ResetButton();
        switchAction = () => stageManager.SwitchState();
    }

    private async UniTask WaitForCondition(int conditionID)
    {
        Debug.Log($"Waiting for condition {conditionID}");
        switch (conditionID)
        {
            case 0:
                await UniTask.WhenAll(
                    WaitForClick(clickButtons[0], paintAction1),
                    WaitForClick(clickButtons[1], paintAction2),
                    WaitForClick(clickButtons[2], paintAction3)
                );
                break;
            case 1:
                await WaitForClick(clickButtons[3]);
                break;
            case 2:
                await WaitForClick(clickButtons[4], foldAction);
                break;
            case 3:
                while (!Input.GetMouseButtonDown(0))
                {
                    await UniTask.Yield();
                }
                break;
            case 4:
                while (!Input.GetMouseButtonDown(1))
                {
                    await UniTask.Yield();
                }
                break;
            case 5:
                await WaitForClick(clickButtons[5], resetAction);
                break;
            case 6:
                await WaitForClick(clickButtons[6], switchAction);
                break;
        }
    }

    private UniTask WaitForClick(ButtonUI button, System.Action onClicked = null)
    {
        var tcs = new UniTaskCompletionSource();
        button.OnSingleClick += Complete;
        return tcs.Task;

        void Complete()
        {
            button.OnSingleClick -= Complete;
            button.SetUI(false);
            onClicked?.Invoke();
            tcs.TrySetResult();
        }
    }

    public async void StartTutorial()
    {
        TutoCanvas.SetActive(true);

        await UniTask.Yield(); // wait for all Start() to complete before showing buttons

        curTutoID++; // 0

        ChangeDinoTutoSprite(curTutoID);

        clickButtons[0].SetUI(true);
        clickButtons[1].SetUI(true);
        clickButtons[2].SetUI(true);

        await WaitForCondition(curTutoID); // 클릭 3개
        curTutoID++; // 1
        ChangeDinoTutoSprite(curTutoID);

        clickButtons[3].SetUI(true);

        await WaitForCondition(curTutoID); // 회색 부분 클릭
        curTutoID++; // 2
        ChangeDinoTutoSprite(curTutoID);

        realFoldButton.SetActive(false);

        clickButtons[4].SetUI(true);
        clickButtons[4].transform.DOScale(transform.localScale * 0.6f, 0.5f)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        await WaitForCondition(curTutoID); // 접기.

        realFoldButton.SetActive(true);

        curTutoID++; // 3
        ChangeDinoTutoSprite(curTutoID);

        await WaitForCondition(curTutoID); // 화면 아무데나 클릭
        curTutoID++; // 4
        ChangeDinoTutoSprite(curTutoID);

        await WaitForCondition(curTutoID); // 화면 아무데나 클릭
        curTutoID++; // 5
        ChangeDinoTutoSprite(curTutoID);

        // 리셋 버튼 깜빡깜빡.
        clickButtons[5].SetUI(true);
        clickButtons[5].transform.DOScale(transform.localScale * 0.6f, 0.5f)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        await WaitForCondition(curTutoID); // 리셋 버튼 클릭
        curTutoID++; // 6
        ChangeDinoTutoSprite(curTutoID);

        // 스위치 버튼 깜빡깜빡.
        clickButtons[6].SetUI(true);
        clickButtons[6].transform.DOScale(transform.localScale * 0.6f, 0.5f)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);

        await WaitForCondition(curTutoID); // 스위치 버튼 클릭
        TutoCanvas.SetActive(false);
    }

    private void ChangeDinoTutoSprite(int id)
    {
        if(GameManager.Instance.CurrentLanguage == GameLanguage.Korean)
        {
            dinoImage.sprite = dinoTutoSpritesKR[id];
        }
        else
        {
            dinoImage.sprite = dinoTutoSpritesEN[id];
        }
        dinoImage.SetNativeSize();
    }
}
