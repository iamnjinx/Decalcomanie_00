using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class StageSelectionUI : MonoBehaviour
{
    public Image chapterBackgroundImage;

    [Header("Buttons")]
    public ButtonUI LeftButton;
    public ButtonUI RightButton;
    public ButtonUI BackButton;


    [Header("Stage Book")]
    public Image stageBookImage;
    public CanvasGroup chapterCG;
    public StagePanel[] stagePanels = new StagePanel[10];
    public Image[] chapterDeco = new Image[2];
    public BaseUI[] chapterFlip = new BaseUI[2];

    public bool is_changingChapter = false;

    public Action OnChapterChanged;

    [SerializeField] private float nextDuration = 0.3f;

    void Start()
    {
        BackButton.OnSingleClick += () => GameManager.Instance.LoadTitleScene();
    }

    public void SetStagePanel()
    {
        
    }

    public async void ChangeChapterDisplay(int chapterIndex, int maxChapterIndex, bool is_right, bool is_instant = false)
    {
        is_changingChapter = true;
        LeftButton.HideUI();
        RightButton.HideUI();

        if(chapterIndex > maxChapterIndex)
        {
            chapterIndex = maxChapterIndex;
        }
        else if(chapterIndex < 0)
        {
            chapterIndex = 0;
        }

        if (is_instant)
        {
            chapterBackgroundImage.sprite = GameManager.Instance.GameData.chapterBackgroundSprites[chapterIndex];
        }
        else
        {
            // 배경 어두워짐.
            if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("fold_paper");

            chapterBackgroundImage.DOFade(0.7f, nextDuration).SetEase(Ease.InQuad);

            if(is_right) chapterFlip[1].ShowUI();
            else chapterFlip[0].ShowUI();

            await UniTask.Delay((int)(nextDuration * 500));

            if(is_right) chapterFlip[1].HideUI();
            else chapterFlip[0].HideUI();

            await UniTask.Delay((int)(nextDuration * 500));

            Debug.Log($"Chapter Index: {chapterIndex}, Background Index: {chapterIndex/10}");
            chapterBackgroundImage.sprite = GameManager.Instance.GameData.chapterBackgroundSprites[chapterIndex];

            chapterDeco[0].sprite = GameManager.Instance.GameData.chapterDecoL[chapterIndex];
            chapterDeco[1].sprite = GameManager.Instance.GameData.chapterDecoR[chapterIndex];
            OnChapterChanged?.Invoke();

            chapterBackgroundImage.DOFade(1f, nextDuration).SetEase(Ease.InQuad);

            await UniTask.Delay((int)(nextDuration * 1000));
        }

        ChangeButtonState(chapterIndex, maxChapterIndex);

        is_changingChapter = false;
    }

    private void ChangeButtonState(int chapterIndex, int maxChapterIndex)
    {
        if (chapterIndex == 0)
        {
            LeftButton.SetUI(false);
        }
        else
        {
            LeftButton.SetUI(true);
        }

        if (chapterIndex == maxChapterIndex)
        {
            RightButton.SetUI(false);
        }
        else
        {
            RightButton.SetUI(true);
        }
    }
}
