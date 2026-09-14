using UnityEngine;

public class HintManager : MonoBehaviour
{
    [SerializeField] private HintUI hintUI;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private int hintCost = 1;

    private static int StageIndex => GameManager.Instance.CurrentStageIndex;

    private HintElement CurrentHintElement => GameManager.Instance.ActiveHintData.hintElements[StageIndex];

    private static bool IsSecondHintUnlocked => GameProgressData.Load().IsSecondHintUnlocked(StageIndex);

    private void Awake()
    {
        // 힌트가 아직 등장하지 않는 초반 스테이지에서는 UI 자체를 숨긴다.
        if (!StageRules.IsHintAvailable(StageIndex))
            hintUI.gameObject.SetActive(false);

        if (stageManager != null)
            stageManager.OnGameStateChanged += HandleGameStateChanged;

        if (IsSecondHintUnlocked)
            hintUI.hintButton.UnlockHintButton();

        hintUI.hintButton.OnPressed += HandleHintPressed;
        hintUI.hintButton.OnReleased += HandleHintReleased;
    }

    void Start()
    {
        RefreshStampText();
    }

    private void OnDestroy()
    {
        if (stageManager != null)
            stageManager.OnGameStateChanged -= HandleGameStateChanged;

        if (hintUI != null && hintUI.hintButton != null)
        {
            hintUI.hintButton.OnPressed -= HandleHintPressed;
            hintUI.hintButton.OnReleased -= HandleHintReleased;
        }
    }

    public void RefreshStampText()
    {
        hintUI.SetRemainingStampText(GameProgressData.Load().remainingStampCount);
    }

    private void HandleHintPressed()
    {
        if (IsSecondHintUnlocked)
            hintUI.ShowSecondHint(CurrentHintElement.Hint2Pos, CurrentHintElement.Hint2Sprite);
        else
            ShowHintUnlockWarning();
    }

    private void HandleHintReleased()
    {
        if (IsSecondHintUnlocked)
            hintUI.HideSecondHint(CurrentHintElement.Hint2Pos);
    }

    // 힌트 UI는 색칠 모드에서만 보여준다.
    private void HandleGameStateChanged(GameState newState)
    {
        if (!StageRules.IsHintAvailable(StageIndex)) return;
        hintUI.gameObject.SetActive(newState == GameState.Paint);
    }

    private void ShowHintUnlockWarning()
    {
        int remainingStampCount = GameProgressData.Load().remainingStampCount;
        hintUI.ShowHintWarning(remainingStampCount, remainingStampCount >= hintCost, GameManager.Instance.CurrentLocalizedData.hintWarningText);
    }

    public void UnlockHint()
    {
        GameProgressData progress = GameProgressData.Load();
        if (progress.remainingStampCount < hintCost) return;

        progress.remainingStampCount -= hintCost;
        progress.SetSecondHintUnlocked(StageIndex);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        hintUI.hintButton.UnlockHintButton();

        RefreshStampText();
    }
}
