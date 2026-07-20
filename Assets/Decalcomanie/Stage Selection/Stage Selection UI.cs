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
    public BaseUI[] chapterFlip = new BaseUI[4];

    public bool is_changingChapter = false;

    public Transform bookmarkParent;
    public BaseUI[] bookmarks;
    public ButtonUI[] bookmarkButtons;

    private int currentBookmarkIndex = -1;




    public Action OnChapterChanged;
    public Action<int> OnBookmarkSelected;

    [SerializeField] private float nextDuration = 0.3f;

    [Header("Demo")]
    public GameObject demoUI;
    public Image demoImage;
    public ButtonUI demoButton;
    [SerializeField] private string demoLinkUrl;

    void Start()
    {
        BackButton.OnSingleClick += () => GameManager.Instance.LoadTitleScene();
        demoButton.OnSingleClick += () => Application.OpenURL(demoLinkUrl);

        demoImage.sprite = GameManager.Instance.CurrentLocalizedData.demoSprite;

        if (bookmarkButtons != null)
        {
            for (int i = 0; i < bookmarkButtons.Length; i++)
            {
                if (bookmarkButtons[i] == null) continue;
                int chapterIndex = i;
                bookmarkButtons[i].OnSingleClick += () => OnBookmarkSelected?.Invoke(chapterIndex);
            }
        }
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

        UpdateBookmarkOrder(chapterIndex);

        if (is_instant)
        {
            chapterBackgroundImage.sprite = GameManager.Instance.GameData.chapterBackgroundSprites[chapterIndex];
            chapterDeco[0].sprite = GameManager.Instance.GameData.chapterDecoL[chapterIndex];
            chapterDeco[1].sprite = GameManager.Instance.GameData.chapterDecoR[chapterIndex];
            OnChapterChanged?.Invoke();
        }
        else
        {
            StartCoroutine(WaitforChangeChapter(chapterIndex));

            if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("fold_paper");

            if(is_right) chapterFlip[3].ShowUI();
            else chapterFlip[0].ShowUI();

            await UniTask.Delay(TimeSpan.FromSeconds(nextDuration/2));

            if(is_right) {chapterFlip[3].HideUI(); chapterFlip[2].ShowUI(); }
            else {chapterFlip[0].HideUI(); chapterFlip[1].ShowUI(); }

            await UniTask.Delay(TimeSpan.FromSeconds(nextDuration/2));

            if(is_right) {chapterFlip[2].HideUI(); chapterFlip[1].ShowUI(); }
            else {chapterFlip[1].HideUI(); chapterFlip[2].ShowUI(); }
            UpdateDemoUI(chapterIndex, maxChapterIndex);

            await UniTask.Delay(TimeSpan.FromSeconds(nextDuration/2));

            if(is_right) {chapterFlip[1].HideUI(); chapterFlip[0].ShowUI(); }
            else {chapterFlip[2].HideUI(); chapterFlip[3].ShowUI(); }

            await UniTask.Delay(TimeSpan.FromSeconds(nextDuration/2));

            if(is_right) {chapterFlip[0].HideUI(); }
            else {chapterFlip[3].HideUI(); }
        }

        ChangeButtonState(chapterIndex, maxChapterIndex);

        is_changingChapter = false;
    }

    private void UpdateBookmarkOrder(int chapterIndex)
    {
        if (bookmarks == null) return;

        if (currentBookmarkIndex != chapterIndex
            && currentBookmarkIndex >= 0 && currentBookmarkIndex < bookmarks.Length
            && bookmarks[currentBookmarkIndex] != null)
        {
            bookmarks[currentBookmarkIndex].HideUI(.5f).Forget();
        }

        if (chapterIndex >= 0 && chapterIndex < bookmarks.Length && bookmarks[chapterIndex] != null)
        {
            bookmarks[chapterIndex].ShowUI(.5f).Forget();
        }

        currentBookmarkIndex = chapterIndex;
    }

    public void UpdateDemoUI(int chapterIndex, int maxChapterIndex)
    {
        if (demoUI == null) return;
        demoUI.SetActive(GameManager.Instance.IsDemo && chapterIndex == maxChapterIndex);
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

    IEnumerator WaitforChangeChapter(int chapterIndex)
    {
        chapterBackgroundImage.DOFade(0.7f, nextDuration).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(nextDuration);
        chapterBackgroundImage.sprite = GameManager.Instance.GameData.chapterBackgroundSprites[chapterIndex];
        chapterBackgroundImage.DOFade(1f, nextDuration).SetEase(Ease.InQuad);

        chapterDeco[0].sprite = GameManager.Instance.GameData.chapterDecoL[chapterIndex];
        chapterDeco[1].sprite = GameManager.Instance.GameData.chapterDecoR[chapterIndex];
        OnChapterChanged?.Invoke();
    }
}
