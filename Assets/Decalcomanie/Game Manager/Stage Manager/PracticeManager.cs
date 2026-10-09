using System;
using Njinx.UI;
using UnityEngine;

/// <summary>
/// 연습(Practice) 모드. 화면을 어둡게 덮고 그 위에 자유롭게 빨간 선을 그릴 수 있게 합니다.
/// GameState와는 별개인 오버레이라서, 켜고 꺼도 색칠 상태는 그대로 유지됩니다.
///
/// - 색칠(Paint) 모드에서만 켤 수 있고, 다른 모드로 바뀌면 자동으로 꺼집니다.
/// - 그린 그림은 같은 스테이지 안에서는 남아 있고, 스테이지/씬이 바뀌면 씬 재로드와 함께 사라집니다.
/// - 지우개 버튼(타일 지우개와는 별개)은 연습 모드 중에만 보이고, 누르면 그림을 전부 지웁니다.
/// </summary>
public class PracticeManager : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private StageUI stageUI;
    [SerializeField] private BaseUI practiceOverlay; // 어두운 반투명 배경. Start Hidden을 켜 두세요.
    [SerializeField] private PracticeDrawingSurface drawingSurface;

    public bool IsPracticeMode { get; private set; }

    public event Action<bool> OnPracticeModeChanged;

    #region Lifecycle

    void Awake()
    {
        drawingSurface.enabled = false;

        stageUI.practiceButton.OnSingleClick += TogglePractice;
        stageUI.practiceEraserButton.OnSingleClick += drawingSurface.Clear;
        stageManager.OnGameStateChanged += HandleGameStateChanged;
    }

    void OnDestroy()
    {
        if (stageUI != null)
        {
            stageUI.practiceButton.OnSingleClick -= TogglePractice;
            if (drawingSurface != null) stageUI.practiceEraserButton.OnSingleClick -= drawingSurface.Clear;
        }

        if (stageManager != null)
            stageManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    #endregion

    public void TogglePractice() => SetPracticeMode(!IsPracticeMode);
    public void ExitPractice() => SetPracticeMode(false);

    private void SetPracticeMode(bool on)
    {
        if (IsPracticeMode == on) return;
        if (on && stageManager.CurrentGameState != GameState.Paint) return;

        IsPracticeMode = on;

        // 끄면 그림을 가리기만 하고 지우지는 않습니다. 같은 스테이지에서 다시 켜면 그대로 보입니다.
        drawingSurface.enabled = on;

        // 페이드 중에 다시 토글되면 BaseUI 상태가 꼬이므로 즉시 켜고 끕니다.
        if (on)
        {
            practiceOverlay.ShowUI();
            stageUI.practiceEraserButton.ShowUI();
        }
        else
        {
            practiceOverlay.HideUI();
            stageUI.practiceEraserButton.HideUI();
        }

        OnPracticeModeChanged?.Invoke(on);
    }

    // 연습 버튼은 색칠 모드에서만 보여주고, 다른 모드로 넘어가면 연습 모드도 닫습니다.
    private void HandleGameStateChanged(GameState newState)
    {
        bool isPaint = newState == GameState.Paint;
        if (!isPaint) ExitPractice();
        stageUI.practiceButton.gameObject.SetActive(isPaint);
    }
}
