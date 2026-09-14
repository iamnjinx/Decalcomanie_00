using System.Collections.Generic;
using UnityEngine;
using TarodevController;
using Cysharp.Threading.Tasks;

public class StageManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] BoardManager boardManager;
    [SerializeField] PaintManager paintManager;
    [SerializeField] PlatformerManager platformerManager;
    [SerializeField] private StageUI stageUI;
    [SerializeField] private HintManager hintManager;

    [Header("Fallback")]
    [SerializeField] private TextAsset testBoardDataTextAsset; // GameManager 없이 씬을 단독 실행할 때 쓰는 보드

    [SerializeField] private float foldButtonFadeTime = 0.2f; // 접기 회전(foldTime)보다 빠르게 버튼을 숨기고 보여주기 위한 시간

    private GameState currentGameState = GameState.Paint;
    public GameState CurrentGameState => currentGameState;

    private int stageIndex;
    private Achievements achievements = Achievements.None;

    private bool isReadyForNextStage = false;
    private bool isPlatformerOnly = false;
    private bool isEarlyStage = false;

    private bool isSwitchUnlocked = false;
    private bool isResetUnlocked = false;
    private bool isFoldUnlocked = false;

    // 리셋 튜토리얼을 띄운 뒤 아직 리셋을 눌러보지 않은 상태.
    // 리셋 버튼은 Paint에서만 보이므로, 이 동안에는 튜토리얼도 상태 전환을 따라 같이 켜고 끕니다.
    private bool isResetTutorialPending = false;

    private int MinMoves => boardManager.CurrentBoard.BoardData.minMoves;

    public event System.Action<GameState> OnGameStateChanged;

    #region Lifecycle

    void Awake()
    {
        SubscribeEvents();
    }

    void Start()
    {
        stageIndex = GameManager.Instance != null ? GameManager.Instance.CurrentStageIndex : 0;

        CreateBoard();
        CreateStageObjects();
        ApplyStageRules();
        InitializeUI();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    #endregion

    #region Setup

    // 버튼과 매니저 이벤트를 전부 이름 있는 핸들러로 연결합니다.
    // (람다로 걸면 OnDestroy에서 해제할 수 없어 씬을 오갈 때 구독이 남습니다.)
    private void SubscribeEvents()
    {
        stageUI.stageClearedNextStageButton.OnSingleClick += MoveToNextStage;
        stageUI.stageClearedStageSelectionButton.OnSingleClick += GoToStageSelection;
        stageUI.stageClearedRestartStageButton.OnSingleClick += RestartStage;

        stageUI.switchButton.OnSingleClick += HandleSwitchButtonTriggered;
        stageUI.resetButton.OnSingleClick += HandleResetButtonTriggered;
        stageUI.undoButton.OnSingleClick += HandleUndoButtonTriggered;

        stageUI.flipHorizontalButton.OnSingleClick += HandleFlipHorizontalTriggered;
        stageUI.flipVerticalButton.OnSingleClick += HandleFlipVerticalTriggered;

        stageUI.OnStampStamped += HandleStampStamped;

        paintManager.OnFoldStarted += HandleFoldStarted;
        paintManager.OnFoldReturning += HandleFoldReturning;
        paintManager.OnPaintCountChanged += stageUI.UpdateUsedTileText;

        // 플랫포머 이벤트는 모드가 바뀔 때마다 붙였다 떼지 않고 여기서 한 번만 연결하고,
        // 핸들러 안에서 현재 상태로 걸러냅니다.
        platformerManager.OnStarObtained += HandleStarObtained;
        platformerManager.OnCleared += GameCleared;
        platformerManager.OnFellIntoHole += HandlePlayerFellIntoHole;
    }

    private void UnsubscribeEvents()
    {
        if (stageUI != null)
        {
            stageUI.stageClearedNextStageButton.OnSingleClick -= MoveToNextStage;
            stageUI.stageClearedStageSelectionButton.OnSingleClick -= GoToStageSelection;
            stageUI.stageClearedRestartStageButton.OnSingleClick -= RestartStage;

            stageUI.switchButton.OnSingleClick -= HandleSwitchButtonTriggered;
            stageUI.resetButton.OnSingleClick -= HandleResetButtonTriggered;
            stageUI.undoButton.OnSingleClick -= HandleUndoButtonTriggered;

            stageUI.flipHorizontalButton.OnSingleClick -= HandleFlipHorizontalTriggered;
            stageUI.flipVerticalButton.OnSingleClick -= HandleFlipVerticalTriggered;

            stageUI.OnStampStamped -= HandleStampStamped;
        }

        if (paintManager != null)
        {
            paintManager.OnFoldStarted -= HandleFoldStarted;
            paintManager.OnFoldReturning -= HandleFoldReturning;
            paintManager.OnPaintCountChanged -= stageUI.UpdateUsedTileText;
            paintManager.OnTilePainted -= HandleSwitchTutorialTilePainted;
            paintManager.OnTilePainted -= HandleFoldTutorialTilePainted;
            paintManager.OnFolded -= HandleFoldTutorialFolded;
        }

        if (platformerManager != null)
        {
            platformerManager.OnStarObtained -= HandleStarObtained;
            platformerManager.OnCleared -= GameCleared;
            platformerManager.OnFellIntoHole -= HandlePlayerFellIntoHole;
        }
    }

    private void CreateBoard()
    {
        GameManager gm = GameManager.Instance;

        if (EditorManager.SavedBoard != null)
        {
            boardManager.CreateBoard(ConvertEditorBoardToBoardData(EditorManager.SavedBoard));
            return;
        }

        if (gm == null || gm.GetCurrentStageAsset() == null)
        {
            boardManager.CreateBoard(testBoardDataTextAsset);
            return;
        }

        boardManager.CreateBoard(gm.GetCurrentStageAsset());
        stageUI.SetStageBackground(stageIndex);
        ShowGuideImage(gm);
    }

    private void ShowGuideImage(GameManager gm)
    {
        if (StageRules.HasGuideImage(stageIndex))
            stageUI.guideImage.sprite = gm.GameData.guideSprites[stageIndex];
        UpdateGuideVisibility();
    }

    // 가이드가 필요한 스테이지(1-1~1-3)에서만, 그 스테이지가 지정한 모드일 때만 켭니다.
    // 1-1~1-2는 Platformer에서, 1-3은 Paint에서 노출합니다.
    private void UpdateGuideVisibility()
    {
        bool show = StageRules.HasGuideImage(stageIndex)
                 && (currentGameState == GameState.Paint) == StageRules.IsGuideShownInPaint(stageIndex);
        stageUI.guideImage.gameObject.SetActive(show);
    }

    // Door/Star/Key/Player는 스테이지 전체에서 재사용되므로,
    // 첫 모드 전환보다 먼저 한 번만 만들어 둡니다. (Paint 모드에서는 숨겨집니다)
    private void CreateStageObjects()
    {
        paintManager.CreateTileControllers();
        platformerManager.CreateBoardObjects(boardManager.CurrentBoard);
    }

    private void ApplyStageRules()
    {
        bool hasGameManager = GameManager.Instance != null;

        isPlatformerOnly = hasGameManager && StageRules.IsPlatformerOnly(stageIndex);
        isEarlyStage = hasGameManager && StageRules.IsEarlyStage(stageIndex);
        isSwitchUnlocked = StageRules.IsSwitchUnlocked(stageIndex);
        isResetUnlocked = StageRules.IsResetUnlocked(stageIndex);
        isFoldUnlocked = StageRules.IsFoldUnlocked(stageIndex);

        if (isPlatformerOnly)
        {
            stageUI.SetPlatformerOnlyMode();
            ChangeGameState(GameState.Platformer);
        }
    }

    private void InitializeUI()
    {
        GameLanguage language = GameManager.Instance != null ? GameManager.Instance.CurrentLanguage : GameLanguage.English;

        stageUI.SetEarlyStageUIVisibility(stageIndex);
        SetupPaintButtonTutorials();
        stageUI.SetObjectiveTexts(language, isEarlyStage, MinMoves);
        stageUI.SetAfterButtonTexts(language);
        stageUI.UpdateUsedTileText(paintManager.PaintCount);
        stageUI.SetCurStageText(stageIndex);
        stageUI.ShowMainUI(isPlatformerOnly);
    }

    #endregion

    #region Early stage tutorials

    // 1-3, 1-4의 튜토리얼성 조기 버튼 활성화 이벤트를 등록합니다.
    private void SetupPaintButtonTutorials()
    {
        if (StageRules.IsSwitchTutorialStage(stageIndex))
        {
            stageUI.clickTutorialUI.ShowUI();
            paintManager.OnTilePainted += HandleSwitchTutorialTilePainted;
        }

        if (StageRules.IsFoldTutorialStage(stageIndex))
        {
            paintManager.OnTilePainted += HandleFoldTutorialTilePainted;
            paintManager.OnFolded += HandleFoldTutorialFolded;
        }
    }

    private void HandleSwitchTutorialTilePainted(int tileID)
    {
        Vector2Int coord = StageRules.SwitchTutorialTileCoord;
        int targetTileID = Board.CoordToIndex(coord.x, coord.y, boardManager.CurrentBoard.size);
        if (tileID != targetTileID) return;

        paintManager.OnTilePainted -= HandleSwitchTutorialTilePainted;

        stageUI.clickTutorialUI.HideUI(.3f).Forget();
        UnlockSwitch();
        stageUI.switchTutorialUI.ShowUI();
    }

    private void HandleFoldTutorialTilePainted(int tileID)
    {
        paintManager.OnTilePainted -= HandleFoldTutorialTilePainted;
        UnlockFold();
    }

    private void HandleFoldTutorialFolded()
    {
        paintManager.OnFolded -= HandleFoldTutorialFolded;

        UnlockReset();
        isResetTutorialPending = true;
        stageUI.resetTutorialUI.ShowUI();
    }

    // 조기 해제는 항상 "플래그 세우기 + 버튼 켜기 + 강조"가 한 묶음입니다.
    private void UnlockSwitch()
    {
        isSwitchUnlocked = true;
        stageUI.SetSwitchButtonActive(true);
        stageUI.HighlightSwitchButton();
    }

    private void UnlockReset()
    {
        isResetUnlocked = true;
        stageUI.SetResetButtonActive(true);
        stageUI.HighlightResetButton();
    }

    private void UnlockFold()
    {
        isFoldUnlocked = true;
        stageUI.SetFoldButtonsActive(true);
        stageUI.HighlightFoldButtons();
    }

    #endregion

    #region Game state

    public void ChangeGameState(GameState newGameState)
    {
        currentGameState = newGameState;
        OnGameStateChanged?.Invoke(currentGameState);

        stageUI.mobileControlButtonContainer.SetActive(Application.isMobilePlatform && currentGameState == GameState.Platformer);

        switch (currentGameState)
        {
            case GameState.Paint:
                EnterPaintState();
                break;
            case GameState.Platformer:
                EnterPlatformerState();
                break;
            case GameState.End:
                break;
        }
    }

    private void EnterPaintState()
    {
        UpdateGuideVisibility();
        stageUI.HideObjectiveIfShown();
        paintManager.SetPaperBoardSurfaceTransparent(false);
        ResetState();

        if (isPlatformerOnly) return;

        stageUI.undoButton.gameObject.SetActive(true);
        stageUI.SetResetButtonActive(isResetUnlocked);
        stageUI.flipHorizontalButton.ShowUI();
        stageUI.flipVerticalButton.ShowUI();

        if (isResetTutorialPending) stageUI.resetTutorialUI.ShowUI();
    }

    private void EnterPlatformerState()
    {
        UpdateGuideVisibility();
        stageUI.ShowObjectiveIfHidden();
        paintManager.SetPaperBoardSurfaceTransparent(true);
        paintManager.PausePaint();
        platformerManager.SetPlatformerObjects();

        if (isPlatformerOnly) return;

        stageUI.undoButton.gameObject.SetActive(false);
        stageUI.SetResetButtonActive(false);
        stageUI.flipHorizontalButton.HideUI();
        stageUI.flipVerticalButton.HideUI();

        stageUI.resetTutorialUI.HideUI();
    }

    public void SwitchState()
    {
        if (isPlatformerOnly || !isSwitchUnlocked) return;
        if (paintManager.IsFolding) return;

        if (currentGameState == GameState.Paint)
            ChangeGameState(GameState.Platformer);
        else if (currentGameState == GameState.Platformer)
            ChangeGameState(GameState.Paint);
    }

    public void ResetButton()
    {
        if (isPlatformerOnly || !isResetUnlocked) return;
        if (paintManager.IsFolding) return;

        if (currentGameState == GameState.Platformer)
            ChangeGameState(GameState.Paint);
        else
            ResetState();

        paintManager.ResetPaint();
    }

    private void ResetState()
    {
        paintManager.ResumePaint();
        platformerManager.ResetObjects();
        stageUI.SetObjectiveStarObtained(false);

        achievements = Achievements.None;
    }

    #endregion

    #region Input

    void Update()
    {
        if (currentGameState == GameState.End)
        {
            HandleStageEndInput();
            return;
        }

        HandlePlayInput();
    }

    private void HandleStageEndInput()
    {
        if (!isEarlyStage && GameInput.BackPressed) GoToStageSelection();
        if (GameInput.ProceedPressed) MoveToNextStage();
    }

    private void HandlePlayInput()
    {
        if (GameInput.SwitchPressed && !isPlatformerOnly) HandleSwitchButtonTriggered();

        if (currentGameState == GameState.Paint) HandlePaintInput();

        if (GameInput.PausePressed) SettingManager.Instance.SetSettingUI();
    }

    private void HandlePaintInput()
    {
        if (isFoldUnlocked && GameInput.FlipHorizontalPressed) HandleFlipHorizontalTriggered();
        if (isFoldUnlocked && GameInput.FlipVerticalPressed) HandleFlipVerticalTriggered();
        if (GameInput.UndoPressed) paintManager.UndoPaintAction();
        if (GameInput.ResetPressed && !isPlatformerOnly) HandleResetButtonTriggered();
    }

    // 모바일 조작 버튼(leftButton/rightButton/upButton)의 EventTrigger(PointerDown/PointerUp)에서 이름으로 호출합니다.
    // 플레이어는 스테이지마다 새로 Instantiate되므로 PlayerControllerT.Current로 현재 플레이어를 찾아갑니다.
    public void OnMobileLeftDown() => WithCurrentPlayer(p => p.OnMobileLeftDown());
    public void OnMobileLeftUp() => WithCurrentPlayer(p => p.OnMobileLeftUp());
    public void OnMobileRightDown() => WithCurrentPlayer(p => p.OnMobileRightDown());
    public void OnMobileRightUp() => WithCurrentPlayer(p => p.OnMobileRightUp());
    public void OnMobileJumpDown() => WithCurrentPlayer(p => p.OnMobileJumpDown());
    public void OnMobileJumpUp() => WithCurrentPlayer(p => p.OnMobileJumpUp());

    private static void WithCurrentPlayer(System.Action<PlayerControllerT> action)
    {
        if (PlayerControllerT.Current != null) action(PlayerControllerT.Current);
    }

    #endregion

    #region Button / manager handlers

    // 버튼 클릭 또는 해당 단축키 입력 시 공통으로 호출됩니다. 강조(깜빡임) 연출을 멈춥니다.
    private void HandleSwitchButtonTriggered()
    {
        SwitchState();
        stageUI.UnhighlightSwitchButton();
        stageUI.switchTutorialUI.HideUI(.3f).Forget();
    }

    private void HandleResetButtonTriggered()
    {
        // ResetButton()이 Paint로 상태를 되돌릴 수 있으므로, EnterPaintState가 튜토리얼을 다시 켜지 않도록 먼저 내려 둡니다.
        isResetTutorialPending = false;

        ResetButton();
        stageUI.UnhighlightResetButton();
        stageUI.resetTutorialUI.HideUI(.3f).Forget();
    }

    private void HandleUndoButtonTriggered() => paintManager.UndoPaintAction();

    private void HandleFlipHorizontalTriggered()
    {
        paintManager.FoldHorizontal();
        stageUI.UnhighlightFoldButtons();
    }

    private void HandleFlipVerticalTriggered()
    {
        paintManager.FoldVertical();
        stageUI.UnhighlightFoldButtons();
    }

    // 접기 회전 중에는 두 버튼을 서서히 숨기고, 되돌아올 때 서서히 다시 보여줍니다.
    private void HandleFoldStarted()
    {
        stageUI.flipHorizontalButton.HideUI(foldButtonFadeTime).Forget();
        stageUI.flipVerticalButton.HideUI(foldButtonFadeTime).Forget();
    }

    private void HandleFoldReturning()
    {
        stageUI.flipHorizontalButton.ShowUI(foldButtonFadeTime).Forget();
        stageUI.flipVerticalButton.ShowUI(foldButtonFadeTime).Forget();
    }

    private void HandleStarObtained() => stageUI.SetObjectiveStarObtained(true);

    // 도장이 찍히는 순간에 맞춰 보유 도장 수를 갱신합니다.
    private void HandleStampStamped()
    {
        if (hintManager != null) hintManager.RefreshStampText();
    }

    private void HandlePlayerFellIntoHole()
    {
        if (currentGameState != GameState.Platformer) return;
        ChangeGameState(GameState.Paint);
    }

    #endregion

    #region Clear

    public async void GameCleared()
    {
        if (currentGameState != GameState.Platformer) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("stage_clear");

        ChangeGameState(GameState.End);
        stageUI.SetObjectiveCleared(true);

        achievements = new Achievements(
            true,
            isEarlyStage || platformerManager.ObtainedStar,
            isEarlyStage || paintManager.PaintCount <= MinMoves);

        SaveProgress();

        // 화면 어두워지고 클리어 UI.
        await stageUI.ShowStageCleared(achievements, MinMoves, isEarlyStage);
        isReadyForNextStage = true;
    }

    private void SaveProgress()
    {
        if (GameManager.Instance == null || SaveManager.Instance == null) return;

        GameProgressData progress = GameProgressData.Load();
        progress.RecordStageCleared(stageIndex, achievements.ObtainedStar, achievements.MinMoves);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);
    }

    #endregion

    #region Stage transitions

    // 클리어 UI가 뜬 뒤에만 한 번 입력을 받도록 하는 가드. 소비되면 true.
    private bool ConsumeStageEndInput()
    {
        if (!isReadyForNextStage) return false;
        if (GameManager.Instance == null) return false;

        isReadyForNextStage = false;
        return true;
    }

    public void MoveToNextStage()
    {
        if (!ConsumeStageEndInput()) return;
        GameManager.Instance.LoadStage(stageIndex + 1);
    }

    public void RestartStage()
    {
        if (!ConsumeStageEndInput()) return;
        GameManager.Instance.LoadStage(stageIndex);
    }

    public void GoToStageSelection()
    {
        if (!ConsumeStageEndInput()) return;
        GameManager.Instance.LoadSelectScene();
    }

    #endregion

    // Decalcomanie 에디터에서 저장한 보드를 런타임 BoardData로 변환합니다.
    // 보드는 정사각형이고 크기 정보를 따로 들고 있지 않으므로 배열 길이에서 역산합니다.
    // 편집기 보드를 스테이지 JSON과 같은 형태로 변환합니다.
    // BoardData.FromJson과 마찬가지로 사방 1칸을 Wall로 둘러싸고, 카메라 프레이밍 기준인
    // PlayableSize는 벽을 뺀 원래 크기로 남깁니다. (벽이 없으면 플랫포머에서 보드 밖으로 떨어집니다)
    private static BoardData ConvertEditorBoardToBoardData(TileType[] board)
    {
        int playableSize = Mathf.RoundToInt(Mathf.Sqrt(board.Length));
        int paddedSize = playableSize + 2;

        // 편집기 인덱스(0-based)를 벽 한 칸만큼 민 1-based 좌표로 옮깁니다.
        // 타일이 놓이지 않았으면 IndexOf가 -1을 주므로, BoardData의 "설정 안 됨" 표기인 (0,0)으로 넘깁니다.
        Vector2 ToVec(int idx) => idx < 0 ? Vector2.zero : new Vector2(idx % playableSize + 2, idx / playableSize + 2);

        var fixedPoints = new List<Vector2>();
        var holePoints = new List<Vector2>();
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == TileType.Fixed) fixedPoints.Add(ToVec(i));
            if (board[i] == TileType.Hole) holePoints.Add(ToVec(i));
        }

        // 편집기에서 커튼으로 막아둔 칸. 하나도 없으면 빈 목록이 되고 Board가 기본 규칙으로 폴백합니다.
        var blockedPoints = new List<Vector2>();
        bool[] curtains = EditorManager.SavedCurtains;
        if (curtains != null && curtains.Length == board.Length)
        {
            for (int i = 0; i < board.Length; i++)
            {
                if (curtains[i]) blockedPoints.Add(ToVec(i));
            }
        }

        var wallPoints = new List<Vector2>();
        for (int i = 1; i <= paddedSize; i++)
        {
            wallPoints.Add(new Vector2(1, i));
            wallPoints.Add(new Vector2(paddedSize, i));
            wallPoints.Add(new Vector2(i, 1));
            wallPoints.Add(new Vector2(i, paddedSize));
        }

        var boardData = new BoardData(
            paddedSize,
            ToVec(System.Array.IndexOf(board, TileType.Start)),
            ToVec(System.Array.IndexOf(board, TileType.End)),
            ToVec(System.Array.IndexOf(board, TileType.Star)),
            ToVec(System.Array.IndexOf(board, TileType.Key)),
            fixedPoints,
            holePoints,
            wallPoints,
            blockedPoints);
        boardData.PlayableSize = playableSize;
        return boardData;
    }
}
