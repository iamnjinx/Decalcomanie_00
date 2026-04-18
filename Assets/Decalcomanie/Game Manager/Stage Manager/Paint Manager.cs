using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class PaintManager : MonoBehaviour
{
    [SerializeField] BoardManager boardManager;

    [SerializeField] Transform TileParent;

    public List<TileController> allTileControllers;

    [SerializeField] private TileController tileControllerPrefab;

    public Stack<TilePaintAction> paintActions = new Stack<TilePaintAction>();

    public Transform LowerLeft;
    public Transform UpperLeft;
    public Transform LowerRight;
    public Transform UpperRight;

    [SerializeField] GameObject PaintPaperObj;

    [SerializeField] Ease flipEase = Ease.InCirc;

    [SerializeField] private PaintUI paintUI;

    private bool _isFolding;

    public int paintCount = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
            UndoPaintAction();
    }

    public void CreateTileControllers()
    {
        for (int i = 0; i < boardManager.CurBoard.allTiles.Length; i++)
        {
            TileController tc = Instantiate(tileControllerPrefab, TileParent);
            Tile tile = boardManager.CurBoard.allTiles[i];
            tc.SetTile(tile, i);
            tc.OnTilePainted += AddPaintAction;
            tc.transform.position = new Vector3(i % boardManager.CurBoard.size, i / boardManager.CurBoard.size, 0) * 2.5f;

            Board board = boardManager.CurBoard;
            tc.SetPaintFlip(
                board.Quadrant2.Contains(i) || board.Quadrant3.Contains(i),
                board.Quadrant3.Contains(i) || board.Quadrant4.Contains(i));
            allTileControllers.Add(tc);
        }

        paintUI.SetPaintButtons(boardManager.CurBoard.allTiles.Length, Paint);
    }

    public void Paint(int id) { allTileControllers[id].PaintTile(); paintCount++; }

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
            TileController tc = allTileControllers[tile];
            if (tc.Tile.type == TileType.Paint || tc.Tile.type == TileType.Paint_Dec)
                tc.ChangeTileType(0);
            else
                tc.UpdateTile();
        }

        if (lastAction.is_paint)
            paintCount--;
    }

    public async void FoldVertical()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (LowerLeft, LowerRight),
            start => new Vector3(179f, start.y, start.z),
            () => boardManager.CurBoard.FoldVertical());
    }

    public async void FoldHorizontal()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (LowerLeft, UpperLeft),
            start => new Vector3(start.x, -179f, start.z),
            () => boardManager.CurBoard.FoldHorizontal());
    }

    private async UniTask ExecuteFold(
        (Transform panel1, Transform panel2) panels,
        System.Func<Vector3, Vector3> getFoldRotation,
        System.Func<List<int>> performFold)
    {
        _isFolding = true;
        await paintUI.SetBasePaintUI(false);

        Vector3 start1 = panels.panel1.eulerAngles;
        Vector3 start2 = panels.panel2.eulerAngles;

        RotatePanelPair(panels, getFoldRotation(start1), getFoldRotation(start2));
        await UniTask.Delay(500);

        AddPaintAction(performFold(), false);
        await UniTask.Delay(100);

        RotatePanelPair(panels, start1, start2);
        await UniTask.Delay(500);

        await paintUI.SetBasePaintUI(true);
        _isFolding = false;
    }

    private void RotatePanelPair(
        (Transform panel1, Transform panel2) panels,
        Vector3 rotation1, Vector3 rotation2)
    {
        panels.panel1.DORotate(rotation1, 0.5f).SetEase(flipEase);
        panels.panel2.DORotate(rotation2, 0.5f).SetEase(flipEase);
    }

    public void ResetPaint()
    {
        paintCount = 0;
        foreach (TileController tc in allTileControllers)
        {
            bool isPainted = tc.Tile.type == TileType.Paint || tc.Tile.type == TileType.Paint_Dec;
            tc.ChangeTileType(isPainted ? 0 : (int)tc.Tile.type);
        }

        ResumePaint();
        paintActions.Clear();
    }

    public void ResumePaint()
    {
        foreach (TileController tc in allTileControllers)
            tc.ShowTile();

        paintUI.SetBasePaintUI(true).Forget();
        paintUI.SetCurtains(true);
        PaintPaperObj.SetActive(true);
    }

    public void PausePaint()
    {
        HideTiles();
        paintUI.SetBasePaintUI(false).Forget();
    }

    private void HideTiles()
    {
        PaintPaperObj.SetActive(false);
        paintUI.SetCurtains(false);
        foreach (TileController tc in allTileControllers)
            tc.HideTile();
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
