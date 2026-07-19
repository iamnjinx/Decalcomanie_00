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

    public ButtonUI switchButton;
    public ButtonUI resetButton;
    public ButtonUI undoButton;

    public ButtonUI flipHorizontalButton;
    public ButtonUI flipVerticalButton;

    [Header("Stage Main UI")]
    public List<Sprite> stageBackgroundSprites;
    public Image stageMainBackgroundImage;
    public TextMeshProUGUI stageTitleText;

    [Header("Objectives")]
    // 0: Stage Clear (early stage only), 1: Star Earned, 2: Star & Min Moves
    public GameObject objectiveObjParent;
    public TextMeshProUGUI[] objectiveTexts = new TextMeshProUGUI[3];
    public ButtonUI objectiveObj;
    public float objectiveShowPosX;
    public float objectiveMoveDuration = 0.3f;

    [SerializeField] private RectTransform objectiveRectTransform;
    private RectTransform objectiveObjRectTransform;
    private float objectiveHidePosX;
    private bool isObjectiveShown;

    public GameObject usedTileObj;
    public TextMeshProUGUI usedTileText;
    public Color usedTileOverMinColor = Color.red;

    private Color usedTileDefaultColor;
    private int currentMinMoves;

    [Header("Stage Cleared UI")]
    public BaseUI stageClearedUI;
    public BaseUI stageClearedAchievementUI;
    public TextMeshProUGUI[] stageClearedTexts = new TextMeshProUGUI[3]; // 0: Stage Clear, 1: Star Earned, 2: Min Moves

    public ButtonUI stageClearedNextStageButton;
    public ButtonUI stageClearedStageSelectionButton;
    public ButtonUI stageClearedRestartStageButton;
    public TextMeshProUGUI[] stageClearedAfterButtonTexts = new TextMeshProUGUI[3]; // 0: Next Stage, 1: Stage Selection, 2: Restart Stage

    public Image achivementImage;
    public List<BaseUI> stars;
    public ButtonUI nextStageButton;
    public TextMeshProUGUI minMovesText;

    public BaseUI postStageUI;

    [Header("Guide UI")]
    public SpriteRenderer guideImage;

    void Awake()
    {
        usedTileDefaultColor = usedTileText.color;

        objectiveHidePosX = objectiveRectTransform.anchoredPosition.x;
        objectiveObjRectTransform = objectiveObj.GetComponent<RectTransform>();
    }

    void Start()
    {
        menuButton.OnSingleClick += () => SettingManager.Instance.OpenSetting();

        objectiveObj.OnSingleClick += ToggleObjectivePosition;
    }

    private void ToggleObjectivePosition()
    {
        isObjectiveShown = !isObjectiveShown;
        float targetX = isObjectiveShown ? objectiveShowPosX : objectiveHidePosX;
        objectiveRectTransform.DOAnchorPosX(targetX, objectiveMoveDuration).SetEase(Ease.OutQuad);
        objectiveObjRectTransform.DOLocalRotate(new Vector3(0f, 0f, 180f), objectiveMoveDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad);
    }

    public async void ShowMainUI(bool isPlatformerOnly = false)
    {
        if (!isPlatformerOnly)
        {
            await UniTask.Delay(1000);
            paintUI.ShowUI(.5f).Forget();
            foreach (var curtain in curtainUI)
            {
                curtain.ShowUI(.5f).Forget();
            }
        }
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
        stageMainBackgroundImage.sprite = stageBackgroundSprites[stageID / 10];
    }

    public void SetObjectiveTexts(GameLanguage language, bool isEarlyStage, int minMoves)
    {
        objectiveObjParent.SetActive(!isEarlyStage);

        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        objectiveTexts[0].text = data.stageClearText;
        objectiveTexts[1].text = data.starEarnedText;
        objectiveTexts[2].text = string.Format(data.starMovesFormat, minMoves);

        currentMinMoves = minMoves;
    }

    public void SetAfterButtonTexts(GameLanguage language)
    {
        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        stageClearedAfterButtonTexts[0].text = data.nextStageText;
        stageClearedAfterButtonTexts[1].text = data.stageSelectionText;
        stageClearedAfterButtonTexts[2].text = data.restartStageText;
    }

    public void UpdateUsedTileText(int usedTileCount)
    {
        usedTileText.text = usedTileCount.ToString() + "/" + currentMinMoves.ToString();
        Debug.Log(currentMinMoves);
        if(currentMinMoves > 0)
            usedTileText.color = usedTileCount > currentMinMoves ? usedTileOverMinColor : usedTileDefaultColor;
    }

    public void SetPlatformerOnlyMode()
    {
        switchButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
        undoButton.gameObject.SetActive(false);
        usedTileObj.SetActive(false);
    }

    public async UniTask ShowStageCleared(bool isCleared, bool obtainedStar, bool minMoves, int minMoveNum, bool isEarlyStage)
    {
        stageClearedStageSelectionButton.gameObject.SetActive(!isEarlyStage);
        stageClearedRestartStageButton.gameObject.SetActive(!isEarlyStage);

        await stageClearedUI.ShowUI(1f);        

        await UniTask.Delay(1000);

        if (!isEarlyStage)
        {
            LocalizedData data = GameManager.Instance.CurrentLocalizedData;
            stageClearedTexts[0].text = data.stageClearText;
            stageClearedTexts[1].text = data.starEarnedText;
            stageClearedTexts[2].text = string.Format(data.starMovesFormat, minMoveNum);
            achivementImage.sprite = data.achievementSprite;
            await stageClearedAchievementUI.ShowUI(1f);

            await ShowAchivementStars(isCleared, obtainedStar, minMoves);   
        }

        // 화면 터치 시, 다음 스테이지로 넘어가게.
        await postStageUI.ShowUI(.5f);

        stageClearedNextStageButton.ShowUI();
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
    }
}
