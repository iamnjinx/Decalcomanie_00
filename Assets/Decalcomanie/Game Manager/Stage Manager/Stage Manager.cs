using System.Collections.Generic;
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

    private bool is_ready_for_next_stage = false;
    private bool isPlatformerOnly = false;
    private bool isEarlyStage = false;

    public event System.Action<GameState> OnGameStateChanged;

    void Awake()
    {
        stageUI.nextStageButton.OnSingleClick += () => MoveToNextStage();

        stageUI.switchButton.OnSingleClick += () => SwitchState();
        stageUI.resetButton.OnSingleClick += () => ResetButton();
        stageUI.undoButton.OnSingleClick += () => paintManager.UndoPaintAction();

        stageUI.flipHorizontalButton.OnSingleClick += () => paintManager.FoldHorizontal();
        stageUI.flipVerticalButton.OnSingleClick += () => paintManager.FoldVertical();
    }

    void Start()
    {
        if (EditorManager.SavedBoard != null)
        {
            boardManager.CreateBoard(ConvertEditorBoardToBoardData(EditorManager.SavedBoard));
        }
        else if(GameManager.Instance == null || GameManager.Instance.GetCurrentStageAsset() == null)
        {
            boardManager.CreateBoard(testBoardDataTextAsset);
        }
        else
        {
            boardManager.CreateBoard(GameManager.Instance.GetCurrentStageAsset());
            stageUI.SetStageBackground(GameManager.Instance.CurrentStageIndex);
            if(GameManager.Instance.CurrentStageIndex < 5)
            {
                stageUI.guideImage.sprite = GameManager.Instance.GameData.guideSprites[GameManager.Instance.CurrentStageIndex];
                //guideimage 오브젝트 sorting layer를 early stage일떄 아닐떄 다르게 설정
                stageUI.guideImage.gameObject.layer = LayerMask.NameToLayer(GameManager.Instance.CurrentStageIndex < 2 ? "Tile" : "Guide");
                stageUI.guideImage.gameObject.SetActive(true);
            }
            else
            {
                stageUI.guideImage.gameObject.SetActive(false);
            }
        }
        isPlatformerOnly = GameManager.Instance != null && GameManager.Instance.CurrentStageIndex < 2;
        if (isPlatformerOnly)
        {
            stageUI.SetPlatformerOnlyMode();
            ChangeGameState(GameState.Platformer);
        }

        isEarlyStage = GameManager.Instance != null && GameManager.Instance.CurrentStageIndex <= 3;
        var language = GameManager.Instance != null ? GameManager.Instance.CurrentLanguage : GameLanguage.English;
        stageUI.SetObjectiveTexts(language, isEarlyStage, boardManager.CurrentBoard.BoardData.minMoves);
        stageUI.SetShortKeySprite(currentGameState, language);
        stageUI.SetCurStageText(GameManager.Instance != null ? GameManager.Instance.CurrentStageIndex : 0);
        paintManager.CreateTileControllers();

        // if (GameManager.Instance != null && GameManager.Instance.CurrentStageIndex == 0)
        //     tutorialManager.StartTutorial();
    }

    public void ChangeGameState(GameState newGameState)
    {
        currentGameState = newGameState;
        stageUI.SetShortKeySprite(currentGameState, GameManager.Instance != null ? GameManager.Instance.CurrentLanguage : GameLanguage.English);
        OnGameStateChanged?.Invoke(currentGameState);

        switch (currentGameState)
        {
            case GameState.Paint:
                ResetState();
                break;
            case GameState.Platformer:
                paintManager.PausePaint();
                platformerManager.SetPlatformerObjects(boardManager.CurrentBoard);

                platformerManager.OnCleared += GameCleared;
                platformerManager.OnFellIntoHole += OnPlayerFellIntoHole;
                break;
            case GameState.End:
                break;
        }
    }

    void Update()
    {
        if (currentGameState == GameState.End)
        {
            if(Input.anyKeyDown)
            {
                MoveToNextStage();
            }
            return;
        }
        if(tutorialManager != null && tutorialManager.CurTutoID != -1) return;

        if (Input.GetKeyDown(KeyCode.R))   { if (!isPlatformerOnly) ResetButton(); }
        if (Input.GetKeyDown(KeyCode.Tab)) { if (!isPlatformerOnly) SwitchState(); }

        if (currentGameState == GameState.Paint)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) paintManager.FoldHorizontal();
            if (Input.GetKeyDown(KeyCode.Alpha2)) paintManager.FoldVertical();
            if (Input.GetMouseButtonDown(1)) paintManager.UndoPaintAction();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SettingManager.Instance.SetSettingUI();
        }
    }

    public void SwitchState()
    {
        if (isPlatformerOnly) return;
        if (paintManager.IsFolding) return;
        if (currentGameState == GameState.Paint)
            ChangeGameState(GameState.Platformer);
        else if (currentGameState == GameState.Platformer)
            ChangeGameState(GameState.Paint);
    }

    private void OnPlayerFellIntoHole() => ChangeGameState(GameState.Paint);

    private void ResetState()
    {
        platformerManager.OnCleared -= GameCleared;
        platformerManager.OnFellIntoHole -= OnPlayerFellIntoHole;
        paintManager.ResumePaint();
        platformerManager.ResetObjects();

        achievements = new Achievements(false, false, false);
    }

    public void ResetButton()
    {
        if (isPlatformerOnly) return;
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

    public async void GameCleared()
    {
        if (currentGameState == GameState.End) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("stage_clear");
        ChangeGameState(GameState.End);

        achievements = new Achievements(
            true,
            isEarlyStage || platformerManager.obtainedStar,
            isEarlyStage || paintManager.paintCount <= boardManager.CurrentBoard.BoardData.minMoves);

        SaveProgress();

        // 화면 어두워지고 클리어 UI.
        await stageUI.ShowStageCleared(achievements.Is_Cleared, achievements.ObtainedStar, achievements.Min_Moves, boardManager.CurrentBoard.BoardData.minMoves, isEarlyStage);
        is_ready_for_next_stage = true;
    }

    private void SaveProgress()
    {
        if (GameManager.Instance == null || SaveManager.Instance == null) return;

        var progress = GameProgressData.Load();
        progress.RecordStageCleared(
            GameManager.Instance.CurrentStageIndex,
            achievements.ObtainedStar,
            achievements.Min_Moves,
            platformerManager.obtainedStar);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);
    }

    private static BoardData ConvertEditorBoardToBoardData(TileType[] board)
    {
        const int size = 8;
        Vector2 ToVec(int idx) => new Vector2(idx % size + 1, idx / size + 1);

        int startIdx = System.Array.IndexOf(board, TileType.Start);
        int endIdx   = System.Array.IndexOf(board, TileType.End);
        int starIdx  = System.Array.IndexOf(board, TileType.Star);
        int keyIdx   = System.Array.IndexOf(board, TileType.Key);

        var fixedPoints = new List<Vector2>();
        var holePoints  = new List<Vector2>();
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == TileType.Fixed) fixedPoints.Add(ToVec(i));
            if (board[i] == TileType.Hole)  holePoints.Add(ToVec(i));
        }

        return new BoardData(size, ToVec(startIdx), ToVec(endIdx), ToVec(starIdx), ToVec(keyIdx), fixedPoints, holePoints);
    }

    public void MoveToNextStage()
    {
        if (is_ready_for_next_stage)
        {
            is_ready_for_next_stage = false;
            // 다음 스테이지로 이동.
            Debug.Log($"Move To Next Stage!");
            GameManager.Instance.LoadStage(GameManager.Instance.CurrentStageIndex + 1);   
        }
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


