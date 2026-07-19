using System.Collections.Generic;
using UnityEngine;
using TarodevController;

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
        //stageUI.nextStageButton.OnSingleClick += () => MoveToNextStage();

        stageUI.stageClearedNextStageButton.OnSingleClick += () => MoveToNextStage();
        stageUI.stageClearedStageSelectionButton.OnSingleClick += () => GoToStageSelection();
        stageUI.stageClearedRestartStageButton.OnSingleClick += () => RestartStage();

        stageUI.switchButton.OnSingleClick += () => SwitchState();
        stageUI.resetButton.OnSingleClick += () => ResetButton();
        stageUI.undoButton.OnSingleClick += () => paintManager.UndoPaintAction();

        stageUI.flipHorizontalButton.OnSingleClick += () => paintManager.FoldHorizontal();
        stageUI.flipVerticalButton.OnSingleClick += () => paintManager.FoldVertical();

        paintManager.OnPaintCountChanged += stageUI.UpdateUsedTileText;
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
        stageUI.SetAfterButtonTexts(language);
        stageUI.UpdateUsedTileText(paintManager.paintCount);
        stageUI.SetCurStageText(GameManager.Instance != null ? GameManager.Instance.CurrentStageIndex : 0);
        stageUI.ShowMainUI(isPlatformerOnly);
        paintManager.CreateTileControllers();

        // if (GameManager.Instance != null && GameManager.Instance.CurrentStageIndex == 0)
        //     tutorialManager.StartTutorial();
    }

    public void ChangeGameState(GameState newGameState)
    {
        currentGameState = newGameState;
        OnGameStateChanged?.Invoke(currentGameState);

        stageUI.mobileControlButtonContainer.SetActive(Application.isMobilePlatform && currentGameState == GameState.Platformer);

        switch (currentGameState)
        {
            case GameState.Paint:
                stageUI.guideImage.gameObject.layer = LayerMask.NameToLayer("Guide");
                ResetState();
                if (!isPlatformerOnly)
                {
                    stageUI.undoButton.gameObject.SetActive(true);
                    stageUI.resetButton.gameObject.SetActive(true);
                }
                break;
            case GameState.Platformer:
                stageUI.guideImage.gameObject.layer = LayerMask.NameToLayer("GuideTile");
                paintManager.PausePaint();
                platformerManager.SetPlatformerObjects(boardManager.CurrentBoard);

                platformerManager.OnCleared += GameCleared;
                platformerManager.OnFellIntoHole += OnPlayerFellIntoHole;
                if (!isPlatformerOnly)
                {
                    stageUI.undoButton.gameObject.SetActive(false);
                    stageUI.resetButton.gameObject.SetActive(false);
                }
                break;
            case GameState.End:
                break;
        }
    }

    void Update()
    {
        if (currentGameState == GameState.End)
        {
            if (!isEarlyStage && Input.GetKeyDown(KeyCode.Escape)) GoToStageSelection();
            if (Input.GetKeyDown(KeyCode.Space))                  MoveToNextStage();
            if (!isEarlyStage && Input.GetKeyDown(KeyCode.R))     RestartStage();
            return;
        }
        if(tutorialManager != null && tutorialManager.CurTutoID != -1) return;

        if (Input.GetKeyDown(KeyCode.R))   { if (!isPlatformerOnly) ResetButton(); }
        if (Input.GetKeyDown(KeyCode.Tab)) { if (!isPlatformerOnly) SwitchState(); }

        if (currentGameState == GameState.Paint)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) paintManager.FoldVertical();
            if (Input.GetKeyDown(KeyCode.Alpha2)) paintManager.FoldHorizontal();
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Z)) paintManager.UndoPaintAction();
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

    // 모바일 조작 버튼(leftButton/rightButton/upButton)의 EventTrigger(PointerDown/PointerUp)에서 호출합니다.
    // 플레이어는 스테이지마다 새로 Instantiate되므로 PlayerControllerT.Current로 현재 플레이어를 찾아갑니다.
    public void OnMobileLeftDown() { if (PlayerControllerT.Current != null) PlayerControllerT.Current.OnMobileLeftDown(); }
    public void OnMobileLeftUp() { if (PlayerControllerT.Current != null) PlayerControllerT.Current.OnMobileLeftUp(); }
    public void OnMobileRightDown() { if (PlayerControllerT.Current != null) PlayerControllerT.Current.OnMobileRightDown(); }
    public void OnMobileRightUp() { if (PlayerControllerT.Current != null) PlayerControllerT.Current.OnMobileRightUp(); }
    public void OnMobileJumpDown() { if (PlayerControllerT.Current != null) PlayerControllerT.Current.OnMobileJumpDown(); }
    public void OnMobileJumpUp() { if (PlayerControllerT.Current != null) PlayerControllerT.Current.OnMobileJumpUp(); }

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

    public void RestartStage()
    {
        if (is_ready_for_next_stage)
        {
            is_ready_for_next_stage = false;
            // 현재 스테이지 다시 시작.
            GameManager.Instance.LoadStage(GameManager.Instance.CurrentStageIndex);
        }
    }

    public void GoToStageSelection()
    {
        if (is_ready_for_next_stage)
        {
            is_ready_for_next_stage = false;
            // 스테이지 선택 화면으로 이동.
            GameManager.Instance.LoadSelectScene();
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


