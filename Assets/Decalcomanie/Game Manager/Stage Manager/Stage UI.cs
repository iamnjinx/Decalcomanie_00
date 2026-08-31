using System.Collections;
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
    private RectTransform objectiveObjRectTransform;
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

    void Awake()
    {
        usedTileDefaultColor = usedTileText.color;

        objectiveShowPosX = objectiveRectTransform.anchoredPosition.x;
        objectiveObjRectTransform = objectiveObj.GetComponent<RectTransform>();

        if (float.IsNaN(objectiveHoverPosX))
        {
            objectiveHoverPosX = Mathf.Lerp(objectiveHidePosX, objectiveShowPosX, 0.1f);
        }
    }

    void Start()
    {
        menuButton.OnSingleClick += () => SettingManager.Instance.OpenSetting();

        objectiveObj.OnSingleClick += ToggleObjectivePosition;
        objectiveObj.OnHoverEnter += HandleObjectiveHoverEnter;
        objectiveObj.OnHoverExit += HandleObjectiveHoverExit;

        undoButton.OnHoverEnter += HandleUndoHoverEnter;
        undoButton.OnHoverExit += HandleUndoHoverExit;

        flipHorizontalButton.OnHoverEnter += HandleFlipHorizontalHoverEnter;
        flipHorizontalButton.OnHoverExit += HandleFlipHorizontalHoverExit;
        flipVerticalButton.OnHoverEnter += HandleFlipVerticalHoverEnter;
        flipVerticalButton.OnHoverExit += HandleFlipVerticalHoverExit;

        LocalizedData undoData = GameManager.Instance.CurrentLocalizedData;
        undoInstructionText.text = undoData.undoText;
        undoInstructionText.font = undoData.fontAsset;
        flipHorizontalInstructionText.text = undoData.flipHorizontalText;
        flipHorizontalInstructionText.font = undoData.fontAsset;
        flipVerticalInstructionText.text = undoData.flipVerticalText;
        flipVerticalInstructionText.font = undoData.fontAsset;
    }

    private void OnDestroy()
    {
        if (undoButton != null)
        {
            undoButton.OnHoverEnter -= HandleUndoHoverEnter;
            undoButton.OnHoverExit -= HandleUndoHoverExit;
        }

        if (flipHorizontalButton != null)
        {
            flipHorizontalButton.OnHoverEnter -= HandleFlipHorizontalHoverEnter;
            flipHorizontalButton.OnHoverExit -= HandleFlipHorizontalHoverExit;
        }

        if (flipVerticalButton != null)
        {
            flipVerticalButton.OnHoverEnter -= HandleFlipVerticalHoverEnter;
            flipVerticalButton.OnHoverExit -= HandleFlipVerticalHoverExit;
        }

        if (objectiveObj == null) return;

        objectiveObj.OnSingleClick -= ToggleObjectivePosition;
        objectiveObj.OnHoverEnter -= HandleObjectiveHoverEnter;
        objectiveObj.OnHoverExit -= HandleObjectiveHoverExit;
    }

    private void HandleUndoHoverEnter()
    {
        undoInstruction.ShowUI();
    }

    private void HandleUndoHoverExit()
    {
        undoInstruction.HideUI();
    }

    private void HandleFlipHorizontalHoverEnter()
    {
        flipHorizontalInstruction.ShowUI();
    }

    private void HandleFlipHorizontalHoverExit()
    {
        flipHorizontalInstruction.HideUI();
    }

    private void HandleFlipVerticalHoverEnter()
    {
        flipVerticalInstruction.ShowUI();
    }

    private void HandleFlipVerticalHoverExit()
    {
        flipVerticalInstruction.HideUI();
    }

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
        float targetX = isObjectiveShown ? objectiveShowPosX : objectiveHidePosX;
        
        MoveObjectiveTo(targetX, duration);
        //objectiveObjRectTransform.DOLocalRotate(new Vector3(0f, 0f, 180f), objectiveMoveDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad);
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

    public void SetObjectiveTexts(GameLanguage language, bool isEarlyStage, int minMoves)
    {
        objectiveObjParent.SetActive(!isEarlyStage);

        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        objectiveTexts[0].text = data.stageClearText;
        objectiveTexts[1].text = data.starEarnedText;
        objectiveTexts[2].text = string.Format(data.starMovesFormat, minMoves);
        foreach (var text in objectiveTexts) text.font = data.fontAsset;

        currentMinMoves = minMoves;

        objectiveStarAchieved = false;
        lastUsedTileCount = 0;
        ApplyObjectiveStrike(0, false, true);
        ApplyObjectiveStrike(1, false, true);
        ApplyObjectiveStrike(2, false, true);
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
        Image image = objectiveStrikeThroughImages[index];
        if (image == null) return;

        image.DOKill();
        float target = achieved ? 1f : 0f;
        if (instant)
        {
            image.fillAmount = target;
        }
        else
        {
            image.DOFillAmount(target, objectiveStrikeFillDuration);
        }
    }

    public void SetAfterButtonTexts(GameLanguage language)
    {
        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        stageClearedAfterButtonTexts[0].text = data.nextStageText;
        stageClearedAfterButtonTexts[1].text = data.stageSelectionText;
        stageClearedAfterButtonTexts[2].text = data.restartStageText;
        foreach (var text in stageClearedAfterButtonTexts) text.font = data.fontAsset;
    }

    public void UpdateUsedTileText(int usedTileCount)
    {
        lastUsedTileCount = usedTileCount;

        usedTileText.text = usedTileCount.ToString() + "/" + currentMinMoves.ToString();
        //Debug.Log(currentMinMoves);
        if(currentMinMoves > 0)
            usedTileText.color = usedTileCount > currentMinMoves ? usedTileOverMinColor : usedTileDefaultColor;

        UpdateMinMovesObjectiveStrike();
    }

    public void SetPlatformerOnlyMode()
    {
        switchButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        undoButton.gameObject.SetActive(false);
        usedTileObj.SetActive(false);
    }

    public void SetEarlyStageUIVisibility(int stageID)
    {
        // STAGE 1-1 ~ 1-3(stageID 0~2)까지 Switch 버튼 숨김
        switchButton.gameObject.SetActive(stageID > 2);

        // STAGE 1-1 ~ 1-4(stageID 0~3)까지 Reset, 접기 버튼 숨김
        resetButton.gameObject.SetActive(stageID > 3);

        bool showFoldButtons = stageID > 3;
        flipHorizontalButton.gameObject.SetActive(showFoldButtons);
        flipVerticalButton.gameObject.SetActive(showFoldButtons);

        // STAGE 1-1 ~ 1-4(stageID 0~3)까지 현재 색칠 수 UI 숨김
        usedTileObj.SetActive(stageID > 3);
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

    public async UniTask ShowStageCleared(bool isCleared, bool obtainedStar, bool minMoves, int minMoveNum, bool isEarlyStage)
    {
        stageClearedStageSelectionButton.gameObject.SetActive(!isEarlyStage);
        stageClearedStageSelectionKeyUI.gameObject.SetActive(!isEarlyStage);
        stageClearedRestartStageButton.gameObject.SetActive(!isEarlyStage);
        stageClearedRestartStageKeyUI.gameObject.SetActive(!isEarlyStage);


        await stageClearedUI.ShowUI(1f);
        AudioManager.Instance.PlaySFX("Confetti");
        stageClearedConfettiObj.SetActive(true);

        await UniTask.Delay(500);

        if (!isEarlyStage)
        {
            LocalizedData data = GameManager.Instance.CurrentLocalizedData;
            stageClearedTexts[0].text = data.stageClearText;
            stageClearedTexts[1].text = data.starEarnedText;
            stageClearedTexts[2].text = string.Format(data.starMovesFormat, minMoveNum);
            foreach (var text in stageClearedTexts) text.font = data.fontAsset;
            achivementImage.sprite = data.achievementSprite;
            stageClearTextUI.HideUI(1f).Forget();
            await stageClearedAchievementUI.ShowUI(1f);

            await ShowAchivementStars(isCleared, obtainedStar, minMoves);   
        }

        // 화면 터치 시, 다음 스테이지로 넘어가게.
        await postStageUI.ShowUI(.5f);

        stageClearedNextStageButton.ShowUI();
        stageClearedNextStageKeyUI.ShowUI();
    }

    public async UniTask ShowAchivementStars(bool isCleared, bool obtainedStar, bool minMoves)
    {
        // Show stars based on achievements

        if (isCleared)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("stage_clear_star");
            stars[0].ShowUI(.1f).Forget();
        }
        if (obtainedStar)
        {
            stars[1].ShowUI(.1f).Forget();
        }
        if (minMoves)
        {
            stars[2].ShowUI(.1f).Forget();
        }

        if(isCleared || obtainedStar || minMoves)
        {
            await UniTask.Delay(1500); // Simulate delay for showing stars
        }

        // 스탬프 찍고 1.5초 대기.
        if (isCleared && obtainedStar && minMoves)
        {
            Stamp.ShowUI(.1f).Forget();
            Stamp.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f);
            await UniTask.Delay(1500);
        }
    }
}
