using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class PaintManager : MonoBehaviour
{
    [SerializeField] BoardManager boardManager;

    [SerializeField] Transform TileParent;

    public List<PaintedController> allTileControllers;

    [SerializeField] private PaintedController tileControllerPrefab;

    [SerializeField] private Transform curtainBlockPrefab;
    private List<Transform> curtainBlocks = new List<Transform>();

    public Stack<TilePaintAction> paintActions = new Stack<TilePaintAction>();

    [SerializeField] private PaperBoard paperBoard;
    public void SetPaperBoardSurfaceTransparent(bool transparent) => paperBoard.SetSurfaceTransparent(transparent);

    [SerializeField] private PaintObjectMarkers objectMarkersPrefab;
    private PaintObjectMarkers objectMarkers;

    public Transform HorizontalFoldAxis;
    public Transform VerticalFoldAxis;

    //[SerializeField] GameObject PaintPaperObj;

    [SerializeField] Ease flipEase = Ease.InCirc;
    [SerializeField] float foldTime = 0.5f;

    [SerializeField] private PaintUI paintUI;
    [SerializeField] private TutorialManager tutorialManager;

    private bool _isFolding;
    public bool IsFolding => _isFolding;

    public int paintCount = 0;

    public event System.Action<int> OnPaintCountChanged;
    public event System.Action<int> OnTilePainted;
    public event System.Action OnFolded;
    public event System.Action OnFoldStarted;
    public event System.Action OnFoldReturning;

    // 스테이지 로드 시 1회만 호출. Fixed/Wall처럼 처음부터 고정된 타일만 미리 만들어 두고,
    // 나머지(Empty) 타일은 실제로 칠해지는 순간 GetOrCreateTile을 통해 그때그때 생성한다.
    public void CreateTileControllers()
    {
        Board board = boardManager.CurrentBoard;
        allTileControllers = new List<PaintedController>();

        for (int i = 0; i < board.allTiles.Length; i++)
        {
            allTileControllers.Add(null);

            TileType type = board.allTiles[i].type;
            if (type == TileType.Fixed || type == TileType.Wall || type == TileType.Hole)
                GetOrCreateTile(i);
        }

        paintUI.SetPaintButtons(board, Paint);
        PlaceCurtainBlocks(board);

        objectMarkers = Instantiate(objectMarkersPrefab, TileParent);
        objectMarkers.Init(board);
    }

    // 칠할 수 없는(Quadrant1/Quadrant3에 속하지 않는) 타일 위치에 curtainBlock을 배치한다.
    private void PlaceCurtainBlocks(Board board)
    {
        foreach (int tileIndex in board.GetPlayableTileIndices())
        {
            bool isPaintable = board.Quadrant1.Contains(tileIndex) || board.Quadrant3.Contains(tileIndex);
            if (isPaintable) continue;

            Transform curtainBlock = Instantiate(curtainBlockPrefab, TileParent);
            curtainBlock.position = board.GetWorldPosition(tileIndex);
            curtainBlocks.Add(curtainBlock);
        }
    }

    private void SetCurtainBlocksActive(bool is_active)
    {
        foreach (Transform curtainBlock in curtainBlocks)
            curtainBlock.gameObject.SetActive(is_active);
    }

    // Platformer 모드로 넘어갈 때 호출. Hole은 실제 HoleController가 대신 보여줘야 하므로
    // Paint 모드용 타일 오브젝트는 지운다.
    public void DestroyHoleTiles()
    {
        Board board = boardManager.CurrentBoard;
        for (int i = 0; i < allTileControllers.Count; i++)
        {
            if (allTileControllers[i] == null || board.allTiles[i].type != TileType.Hole) continue;
            Destroy(allTileControllers[i].gameObject);
            allTileControllers[i] = null;
        }
    }

    // Paint 모드로 돌아올 때 호출. 지워졌던 Hole 타일 오브젝트를 다시 만든다.
    public void RecreateHoleTiles()
    {
        Board board = boardManager.CurrentBoard;
        for (int i = 0; i < board.allTiles.Length; i++)
            if (board.allTiles[i].type == TileType.Hole)
                GetOrCreateTile(i);
    }

    private PaintedController GetOrCreateTile(int id)
    {
        if (allTileControllers[id] != null) return allTileControllers[id];

        Board board = boardManager.CurrentBoard;
        PaintedController tc = Instantiate(tileControllerPrefab, TileParent);
        tc.transform.position = board.GetWorldPosition(id);
        tc.Init(board.allTiles[id], id, board);
        tc.OnTilePainted += AddPaintAction;
        allTileControllers[id] = tc;
        return tc;
    }

    public void Paint(int id)
    {
        if (boardManager.CurrentBoard.allTiles[id].type != TileType.Empty) return;

        PaintedController tc = GetOrCreateTile(id);
        tc.PaintTile();
        paintCount++;
        OnPaintCountChanged?.Invoke(paintCount);
        OnTilePainted?.Invoke(id);
        if(AudioManager.Instance != null)
            AudioManager.Instance.PlayRandomSFX(new[] { "paint_1", "paint_2" });
    }

    public void AddPaintAction(List<int> paintedTiles, bool is_paint = true) => paintActions.Push(new TilePaintAction(paintedTiles, is_paint));

    public void AddPaintAction(int paintedTileID) => AddPaintAction(new List<int> { paintedTileID });

    public void UndoPaintAction()
    {
        if (paintActions.Count == 0)
        {
            Debug.Log("No paint actions to undo");
            return;
        }

        var lastAction = paintActions.Pop();

        foreach (int tile in lastAction.PaintedTiles)
        {
            PaintedController tc = allTileControllers[tile];
            if (tc.Tile.IsPainted)
            {
                tc.ChangeTileType(0);
                Destroy(tc.gameObject);
                allTileControllers[tile] = null;
            }
            else
                tc.Refresh();
        }

        if (lastAction.is_paint)
        {
            paintCount--;
            OnPaintCountChanged?.Invoke(paintCount);
        }
    }

    [SerializeField] private float verticalAxisCreaseAngle = -170f;
    [SerializeField] private float horizontalAxisCreaseAngle = -170f;

    private static Vector3 GetVerticalAxisFoldRotation(Vector3 start) => new Vector3(170f, start.y, start.z);
    private static Vector3 GetHorizontalAxisFoldRotation(Vector3 start) => new Vector3(start.x, -170f, start.z);

    private Vector3 GetVerticalAxisCreaseRotation(Vector3 start) => new Vector3(verticalAxisCreaseAngle, start.y, start.z);
    private Vector3 GetHorizontalAxisCreaseRotation(Vector3 start) => new Vector3(start.x, horizontalAxisCreaseAngle, start.z);

    public async void FoldVertical()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (paperBoard.UpperLeft, paperBoard.UpperRight),
            GetVerticalAxisFoldRotation,
            VerticalFoldAxis,
            GetVerticalAxisCreaseRotation,
            () => boardManager.CurrentBoard.FoldVertical());
    }

    public async void FoldHorizontal()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (paperBoard.LowerLeft, paperBoard.UpperLeft),
            GetHorizontalAxisFoldRotation,
            HorizontalFoldAxis,
            GetHorizontalAxisCreaseRotation,
            () => boardManager.CurrentBoard.FoldHorizontal());
    }

    private async UniTask ExecuteFold(
        (Transform panel1, Transform panel2) panels,
        System.Func<Vector3, Vector3> getFoldRotation,
        Transform foldAxis,
        System.Func<Vector3, Vector3> getAxisFoldRotation,
        System.Func<List<int>> performFold)
    {
        _isFolding = true;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("fold_paper");
        await paintUI.SetBasePaintUI(false);

        Vector3 start1 = panels.panel1.localEulerAngles;
        Vector3 start2 = panels.panel2.localEulerAngles;
        Vector3 axisStart = foldAxis != null ? foldAxis.eulerAngles : default;

        OnFoldStarted?.Invoke();
        RotatePanelPair(panels, getFoldRotation(start1), getFoldRotation(start2));
        RotateAxis(foldAxis, getAxisFoldRotation(axisStart));
        await UniTask.Delay((int)(foldTime * 1000));

        List<int> foldedTiles = performFold();
        foreach (int tileID in foldedTiles) GetOrCreateTile(tileID);
        AddPaintAction(foldedTiles, false);
        OnFolded?.Invoke();
        await UniTask.Delay(100);

        OnFoldReturning?.Invoke();
        RotatePanelPair(panels, start1, start2);
        RotateAxis(foldAxis, axisStart);
        await UniTask.Delay((int)(foldTime * 1000));

        await paintUI.SetBasePaintUI(true);
        _isFolding = false;
    }

    private void RotatePanelPair(
        (Transform panel1, Transform panel2) panels,
        Vector3 rotation1, Vector3 rotation2)
    {
        panels.panel1.DOLocalRotate(rotation1, foldTime).SetEase(flipEase);
        panels.panel2.DOLocalRotate(rotation2, foldTime).SetEase(flipEase);
    }

    private void RotateAxis(Transform axis, Vector3 rotation)
    {
        if (axis == null) return;
        axis.DORotate(rotation, foldTime).SetEase(flipEase);
    }

    public void ResetPaint()
    {
        paintCount = 0;
        OnPaintCountChanged?.Invoke(paintCount);
        for (int i = 0; i < allTileControllers.Count; i++)
        {
            PaintedController tc = allTileControllers[i];
            if (tc == null) continue;

            if (tc.Tile.IsPainted)
            {
                tc.ChangeTileType(0);
                Destroy(tc.gameObject);
                allTileControllers[i] = null;
            }
            else
                tc.ChangeTileType((int)tc.Tile.type);
        }

        ResumePaint();
        paintActions.Clear();
    }

    public void ResumePaint()
    {
        paintUI.SetBasePaintUI(true).Forget();
        SetCurtainBlocksActive(true);
        objectMarkers.gameObject.SetActive(true);
    }

    public void PausePaint()
    {
        paintUI.SetBasePaintUI(false).Forget();
        SetCurtainBlocksActive(false);
        objectMarkers.gameObject.SetActive(false);
    }
}

public class TilePaintAction
{
    public List<int> PaintedTiles;
    public bool is_paint;

    public TilePaintAction(List<int> paintedTiles, bool is_paint = true)
    {
        PaintedTiles = paintedTiles;
        this.is_paint = is_paint;
    }
}
