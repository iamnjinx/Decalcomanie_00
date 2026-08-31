using System.Collections.Generic;
using UnityEngine;
using TarodevController;
using Cysharp.Threading.Tasks;

public class StageManager : MonoBehaviour
{
    // 스테이지 인덱스 기준 초반부 특수 규칙(디자인상 고정값).
    private const int PlatformerOnlyBelowStage = 2; // 이 인덱스 미만은 페인트 없이 플랫포머만
    private const int EarlyStageMaxIndex = 3;       // 이 인덱스 이하는 별/최소이동 자동 달성
    private const int GuideStageCount = 6;          // 이 인덱스 미만은 가이드 이미지 노출

    private const int SwitchUnlockStage = 3; // 1-4부터 Switch 버튼 등장
    private const int ResetUnlockStage = 4;  // 1-5부터 Reset 버튼 등장
    private const int FoldUnlockStage = 4;   // 1-5부터 접기 버튼 등장

    private const int SwitchTutorialStageIndex = 2; // 1-3: 특정 타일 클릭 시 Switch 조기 활성화
    private const int FoldTutorialStageIndex = 3;   // 1-4: 타일 클릭 시 접기 조기 활성화, 접기 시 Reset 조기 활성화
    // BoardData.FromJson이 사방 1칸을 벽으로 두르기 때문에, 원본 스테이지 JSON 좌표(4,1)에서 (+1,+1) 밀린 값입니다.
    private static readonly Vector2Int SwitchTutorialTileCoord = new Vector2Int(5, 2);

    private GameState currentGameState = GameState.Paint;

    [SerializeField] BoardManager boardManager;
    [SerializeField] PaintManager paintManager;
    [SerializeField] PlatformerManager platformerManager;
    [SerializeField] private TextAsset testBoardDataTextAsset;

    [SerializeField] private StageUI stageUI;
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private HintManager hintManager;

    [SerializeField] private float foldButtonFadeTime = 0.2f; // 접기 회전(foldTime)보다 빠르게 버튼을 숨기고 보여주기 위한 시간

    private Achievements achievements = new Achievements(false, false, false);

    private bool isReadyForNextStage = false;
    private bool isPlatformerOnly = false;
    private bool isEarlyStage = false;

    private bool isSwitchUnlocked = false;
    private bool isResetUnlocked = false;
    private bool isFoldUnlocked = false;

    public event System.Action<GameState> OnGameStateChanged;

    void Awake()
    {
        stageUI.stageClearedNextStageButton.OnSingleClick += () => MoveToNextStage();
        stageUI.stageClearedStageSelectionButton.OnSingleClick += () => GoToStageSelection();
        stageUI.stageClearedRestartStageButton.OnSingleClick += () => RestartStage();

        stageUI.switchButton.OnSingleClick += HandleSwitchButtonTriggered;
        stageUI.resetButton.OnSingleClick += HandleResetButtonTriggered;
        stageUI.undoButton.OnSingleClick += () => paintManager.UndoPaintAction();

        stageUI.flipHorizontalButton.OnSingleClick += HandleFlipHorizontalTriggered;
        stageUI.flipVerticalButton.OnSingleClick += HandleFlipVerticalTriggered;

        // 접기 회전 중에는 두 버튼을 서서히 숨기고, 되돌아올 때 서서히 다시 보여줍니다.
        paintManager.OnFoldStarted += () =>
        {
            stageUI.flipHorizontalButton.HideUI(foldButtonFadeTime).Forget();
            stageUI.flipVerticalButton.HideUI(foldButtonFadeTime).Forget();
        };
        paintManager.OnFoldReturning += () =>
        {
            stageUI.flipHorizontalButton.ShowUI(foldButtonFadeTime).Forget();
            stageUI.flipVerticalButton.ShowUI(foldButtonFadeTime).Forget();
        };

        paintManager.OnPaintCountChanged += stageUI.UpdateUsedTileText;
        platformerManager.OnStarObtained += () => stageUI.SetObjectiveStarObtained(true);
    }

    void Start()
    {
        var gm = GameManager.Instance;
        int stageIndex = gm != null ? gm.CurrentStageIndex : 0;

        if (EditorManager.SavedBoard != null)
        {
            boardManager.CreateBoard(ConvertEditorBoardToBoardData(EditorManager.SavedBoard));
        }
        else if (gm == null || gm.GetCurrentStageAsset() == null)
        {
            boardManager.CreateBoard(testBoardDataTextAsset);
        }
        else
        {
            boardManager.CreateBoard(gm.GetCurrentStageAsset());
            stageUI.SetStageBackground(stageIndex);

            bool hasGuide = stageIndex < GuideStageCount;
            if (hasGuide)
                stageUI.guideImage.sprite = gm.GameData.guideSprites[stageIndex];
            stageUI.guideImage.gameObject.SetActive(hasGuide);
        }

        // Door/Star/Key/Player는 스테이지 전체에서 재사용되므로,
        // 아래 isPlatformerOnly 초기 모드 전환보다 먼저 한 번만 만들어 둔다. (Paint 모드에서는 숨겨진다)
        paintManager.CreateTileControllers();
        platformerManager.CreateBoardObjects(boardManager.CurrentBoard);

        isPlatformerOnly = gm != null && stageIndex < PlatformerOnlyBelowStage;
        if (isPlatformerOnly)
        {
            stageUI.SetPlatformerOnlyMode();
            ChangeGameState(GameState.Platformer);
        }

        isEarlyStage = gm != null && stageIndex <= EarlyStageMaxIndex;
        isSwitchUnlocked = stageIndex >= SwitchUnlockStage;
        isResetUnlocked = stageIndex >= ResetUnlockStage;
        isFoldUnlocked = stageIndex >= FoldUnlockStage;
        var language = gm != null ? gm.CurrentLanguage : GameLanguage.English;

        stageUI.SetEarlyStageUIVisibility(stageIndex);
        SetupPaintButtonTutorials(stageIndex);
        stageUI.SetObjectiveTexts(language, isEarlyStage, boardManager.CurrentBoard.BoardData.minMoves);
        stageUI.SetAfterButtonTexts(language);
        stageUI.UpdateUsedTileText(paintManager.paintCount);
        stageUI.SetCurStageText(stageIndex);
        stageUI.ShowMainUI(isPlatformerOnly);
    }

    // 1-3, 1-4의 튜토리얼성 조기 버튼 활성화 이벤트를 등록합니다.
    private void SetupPaintButtonTutorials(int stageIndex)
    {
        if (stageIndex == SwitchTutorialStageIndex)
        {
            stageUI.clickTutorialUI.ShowUI();
            paintManager.OnTilePainted += HandleSwitchTutorialTilePainted;
        }

        if (stageIndex == FoldTutorialStageIndex)
        {
            paintManager.OnTilePainted += HandleFoldTutorialTilePainted;
            paintManager.OnFolded += HandleFoldTutorialFolded;
        }
    }

    private void HandleSwitchTutorialTilePainted(int tileID)
    {
        int size = boardManager.CurrentBoard.size;
        int targetTileID = Board.CoordToIndex(SwitchTutorialTileCoord.x, SwitchTutorialTileCoord.y, size);
        if (tileID != targetTileID) return;

        isSwitchUnlocked = true;
        stageUI.clickTutorialUI.HideUI(.3f).Forget();
        stageUI.SetSwitchButtonActive(true);
        stageUI.HighlightSwitchButton();
        stageUI.switchTutorialUI.ShowUI();
        paintManager.OnTilePainted -= HandleSwitchTutorialTilePainted;
    }

    private void HandleFoldTutorialTilePainted(int tileID)
    {
        isFoldUnlocked = true;
        stageUI.SetFoldButtonsActive(true);
        stageUI.HighlightFoldButtons();
        paintManager.OnTilePainted -= HandleFoldTutorialTilePainted;
    }

    private void HandleFoldTutorialFolded()
    {
        isResetUnlocked = true;
        stageUI.SetResetButtonActive(true);
        stageUI.HighlightResetButton();
        stageUI.resetTutorialUI.ShowUI();
        paintManager.OnFolded -= HandleFoldTutorialFolded;
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
                stageUI.HideObjectiveIfShown();
                paintManager.SetPaperBoardSurfaceTransparent(false);
                ResetState();
                if (!isPlatformerOnly)
                {
                    stageUI.undoButton.gameObject.SetActive(true);
                    stageUI.SetResetButtonActive(isResetUnlocked);
                    stageUI.flipHorizontalButton.ShowUI();
                    stageUI.flipVerticalButton.ShowUI();
                }
                break;
            case GameState.Platformer:
                stageUI.guideImage.gameObject.layer = LayerMask.NameToLayer("GuideTile");
                stageUI.ShowObjectiveIfHidden();
                paintManager.SetPaperBoardSurfaceTransparent(true);
                paintManager.PausePaint();
                platformerManager.SetPlatformerObjects();

                platformerManager.OnCleared += GameCleared;
                platformerManager.OnFellIntoHole += OnPlayerFellIntoHole;
                if (!isPlatformerOnly)
                {
                    stageUI.undoButton.gameObject.SetActive(false);
                    stageUI.SetResetButtonActive(false);
                    stageUI.flipHorizontalButton.HideUI();
                    stageUI.flipVerticalButton.HideUI();
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
            return;
        }
        if(tutorialManager != null && tutorialManager.CurTutoID != -1) return;

        if (Input.GetKeyDown(KeyCode.Tab)) { if (!isPlatformerOnly) HandleSwitchButtonTriggered(); }

        if (currentGameState == GameState.Paint)
        {
            if (isFoldUnlocked && Input.GetKeyDown(KeyCode.Alpha1)) HandleFlipHorizontalTriggered();
            if (isFoldUnlocked && Input.GetKeyDown(KeyCode.Alpha2)) HandleFlipVerticalTriggered();
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Z)) paintManager.UndoPaintAction();
            if (Input.GetKeyDown(KeyCode.R))   { if (!isPlatformerOnly) HandleResetButtonTriggered(); }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SettingManager.Instance.SetSettingUI();
        }
    }

    // 버튼 클릭 또는 해당 단축키 입력 시 공통으로 호출됩니다. 강조(깜빡임) 연출을 멈춥니다.
    private void HandleSwitchButtonTriggered()
    {
        SwitchState();
        stageUI.UnhighlightSwitchButton();
        stageUI.switchTutorialUI.HideUI(.3f).Forget();
    }

    private void HandleResetButtonTriggered()
    {
        ResetButton();
        stageUI.UnhighlightResetButton();
        stageUI.resetTutorialUI.HideUI(.3f).Forget();
    }

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

    public void SwitchState()
    {
        if (isPlatformerOnly || !isSwitchUnlocked) return;
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
        stageUI.SetObjectiveStarObtained(false);

        achievements = new Achievements(false, false, false);
    }

    public void ResetButton()
    {
        if (isPlatformerOnly || !isResetUnlocked) return;
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
        stageUI.SetObjectiveCleared(true);

        achievements = new Achievements(
            true,
            isEarlyStage || platformerManager.obtainedStar,
            isEarlyStage || paintManager.paintCount <= boardManager.CurrentBoard.BoardData.minMoves);

        SaveProgress();

        // 화면 어두워지고 클리어 UI.
        await stageUI.ShowStageCleared(achievements.IsCleared, achievements.ObtainedStar, achievements.MinMoves, boardManager.CurrentBoard.BoardData.minMoves, isEarlyStage);
        isReadyForNextStage = true;
    }

    private void SaveProgress()
    {
        if (GameManager.Instance == null || SaveManager.Instance == null) return;

        var progress = GameProgressData.Load();
        progress.RecordStageCleared(
            GameManager.Instance.CurrentStageIndex,
            achievements.ObtainedStar,
            achievements.MinMoves);
        SaveManager.Instance.Save(GameProgressData.SaveKey, progress);

        if (hintManager != null) hintManager.RefreshStampText();
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

    // 클리어 UI가 뜬 뒤에만 한 번 입력을 받도록 하는 가드. 소비되면 true.
    private bool ConsumeStageEndInput()
    {
        if (!isReadyForNextStage) return false;
        isReadyForNextStage = false;
        return true;
    }

    public void MoveToNextStage()
    {
        if (!ConsumeStageEndInput()) return;
        GameManager.Instance.LoadStage(GameManager.Instance.CurrentStageIndex + 1);
    }

    public void RestartStage()
    {
        if (!ConsumeStageEndInput()) return;
        GameManager.Instance.LoadStage(GameManager.Instance.CurrentStageIndex);
    }

    public void GoToStageSelection()
    {
        if (!ConsumeStageEndInput()) return;
        GameManager.Instance.LoadSelectScene();
    }
}

public enum GameState
    {
        Paint, Platformer, End
    }

    public class Achievements
    {
        public bool IsCleared { get; private set; }
        public bool ObtainedStar { get; private set; }
        public bool MinMoves { get; private set; }

        public Achievements(bool isCleared, bool obtainedStar, bool minMoves)
        {
            IsCleared = isCleared;
            ObtainedStar = obtainedStar;
            MinMoves = minMoves;
        }
    }


