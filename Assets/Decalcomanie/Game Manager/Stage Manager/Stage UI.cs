using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using Unity.Burst.CompilerServices;

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

    public Image paintShortKeyImage;
    public Image platformerShortKeyImage;


    [Header("Objectives")]
    // 0: Stage Clear (early stage only), 1: Star Earned, 2: Star & Min Moves
    public TextMeshProUGUI[] objectiveTexts = new TextMeshProUGUI[3];
    public GameObject objectiveObj;
    public TextMeshProUGUI usedTileText;
    public Color usedTileOverMinColor = Color.red;

    private Color usedTileDefaultColor;
    private int currentMinMoves;

    [Header("Stage Cleared UI")]
    public BaseUI stageClearedUI;
    public BaseUI stageClearedAchievementUI;
    public TextMeshProUGUI[] stageClearedTexts = new TextMeshProUGUI[3]; // 0: Stage Clear, 1: Star Earned, 2: Min Moves

    public Image achivementImage;
    public List<BaseUI> stars;
    public ButtonUI nextStageButton;
    public TextMeshProUGUI minMovesText;


    [Header("Guide UI")]
    public SpriteRenderer guideImage;

    void Awake()
    {
        usedTileDefaultColor = usedTileText.color;
    }

    void Start()
    {
        menuButton.OnSingleClick += () => SettingManager.Instance.OpenSetting();
    }

    public async void ShowMainUI(bool isPlatformerOnly = false)
    {
        if (!isPlatformerOnly)
        {
            await UniTask.Delay(1000);
        }
        foreach (var curtain in curtainUI)
        {
            curtain.ShowUI(.5f).Forget();
        }
        paintUI.ShowUI(.5f).Forget();
        await mainUI.ShowUI(.5f);
    }

    public void SetCurStageText(int stageID)
    {
        Material material = stageTitleText.fontMaterial;
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 1.2f); // 1보다 큰 값
        stageTitleText.text = $"STAGE {stageID / 10 + 1}-{stageID % 10 + 1}";
    }

    public void SetStageBackground(int stageID)
    {
        if (stageID < 0 || stageID >= stageBackgroundSprites.Count * 10) return; // stageID가 유효한 경우에만 배경을 설정
        stageMainBackgroundImage.sprite = stageBackgroundSprites[stageID / 10];
    }

    public void SetShortKeySprite(GameState gameState, GameLanguage language)
    {
        bool isPaint = gameState == GameState.Paint;
        paintShortKeyImage.gameObject.SetActive(isPaint);
        platformerShortKeyImage.gameObject.SetActive(!isPaint);

        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        if (isPaint)
            paintShortKeyImage.sprite = data.paintShortKeySprite;
        else
            platformerShortKeyImage.sprite = data.platformerShortKeySprite;
    }

    public void SetObjectiveTexts(GameLanguage language, bool isEarlyStage, int minMoves)
    {
        objectiveObj.SetActive(!isEarlyStage);

        LocalizedData data = GameManager.Instance.LocalizationData.GetData(language);
        objectiveTexts[0].text = data.stageClearText;
        objectiveTexts[1].text = data.starEarnedText;
        objectiveTexts[2].text = string.Format(data.starMovesFormat, minMoves);

        currentMinMoves = minMoves;
    }

    public void UpdateUsedTileText(int usedTileCount)
    {
        usedTileText.text = usedTileCount.ToString();
        usedTileText.color = usedTileCount > currentMinMoves ? usedTileOverMinColor : usedTileDefaultColor;
    }

    public void SetPlatformerOnlyMode()
    {
        switchButton.gameObject.SetActive(false);
        resetButton.gameObject.SetActive(false);
    }

    public async UniTask ShowStageCleared(bool isCleared, bool obtainedStar, bool minMoves, int minMoveNum, bool isEarlyStage)
    {
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
        nextStageButton.ShowUI();
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
