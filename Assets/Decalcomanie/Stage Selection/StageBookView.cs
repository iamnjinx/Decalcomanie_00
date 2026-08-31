using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Stage Book" 그룹: 스테이지 책(배경/장식/도장/책갈피/페이지 넘김)과
/// 스테이지 패널을 담당한다.
/// </summary>
[Serializable]
public class StageBookView
{
    [SerializeField] private Image chapterBackgroundImage;
    [SerializeField] private Image stageBookImage;
    [SerializeField] private CanvasGroup chapterCanvasGroup;
    [SerializeField] private StagePanel[] stagePanels = new StagePanel[10];
    [SerializeField] private Image[] chapterDeco = new Image[2];
    [SerializeField] private BaseUI[] chapterStamps = new BaseUI[2];
    [SerializeField] private BaseUI[] chapterFlip = new BaseUI[4];
    [SerializeField] private Transform bookmarkParent;
    [SerializeField] private BaseUI[] bookmarks;
    [SerializeField] private ButtonUI[] bookmarkButtons;
    [SerializeField] private TextMeshProUGUI stampCountText;
    [SerializeField] private float chapterChangeDuration = 0.3f;

    // 페이지 넘김 프레임을 보여주는 순서(오른쪽/왼쪽 방향).
    private static readonly int[] RightFlipOrder = { 3, 2, 1, 0 };
    private static readonly int[] LeftFlipOrder = { 0, 1, 2, 3 };

    private int currentBookmarkIndex = -1;

    public StagePanel[] StagePanels => stagePanels;

    // ----- 책갈피 -----

    /// <summary>책갈피 버튼 클릭을 챕터 선택 콜백에 연결한다.</summary>
    public void WireBookmarks(Action<int> onSelected)
    {
        if (bookmarkButtons == null) return;

        for (int i = 0; i < bookmarkButtons.Length; i++)
        {
            if (bookmarkButtons[i] == null) continue;
            int chapterIndex = i;
            bookmarkButtons[i].OnSingleClick += () => onSelected?.Invoke(chapterIndex);
        }
    }

    /// <summary>열린 챕터까지만 책갈피(버튼/이미지)를 활성화한다.</summary>
    public void SetBookmarksActive(int maxChapterIndex)
    {
        SetArrayActiveUpTo(bookmarkButtons, maxChapterIndex);
        SetArrayActiveUpTo(bookmarks, maxChapterIndex);
    }

    private static void SetArrayActiveUpTo(Component[] items, int maxIndex)
    {
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            items[i].gameObject.SetActive(i <= maxIndex);
        }
    }

    /// <summary>이전 책갈피는 접고 현재 챕터의 책갈피를 편다.</summary>
    public void MoveBookmark(int chapterIndex)
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

    // ----- 도장 / 카운트 -----

    /// <summary>남은 도장 개수 텍스트를 갱신한다.</summary>
    public void RefreshStampCountText()
    {
        if (stampCountText != null)
            stampCountText.text = "x " + GameProgressData.Load().remainingStampCount;
    }

    /// <summary>챕터 전/후반 도장 완성 여부를 반영한다.</summary>
    public void SetChapterStamps(bool firstHalfComplete, bool secondHalfComplete)
    {
        chapterStamps[0].SetUI(firstHalfComplete);
        chapterStamps[1].SetUI(secondHalfComplete);
    }

    // ----- 챕터 비주얼 -----

    /// <summary>배경/장식 스프라이트를 페이드 없이 즉시 교체한다.</summary>
    public void ApplyChapterInstant(int chapterIndex)
    {
        SetChapterSprites(chapterIndex);
    }

    /// <summary>배경을 어둡게 페이드한 뒤 스프라이트를 교체하고 다시 밝게 페이드한다.</summary>
    public async UniTask FadeToChapterAsync(int chapterIndex, Action onVisualsApplied)
    {
        chapterBackgroundImage.DOFade(0.7f, chapterChangeDuration).SetEase(Ease.InQuad);
        await UniTask.Delay(TimeSpan.FromSeconds(chapterChangeDuration));

        SetChapterSprites(chapterIndex);
        chapterBackgroundImage.DOFade(1f, chapterChangeDuration).SetEase(Ease.InQuad);

        onVisualsApplied?.Invoke();
    }

    private void SetChapterSprites(int chapterIndex)
    {
        var data = GameManager.Instance.GameData;
        if (data == null || chapterIndex < 0 || chapterIndex >= data.chapterBackgroundSprites.Count) return;
        chapterBackgroundImage.sprite = data.chapterBackgroundSprites[chapterIndex];
        chapterDeco[0].sprite = data.chapterDecoL[chapterIndex];
        chapterDeco[1].sprite = data.chapterDecoR[chapterIndex];
    }

    /// <summary>
    /// 페이지를 넘기는 애니메이션. 프레임을 순서대로 한 장씩 교체하며,
    /// 세 번째 프레임이 보일 때 <paramref name="onMidpoint"/>를 호출한다.
    /// </summary>
    public async UniTask PlayFlipAsync(bool isRight, Action onMidpoint)
    {
        int[] order = isRight ? RightFlipOrder : LeftFlipOrder;
        float stepDuration = chapterChangeDuration / 2f;

        chapterFlip[order[0]].ShowUI();

        for (int k = 1; k < order.Length; k++)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
            chapterFlip[order[k - 1]].HideUI();
            chapterFlip[order[k]].ShowUI();

            if (k == 2) onMidpoint?.Invoke();
        }

        await UniTask.Delay(TimeSpan.FromSeconds(stepDuration));
        chapterFlip[order[order.Length - 1]].HideUI();
    }
}
