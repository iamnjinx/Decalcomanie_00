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
    public Image objectivePanelImage;
    public Sprite[] languageObjectivePanelSprites; // 0: English, 1: Korean
    public TextMeshProUGUI objectiveNumText;
    public float[] objectiveNumTextXPos;

    [Header("Stage Cleared UI")]
    public BaseUI stageClearedUI;
    public List<BaseUI> stars;
    public ButtonUI nextStageButton;
    public TextMeshProUGUI minMovesText;

    void Start()
    {
        menuButton.OnSingleClick += () => SettingManager.Instance.OpenSetting();
    }

    public void SetCurStageText(int stageID)
    {
        Material material = stageTitleText.fontMaterial;
        material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 1.2f); // 1보다 큰 값
        stageTitleText.text = $"STAGE {stageID}";
    }

    public void SetStageBackground(int stageID)
    {
        stageMainBackgroundImage.sprite = stageBackgroundSprites[(stageID+9) / 10];
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

    public void SetObjectivePanel(GameLanguage language)
    {
        objectivePanelImage.sprite = languageObjectivePanelSprites[(int)language];
        // Adjust the position of the objective number text based on the language
        objectiveNumText.rectTransform.anchoredPosition = new Vector2(
            objectiveNumTextXPos[(int)language],
            objectiveNumText.rectTransform.anchoredPosition.y
        );
    }

    public async UniTask ShowStageCleared(bool isCleared, bool obtainedStar, bool minMoves, int minMoveNum)
    {
        minMovesText.text = $"{minMoveNum}"; 

        await UniTask.Delay(1000);

        await stageClearedUI.ShowUI(1f);

        await ShowAchivementStars(isCleared, obtainedStar, minMoves);


        // 화면 터치 시, 다음 스테이지로 넘어가게.
        nextStageButton.ShowUI();
    }

    public async UniTask ShowAchivementStars(bool isCleared, bool obtainedStar, bool minMoves)
    {
        // Show stars based on achievements

        if (isCleared)
        {
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
