using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

public class StageUI : MonoBehaviour
{
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
    [SerializeField] private Sprite[] paintShortKeySprites; // 0: English, 1: Korean
    public Image platformerShortKeyImage;
    [SerializeField] private Sprite[] platformerShortKeySprites; // 0: English, 1: Korean


    [Header("Objectives")]
    // 0: Stage Clear (early stage only), 1: Star Earned, 2: Star & Min Moves
    public TextMeshProUGUI[] objectiveTexts = new TextMeshProUGUI[3];
    public GameObject objectiveObj;
    private static readonly string[] stageClearTexts = { "STAGE CLEAR!!", "스테이지 클리어!!" }; // 0: English, 1: Korean
    private static readonly string[] starEarnedTexts = { "STAR EARNED!!", "별 획득!!" }; // 0: English, 1: Korean
    private static readonly string[] starMovesFormats = { "STAR & Paint <= {0}", "별 & 이동 <= {0}" }; // 0: English, 1: Korean

    [Header("Stage Cleared UI")]
    public BaseUI stageClearedUI;
    public BaseUI stageClearedAchievementUI;
    public List<BaseUI> stars;
    public ButtonUI nextStageButton;
    public TextMeshProUGUI minMovesText;
    [Header("Guide UI")]
    public SpriteRenderer guideImage;

    void Start()
    {
        menuButton.OnSingleClick += () => SettingManager.Instance.OpenSetting();
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

        if (isPaint)
            paintShortKeyImage.sprite = paintShortKeySprites[(int)language];
        else
            platformerShortKeyImage.sprite = platformerShortKeySprites[(int)language];
    }

    public void SetObjectiveTexts(GameLanguage language, bool isEarlyStage, int minMoves)
    {
        objectiveObj.SetActive(!isEarlyStage);

        objectiveTexts[0].text = stageClearTexts[(int)language];
        objectiveTexts[1].text = starEarnedTexts[(int)language];
        objectiveTexts[2].text = string.Format(starMovesFormats[(int)language], minMoves);
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
            minMovesText.text = $"{minMoveNum}"; 
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
