using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스테이지 선택 화면의 UI 파사드. Header 그룹별로 분리된 세 뷰
/// (내비게이션 버튼 / 스테이지 책 / 데모)를 조합하고, 그룹을 가로지르는
/// 챕터 전환 흐름만 여기서 조율한다.
/// </summary>
public class StageSelectionUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private StageNavigation navigation;

    [Header("Stage Book")]
    [SerializeField] private StageBookView book;

    [Header("Demo")]
    [SerializeField] private StageDemoView demo;

    public StageNavigation Navigation => navigation;
    public StageBookView Book => book;
    public StageDemoView Demo => demo;

    public bool IsChangingChapter { get; private set; }

    public event Action OnChapterChanged;
    public event Action<int> OnBookmarkSelected;

    void Start()
    {
        demo.Initialize();
        book.WireBookmarks(index => OnBookmarkSelected?.Invoke(index));
        book.RefreshStampCountText();
    }

    /// <summary>
    /// 챕터를 전환한다. 즉시 전환이면 스프라이트만 바꾸고, 아니면 배경 페이드와
    /// 페이지 넘김 애니메이션을 동시에 재생한다.
    /// </summary>
    public async void ChangeChapterDisplay(int chapterIndex, int maxChapterIndex, bool is_right, bool is_instant = false)
    {
        IsChangingChapter = true;
        navigation.HideChapterButtons();

        chapterIndex = Mathf.Clamp(chapterIndex, 0, maxChapterIndex);

        book.MoveBookmark(chapterIndex);

        if (is_instant)
        {
            book.ApplyChapterInstant(chapterIndex);
            OnChapterChanged?.Invoke();
        }
        else
        {
            book.FadeToChapterAsync(chapterIndex, () => OnChapterChanged?.Invoke()).Forget();

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("fold_paper");

            await book.PlayFlipAsync(is_right, () => demo.UpdateVisibility(chapterIndex, maxChapterIndex));
        }

        navigation.RefreshChapterButtons(chapterIndex, maxChapterIndex);
        IsChangingChapter = false;
    }
}
