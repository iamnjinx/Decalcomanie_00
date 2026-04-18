using System.Collections;
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

    private Achievements achievements = new Achievements(false, false, false);

    void Awake()
    {
        stageUI.nextStageButton.OnSingleClick += () => MoveToNextStage();

        stageUI.switchButton.OnSingleClick += () => SWITCH();
        stageUI.resetButton.OnSingleClick += () => ResetButton();

        stageUI.flipHorizontalButton.OnSingleClick += () => paintManager.FoldHorizontal();
        stageUI.flipVerticalButton.OnSingleClick += () => paintManager.FoldVertical();
    }

    void Start()
    {
        boardManager.CreateBoard(GameManager.Instance.GetCurrentStageAsset());
        paintManager.CreateTileControllers();
    }

    public void ChangeGameState(GameState newGameState)
    {
        currentGameState = newGameState;

        switch (currentGameState)
        {
            case GameState.Paint:
                _RESET();
                break;
            case GameState.Platformer:
                paintManager.PausePaint();
                platformerManager.SetPlatformerObjects(boardManager.CurBoard);

                platformerManager.OnCleared += GameCleared;
                break;
            case GameState.End:
                break;
        }
    }

    public void SWITCH()
    {
        if (currentGameState == GameState.Paint)
        {
            ChangeGameState(GameState.Platformer);
        }
        else if (currentGameState == GameState.Platformer)
        {
            ChangeGameState(GameState.Paint);
        }
    }

    private void _RESET()
    {
        platformerManager.OnCleared -= GameCleared;
        paintManager.ResumePaint();
        platformerManager.ResetObjects();

        achievements = new Achievements(false, false, false);
    }

    public void ResetButton()
    {
        if (currentGameState == GameState.Platformer)
        {
            ChangeGameState(GameState.Paint);
        }
        else
        {
            _RESET();
        }
        paintManager.ResetPaint();
    }

    public void GameCleared()
    {
        if (currentGameState == GameState.End) return;
        ChangeGameState(GameState.End);
        Debug.Log($"Game Cleared!");

        achievements = new Achievements(true, platformerManager.obtainedStar, paintManager.paintCount <= boardManager.CurBoard.BoardData.minMoves);

        // 화면 어두워지고 클리어 UI.
        stageUI.ShowStageCleared(achievements.Is_Cleared, achievements.ObtainedStar, achievements.Min_Moves);
    }

    public void MoveToNextStage()
    {
        // 다음 스테이지로 이동.
        Debug.Log($"Move To Next Stage!");
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
}


