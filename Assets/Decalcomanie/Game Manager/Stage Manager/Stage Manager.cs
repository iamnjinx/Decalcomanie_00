using UnityEngine;

public class StageManager : MonoBehaviour
{
    private GameState currentGameState = GameState.Paint;

    [SerializeField] BoardManager boardManager;
    [SerializeField] PaintManager paintManager;
    [SerializeField] PlatformerManager platformerManager;
    [SerializeField] private TextAsset testBoardDataTextAsset;

    [SerializeField] private StageUI stageUI;
    [SerializeField] private TutorialManager tutorialManager;

    private Achievements achievements = new Achievements(false, false, false);

    void Awake()
    {
        stageUI.nextStageButton.OnSingleClick += () => MoveToNextStage();

        stageUI.switchButton.OnSingleClick += () => SwitchState();
        stageUI.resetButton.OnSingleClick += () => ResetButton();

        stageUI.flipHorizontalButton.OnSingleClick += () => paintManager.FoldHorizontal();
        stageUI.flipVerticalButton.OnSingleClick += () => paintManager.FoldVertical();
    }

    void Start()
    {
        if(GameManager.Instance == null || GameManager.Instance.GetCurrentStageAsset() == null)
        {
            boardManager.CreateBoard(testBoardDataTextAsset);
        }
        else
        {
            boardManager.CreateBoard(GameManager.Instance.GetCurrentStageAsset());
            stageUI.SetStageBackground(GameManager.Instance.CurrentStageIndex);
        }
        stageUI.objectiveNumText.text = $"{boardManager.CurrentBoard.BoardData.minMoves}";
        var language = GameManager.Instance != null ? GameManager.Instance.CurrentLanguage : GameLanguage.English;
        stageUI.SetObjectivePanel(language);
        stageUI.SetShortKeySprite(currentGameState, language);
        paintManager.CreateTileControllers();

        if (GameManager.Instance != null && GameManager.Instance.CurrentStageIndex == 0)
            tutorialManager.StartTutorial();
    }

    public void ChangeGameState(GameState newGameState)
    {
        currentGameState = newGameState;
        stageUI.SetShortKeySprite(currentGameState, GameManager.Instance != null ? GameManager.Instance.CurrentLanguage : GameLanguage.English);

        switch (currentGameState)
        {
            case GameState.Paint:
                ResetState();
                break;
            case GameState.Platformer:
                paintManager.PausePaint();
                platformerManager.SetPlatformerObjects(boardManager.CurrentBoard);

                platformerManager.OnCleared += GameCleared;
                break;
            case GameState.End:
                break;
        }
    }

    void Update()
    {
        if (currentGameState == GameState.End) return;

        if (Input.GetKeyDown(KeyCode.R))         ResetButton();
        if (Input.GetKeyDown(KeyCode.Tab))       SwitchState();
        if (currentGameState == GameState.Paint)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) paintManager.FoldHorizontal();
            if (Input.GetKeyDown(KeyCode.Alpha2)) paintManager.FoldVertical();
        }
    }

    public void SwitchState()
    {
        if (paintManager.IsFolding) return;
        if (currentGameState == GameState.Paint)
            ChangeGameState(GameState.Platformer);
        else if (currentGameState == GameState.Platformer)
            ChangeGameState(GameState.Paint);
    }

    private void ResetState()
    {
        platformerManager.OnCleared -= GameCleared;
        paintManager.ResumePaint();
        platformerManager.ResetObjects();

        achievements = new Achievements(false, false, false);
    }

    public void ResetButton()
    {
        if (paintManager.IsFolding) return;
        if (currentGameState == GameState.Platformer)
        {
            ChangeGameState(GameState.Paint);
        }
        else
        {
            ResetState();
        }
        paintManager.ResetPaint();
    }

    public void GameCleared()
    {
        if (currentGameState == GameState.End) return;
        AudioManager.Instance.PlaySFX("stage_clear");
        ChangeGameState(GameState.End);
        Debug.Log($"Game Cleared!");

        achievements = new Achievements(true, platformerManager.obtainedStar, paintManager.paintCount <= boardManager.CurrentBoard.BoardData.minMoves);

        SaveProgress();

        // 화면 어두워지고 클리어 UI.
        stageUI.ShowStageCleared(achievements.Is_Cleared, achievements.ObtainedStar, achievements.Min_Moves, boardManager.CurrentBoard.BoardData.minMoves);
    }

    private void SaveProgress()
    {
        if (GameManager.Instance == null || SaveManager.Instance == null) return;

        var progress = SaveManager.Instance.Load<GameProgressData>(GameProgressData.SaveKey, new GameProgressData());
        progress.RecordStageCleared(
            GameManager.Instance.CurrentStageIndex,
            achievements.ObtainedStar,
            achievements.Min_Moves);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);
    }

    public void MoveToNextStage()
    {
        // 다음 스테이지로 이동.
        Debug.Log($"Move To Next Stage!");
        GameManager.Instance.LoadStage(GameManager.Instance.CurrentStageIndex + 1);
    }
}

public enum GameState
    {
        Paint, Platformer, End
    }

    public class Achievements
    {
        public bool Is_Cleared { get; private set; }
        public bool ObtainedStar { get; private set; }
        public bool Min_Moves { get; private set; }

        public Achievements(bool isCleared, bool obtainedStar, bool minMoves)
        {
            Is_Cleared = isCleared;
            ObtainedStar = obtainedStar;
            Min_Moves = minMoves;
        }
    }


