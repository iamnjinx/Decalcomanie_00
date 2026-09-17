using System;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class StageUI : MonoBehaviour
{
    [SerializeField] BaseUI mainUI;
    [SerializeField] BaseUI paintUI;
    [SerializeField] BaseUI[] curtainUI;

    [Header("Stage UI Buttons")]
    public ButtonUI menuButton;

    public FunctionButton switchButton;
    public FunctionButton resetButton;
    public ButtonUI undoButton;

    public BaseUI undoInstruction;
    public TextMeshProUGUI undoInstructionText;

    public FunctionButton flipHorizontalButton;
    public FunctionButton flipVerticalButton;

    public BaseUI flipHorizontalInstruction;
    public TextMeshProUGUI flipHorizontalInstructionText;
    public BaseUI flipVerticalInstruction;
    public TextMeshProUGUI flipVerticalInstructionText;

    [Header("Stage Main UI")]
    public List<Sprite> stageBackgroundSprites;
    public Image stageMainBackgroundImage;
    public TextMeshProUGUI stageTitleText;

    [Header("Objectives")]
    // 0: Stage Clear (early stage only), 1: Star Earned, 2: Star & Min Moves
    public GameObject objectiveObjParent;
    public TextMeshProUGUI[] objectiveTexts = new TextMeshProUGUI[3];
    public Image[] objectiveStrikeThroughImages = new Image[3];
    public ButtonUI objectiveObj;
    public float objectiveHidePosX;
    public float objectiveHoverPosX = float.NaN;
    public float objectiveMoveDuration = 0.3f;
    public float objectiveHoverMoveDuration = 0.2f;
    public float objectiveStrikeFillDuration = 0.3f;

    [SerializeField] private RectTransform objectiveRectTransform;
    private float objectiveShowPosX;
    private bool isObjectiveShown = true;

    private bool objectiveStarAchieved = false;
    private int lastUsedTileCount = 0;

    public GameObject usedTileObj;
    public TextMeshProUGUI usedTileText;
    public Color usedTileOverMinColor = Color.red;

    private Color usedTileDefaultColor;
    private int currentMinMoves;

    [Header("Stage Cleared UI")]
    public BaseUI stageClearedUI;
    public BaseUI stageClearTextUI;
    public GameObject stageClearedConfettiObj;
    public BaseUI stageClearedAchievementUI;
    public TextMeshProUGUI[] stageClearedTexts = new TextMeshProUGUI[3]; // 0: Stage Clear, 1: Star Earned, 2: Min Moves

    public ButtonUI stageClearedNextStageButton;
    public BaseUI stageClearedNextStageKeyUI;
    public ButtonUI stageClearedStageSelectionButton;
    public BaseUI stageClearedStageSelectionKeyUI;
    public ButtonUI stageClearedRestartStageButton;
    public BaseUI stageClearedRestartStageKeyUI;
    public TextMeshProUGUI[] stageClearedAfterButtonTexts = new TextMeshProUGUI[3]; // 0: Next Stage, 1: Stage Selection, 2: Restart Stage

    public Image achivementImage;
    public List<BaseUI> stars;
    public BaseUI Stamp;

    // 별 3개 달성으로 도장이 찍히는 순간 호출.
    public event Action OnStampStamped;

    public ButtonUI nextStageButton;
    public TextMeshProUGUI minMovesText;

    public BaseUI postStageUI;

    [Header("Guide UI")]
    public SpriteRenderer guideImage;

    [Header("Tutorial")]
    public BaseUI clickTutorialUI;
    public BaseUI resetTutorialUI;
    public BaseUI switchTutorialUI;

    public GameObject mobileControlButtonContainer;

    [Header("Item Buttons")]
    public ButtonUI pencilButton;
    public ButtonUI eraserButton;

    // BindHover로 걸어 둔 구독을 OnDestroy에서 한꺼번에 되돌리기 위한 목록.
    private readonly List<Action> unbindActions = new List<Action>();

    #region Lifecycle

    void Awake()
    {
        usedTileDefaultColor = usedTileText.color;

        objectiveShowPosX = objectiveRectTransform.anchoredPosition.x;

        if (float.IsNaN(objectiveHoverPosX))
            objectiveHoverPosX = Mathf.Lerp(objectiveHidePosX, objectiveShowPosX, 0.1f);
    }

    void Start()
    {
        menuButton.OnSingleClick += HandleMenuClicked;

        BindHover(objectiveObj, HandleObjectiveHoverEnter, HandleObjectiveHoverExit);
        objectiveObj.OnSingleClick += ToggleObjectivePosition;
        unbindActions.Add(() => objectiveObj.OnSingleClick -= ToggleObjectivePosition);

        BindInstruction(undoButton, undoInstruction);
        BindInstruction(flipHorizontalButton, flipHorizontalInstruction);
        BindInstruction(flipVerticalButton, flipVerticalInstruction);

        ApplyInstructionTexts(GameManager.Instance.CurrentLocalizedData);
    }

    private void OnDestroy()
    {
        foreach (Action unbind in unbindActions) unbind();
        unbindActions.Clear();

        if (menuButton != null) menuButton.OnSingleClick -= HandleMenuClicked;
    }

    private void HandleMenuClicked() => SettingManager.Instance.OpenSetting();

    // 버튼에 마우스를 올리면 설명 UI를 보여주고, 벗어나면 숨깁니다.
    private void BindInstruction(ButtonUI button, BaseUI instruction)
    {
        if (button == null || instruction == null) return;
        BindHover(button, instruction.ShowUI, instruction.HideUI);
    }

    private void BindHover(ButtonUI button, Action onEnter, Action onExit)
    {
        if (button == null) return;

        if (onEnter != null)
        {
            button.OnHoverEnter += onEnter;
            unbindActions.Add(() => button.OnHoverEnter -= onEnter);
        }

        if (onExit != null)
        {
            button.OnHoverExit += onExit;
            unbindActions.Add(() => button.OnHoverExit -= onExit);
        }
    }

    #endregion

    #region Localized text

    private void ApplyInstructionTexts(LocalizedData data)
    {
        ApplyText(undoInstructionText, data.undoText, data.fontAsset);
        ApplyText(flipHorizontalInstructionText, data.flipHorizontalText, data.fontAsset);
        ApplyText(flipVerticalInstructionText, data.flipVerticalText, data.fontAsset);
    }

    // 목표 3종(클리어 / 별 획득 / 최소 이동)은 목표 패널과 클리어 화면에서 같은 문구를 씁니다.
    private static void ApplyAchievementTexts(TextMeshProUGUI[] texts, LocalizedData data, int minMoves)
    {
        ApplyText(texts[0], data.stageClearText, data.fontAsset);
        ApplyText(texts[1], data.starEarnedText, data.fontAsset);
        ApplyText(texts[2], string.Format(data.starMovesFormat, minMoves), data.fontAsset);
    }

    private static void ApplyText(TextMeshProUGUI text, string value, TMP_FontAsset font)
    {
        if (text == null) return;
        text.text = value;
        text.font = font;
    }

    #endregion

    #region Objective panel

    private void HandleObjectiveHoverEnter()
    {
        if (isObjectiveShown) return;
        MoveObjectiveTo(objectiveHoverPosX, objectiveHoverMoveDuration);
    }

    private void HandleObjectiveHoverExit()
    {
        if (isObjectiveShown) return;
        MoveObjectiveTo(objectiveHidePosX, objectiveHoverMoveDuration);
    }

    private void ToggleObjectivePosition()
    {
        ToggleObjectivePosition(-1f);
    }

    private void ToggleObjectivePosition(float duration)
    {
        isObjectiveShown = !isObjectiveShown;
        MoveObjectiveTo(isObjectiveShown ? objectiveShowPosX : objectiveHidePosX, duration);
        //objectiveObj.transform.DOLocalRotate(new Vector3(0f, 0f, 180f), objectiveMoveDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad);
    }

    private void MoveObjectiveTo(float targetX, float duration)
    {
        float tweenDuration = duration <= 0f ? objectiveMoveDuration : duration;
        objectiveRectTransform.DOKill();
        objectiveRectTransform.DOAnchorPosX(targetX, tweenDuration).SetEase(Ease.InOutQuad);
    }

    // 플랫포머 모드로 전환될 때, 접혀 있던 목표(Objective)를 펼쳐줍니다.
    public void ShowObjectiveIfHidden()
    {
        if (isObjectiveShown) return;
        ToggleObjectivePosition(-1f);
    }

    // 색칠 모드로 전환될 때, 펼쳐져 있던 목표(Objective)를 접어줍니다.
    public void HideObjectiveIfShown()
    {
        if (!isObjectiveShown) return;
        ToggleObjectivePosition(-1f);
    }

    public void SetObjectiveTexts(GameLanguage language, bool isEarlyStage, int minMoves)
    {
        objectiveObjParent.SetActive(!isEarlyStage);

        ApplyAchievementTexts(objectiveTexts, GameManager.Instance.LocalizationData.GetData(language), minMoves);

        currentMinMoves = minMoves;
        objectiveStarAchieved = false;
        lastUsedTileCount = 0;

        for (int i = 0; i < objectiveStrikeThroughImages.Length; i++)
            ApplyObjectiveStrike(i, false, true);
    }

    // 스테이지 클리어(문 통과) 시 0번 목표를 채워줍니다.
    public void SetObjectiveCleared(bool cleared)
    {
        ApplyObjectiveStrike(0, cleared);
    }

    // 별 획득 시 1번 목표를 채워주고, 2번 목표(별 + 최소 이동) 달성 여부를 다시 계산합니다.
    public void SetObjectiveStarObtained(bool obtained)
    {
        objectiveStarAchieved = obtained;
        ApplyObjectiveStrike(1, obtained);
        UpdateMinMovesObjectiveStrike();
    }

    private void UpdateMinMovesObjectiveStrike()
    {
        bool achieved = objectiveStarAchieved && currentMinMoves > 0 && lastUsedTileCount <= currentMinMoves;
        ApplyObjectiveStrike(2, achieved);
    }

    private void ApplyObjectiveStrike(int index, bool achieved, bool instant = false)
    {
        if (index < 0 || index >= objectiveStrikeThroughImages.Length) return;

        Image image = objectiveStrikeThroughImages[index];
        if (image == null) return;

        image.DOKill();
        float target = achieved ? 1f : 0f;

        if (instant) image.fillAmount = target;
        else image.DOFillAmount(target, objectiveStrikeFillDuration);
    }

    #endregion

    #region Stage header / counters

    public async void ShowMainUI(bool isPlatformerOnly = false)
    {
        if (!isPlatformerOnly)
        {
            await UniTask.Delay(1000);
            paintUI.ShowUI(.5f).Forget();
            flipHorizontalButton.ShowUI(.5f).Forget();
            flipVerticalButton.ShowUI(.5f).Forget();
            // foreach (var curtain in curtainUI)
            // {
            //     curtain.ShowUI(.5f).Forget();
            // }
        }
        ToggleObjectivePosition(1f);
        await mainUI.ShowUI(.5f);
    }

    public void SetCurStageText(int stageID)
    {
        //Material material = stageTitleText.fontMaterial;
        //material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 1.2f); // 1보다 큰 값
        stageTitleText.text = $"STAGE {stageID / 10 + 1}-{stageID % 10 + 1}";
    }

    public void SetStageBackground(int stageID)
    {
        if (stageID < 0 || stageID >= stageBackgroundSprites.Count * 10) return; // stageID가 유효한 경우에만 배경을 설정
        //stageMainBackgroundImage.sprite = stageBackgroundSprites[stageID / 10];
    }

    public void SetAfterButtonTexts(GameLanguage language)
    {
        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        ApplyText(stageClearedAfterButtonTexts[0], data.nextStageText, data.fontAsset);
        ApplyText(stageClearedAfterButtonTexts[1], data.stageSelectionText, data.fontAsset);
        ApplyText(stageClearedAfterButtonTexts[2], data.restartStageText, data.fontAsset);
    }

    public void UpdateUsedTileText(int usedTileCount)
    {
        lastUsedTileCount = usedTileCount;

        usedTileText.text = $"{usedTileCount}/{currentMinMoves}";
        if (currentMinMoves > 0)
            usedTileText.color = usedTileCount > currentMinMoves ? usedTileOverMinColor : usedTileDefaultColor;

        UpdateMinMovesObjectiveStrike();
    }

    #endregion

    #region Button visibility

    public void SetPlatformerOnlyMode()
    {
        switchButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        undoButton.gameObject.SetActive(false);
        usedTileObj.SetActive(false);
    }

    // 초반 스테이지에서는 아직 배우지 않은 버튼을 숨깁니다. 규칙은 StageRules가 갖고 있습니다.
    public void SetEarlyStageUIVisibility(int stageID)
    {
        switchButton.gameObject.SetActive(StageRules.IsSwitchUnlocked(stageID));
        resetButton.gameObject.SetActive(StageRules.IsResetUnlocked(stageID));
        SetFoldButtonsActive(StageRules.IsFoldUnlocked(stageID));
        usedTileObj.SetActive(StageRules.IsUsedTileCountShown(stageID));
    }

    // 튜토리얼(타일 클릭/접기)로 조기 활성화될 때 호출됩니다.
    public void SetSwitchButtonActive(bool active) => switchButton.gameObject.SetActive(active);
    public void SetResetButtonActive(bool active) => resetButton.gameObject.SetActive(active);
    public void SetFoldButtonsActive(bool active)
    {
        flipHorizontalButton.gameObject.SetActive(active);
        flipVerticalButton.gameObject.SetActive(active);
    }

    // 튜토리얼 조건으로 조기 활성화됐음을 강조합니다.
    public void HighlightSwitchButton() => switchButton.OnHighlighted();
    public void HighlightResetButton() => resetButton.OnHighlighted();
    public void HighlightFoldButtons()
    {
        flipHorizontalButton.OnHighlighted();
        flipVerticalButton.OnHighlighted();
    }

    // 강조된 버튼을 한 번이라도 클릭하면 강조를 해제합니다.
    public void UnhighlightSwitchButton() => switchButton.OnUnhighlighted();
    public void UnhighlightResetButton() => resetButton.OnUnhighlighted();
    public void UnhighlightFoldButtons()
    {
        flipHorizontalButton.OnUnhighlighted();
        flipVerticalButton.OnUnhighlighted();
    }

    #endregion

    #region Paint mode buttons

    // 연필/지우개 버튼 중 현재 모드에 해당하는 것만 살짝 확대해 선택 상태를 표시한다.
    private readonly Dictionary<ButtonUI, Vector3> paintModeButtonBaseScale = new Dictionary<ButtonUI, Vector3>();

    public void SetPaintModeButtonsSelected(PaintManager.PaintMode mode)
    {
        SetPaintModeButtonSelected(pencilButton, mode == PaintManager.PaintMode.Pencil);
        SetPaintModeButtonSelected(eraserButton, mode == PaintManager.PaintMode.Eraser);
    }

    private void SetPaintModeButtonSelected(ButtonUI button, bool selected)
    {
        if (button == null) return;

        if (!paintModeButtonBaseScale.TryGetValue(button, out Vector3 baseScale))
        {
            baseScale = button.transform.localScale;
            paintModeButtonBaseScale[button] = baseScale;
        }

        button.transform.DOKill();
        button.transform.DOScale(selected ? baseScale * 1.15f : baseScale, 0.15f).SetEase(Ease.OutQuad);
    }

    #endregion

    #region Stage cleared

    public async UniTask ShowStageCleared(Achievements achievements, int minMoveNum, bool isEarlyStage)
    {
        SetAfterButtonsActive(!isEarlyStage);

        await stageClearedUI.ShowUI(1f);
        AudioManager.Instance.PlaySFX("Confetti");
        stageClearedConfettiObj.SetActive(true);

        await UniTask.Delay(500);

        if (!isEarlyStage)
        {
            await ShowAchievementPanel(minMoveNum);
            await ShowAchievementStars(achievements);
        }

        // 화면 터치 시, 다음 스테이지로 넘어가게.
        await postStageUI.ShowUI(.5f);

        stageClearedNextStageButton.ShowUI();
        stageClearedNextStageKeyUI.ShowUI();
    }

    // 초반 스테이지에서는 다음 스테이지 버튼만 남기고 나머지 선택지를 감춥니다.
    private void SetAfterButtonsActive(bool active)
    {
        stageClearedStageSelectionButton.gameObject.SetActive(active);
        stageClearedStageSelectionKeyUI.gameObject.SetActive(active);
        stageClearedRestartStageButton.gameObject.SetActive(active);
        stageClearedRestartStageKeyUI.gameObject.SetActive(active);
    }

    private async UniTask ShowAchievementPanel(int minMoveNum)
    {
        LocalizedData data = GameManager.Instance.CurrentLocalizedData;
        ApplyAchievementTexts(stageClearedTexts, data, minMoveNum);
        achivementImage.sprite = data.achievementSprite;

        stageClearTextUI.HideUI(1f).Forget();
        await stageClearedAchievementUI.ShowUI(1f);
    }

    private async UniTask ShowAchievementStars(Achievements achievements)
    {
        if (achievements.IsCleared)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("stage_clear_star");
            stars[0].ShowUI(.1f).Forget();
        }
        if (achievements.ObtainedStar) stars[1].ShowUI(.1f).Forget();
        if (achievements.MinMoves) stars[2].ShowUI(.1f).Forget();

        if (achievements.IsCleared || achievements.ObtainedStar || achievements.MinMoves)
            await UniTask.Delay(1500);

        if (!achievements.IsAllAchieved) return;

        // 스탬프 찍고 1.5초 대기.
        Stamp.ShowUI(.1f).Forget();
        Stamp.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f);
        OnStampStamped?.Invoke();
        await UniTask.Delay(1500);
    }

    #endregion
}
