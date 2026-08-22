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

    public Transform HorizontalFoldAxis;
    public Transform VerticalFoldAxis;

    [SerializeField] GameObject PaintPaperObj;

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

    public void CreateTileControllers()
    {
        Board board = boardManager.CurrentBoard;
        for (int i = 0; i < board.allTiles.Length; i++)
        {
            TileController tc = Instantiate(tileControllerPrefab, TileParent);
            tc.SetTile(board.allTiles[i], i);
            tc.OnTilePainted += AddPaintAction;
            tc.transform.position = board.GetWorldPosition(i);
            var (flipX, flipY) = board.GetFlips(i);
            tc.SetPaintFlip(flipX, flipY);
            allTileControllers.Add(tc);
        }

        paintUI.SetPaintButtons(board.GetPlayableTileIndices(), Paint);
    }

    public void Paint(int id)
    {
        if (allTileControllers[id].Tile.type != TileType.Empty) return;
        allTileControllers[id].PaintTile();
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
            TileController tc = allTileControllers[tile];
            if (tc.Tile.IsPainted)
                tc.ChangeTileType(0);
            else
                tc.UpdateTile();
        }

        if (lastAction.is_paint)
        {
            paintCount--;
            OnPaintCountChanged?.Invoke(paintCount);
        }
    }

    private static Vector3 GetVerticalAxisFoldRotation(Vector3 start) => new Vector3(-179f, start.y, start.z);
    private static Vector3 GetHorizontalAxisFoldRotation(Vector3 start) => new Vector3(start.x, -179f, start.z);

    public async void FoldVertical()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (UpperLeft, UpperRight),
            GetVerticalAxisFoldRotation,
            VerticalFoldAxis,
            GetVerticalAxisFoldRotation,
            () => boardManager.CurrentBoard.FoldVertical());
    }

    public async void FoldHorizontal()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (LowerLeft, UpperLeft),
            GetHorizontalAxisFoldRotation,
            HorizontalFoldAxis,
            GetHorizontalAxisFoldRotation,
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
        paintUI.SetShadow(false);
        await paintUI.SetBasePaintUI(false);

        Vector3 start1 = panels.panel1.eulerAngles;
        Vector3 start2 = panels.panel2.eulerAngles;
        Vector3 axisStart = foldAxis != null ? foldAxis.eulerAngles : default;

        OnFoldStarted?.Invoke();
        RotatePanelPair(panels, getFoldRotation(start1), getFoldRotation(start2));
        RotateAxis(foldAxis, getAxisFoldRotation(axisStart));
        await UniTask.Delay((int)(foldTime * 1000));

        AddPaintAction(performFold(), false);
        OnFolded?.Invoke();
        await UniTask.Delay(100);

        OnFoldReturning?.Invoke();
        RotatePanelPair(panels, start1, start2);
        RotateAxis(foldAxis, axisStart);
        await UniTask.Delay((int)(foldTime * 1000));

        paintUI.SetShadow(true);
        await paintUI.SetBasePaintUI(true);
        _isFolding = false;
    }

    private void RotatePanelPair(
        (Transform panel1, Transform panel2) panels,
        Vector3 rotation1, Vector3 rotation2)
    {
        panels.panel1.DORotate(rotation1, foldTime).SetEase(flipEase);
        panels.panel2.DORotate(rotation2, foldTime).SetEase(flipEase);
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
        foreach (TileController tc in allTileControllers)
            tc.ChangeTileType(tc.Tile.IsPainted ? 0 : (int)tc.Tile.type);

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
