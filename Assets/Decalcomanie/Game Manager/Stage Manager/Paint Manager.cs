using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

public class PaintManager : MonoBehaviour
{
    [SerializeField] BoardManager boardManager;
    [SerializeField] Transform TileParent;

    [SerializeField] private PaintedController tileControllerPrefab;
    [SerializeField] private Transform curtainBlockPrefab;
    [SerializeField] private PaintObjectMarkers objectMarkersPrefab;
    [SerializeField] private PaperBoard paperBoard;

    [SerializeField] private PaintUI paintUI;

    [Header("Fold")]
    [SerializeField] Ease flipEase = Ease.InCirc;
    [SerializeField] float foldTime = 0.5f;
    [SerializeField] private float verticalAxisCreaseAngle = -170f;
    [SerializeField] private float horizontalAxisCreaseAngle = -170f;

    public Transform HorizontalFoldAxis;
    public Transform VerticalFoldAxis;

    private readonly List<Transform> curtainBlocks = new List<Transform>();
    private readonly Stack<TilePaintAction> paintActions = new Stack<TilePaintAction>();

    // 스테이지에서 생성된 아이템 버튼 전체(Reset 시 복구용)와, 아직 소모되지 않아 사용 가능한 버튼(순서 보장, 앞에서부터 소모).
    private readonly Dictionary<PaintMode, List<ItemButton>> allItemButtons = new Dictionary<PaintMode, List<ItemButton>>();
    private readonly Dictionary<PaintMode, List<ItemButton>> availableItemButtons = new Dictionary<PaintMode, List<ItemButton>>();

    // 유저가 직접 클릭해서 고른 아이템 버튼 하나. 같은 종류(연필) 버튼이 여러 개여도 이 버튼만 selected로 표시되고, 실제로 소모되는 것도 이 버튼이다.
    private ItemButton _selectedItemButton;

    // 연필로 만든 Fixed 타일의 인덱스만 추적한다. 레벨(JSON) Fixed는 여기 포함되지 않는다.
    // (지우개는 둘 다 지울 수 있지만, Reset 때 "원래 있던 것"과 "플레이어가 추가한 것"을 구분하는 데 쓰인다.)
    private readonly HashSet<int> userFixedTiles = new HashSet<int>();

    // 스테이지 로드 시점의 타일 타입 스냅샷. Reset은 지금 상태가 아니라 항상 이 원본으로 되돌아간다.
    private TileType[] originalTileTypes;

    private List<PaintedController> tileControllers = new List<PaintedController>();
    private PaintObjectMarkers objectMarkers;

    private bool _isFolding;
    public bool IsFolding => _isFolding;

    public int PaintCount { get; private set; }

    public enum PaintMode { Paint, Pencil, Eraser }

    private PaintMode _currentMode = PaintMode.Paint;
    public PaintMode CurrentMode => _currentMode;

    public event System.Action<int> OnPaintCountChanged;
    public event System.Action<int> OnTilePainted;
    public event System.Action<PaintMode> OnPaintModeChanged;
    public event System.Action OnFolded;
    public event System.Action OnFoldStarted;
    public event System.Action OnFoldReturning;

    private Board CurrentBoard => boardManager.CurrentBoard;

    #region Setup

    // 스테이지 로드 시 1회만 호출. Fixed/Wall처럼 처음부터 고정된 타일만 미리 만들어 두고,
    // 나머지(Empty) 타일은 실제로 칠해지는 순간 GetOrCreateTile을 통해 그때그때 생성한다.
    public void CreateTileControllers()
    {
        Board board = CurrentBoard;
        tileControllers = new List<PaintedController>(board.allTiles.Length);
        originalTileTypes = new TileType[board.allTiles.Length];

        for (int i = 0; i < board.allTiles.Length; i++)
        {
            tileControllers.Add(null);

            TileType type = board.allTiles[i].type;
            originalTileTypes[i] = type;

            if (type == TileType.Fixed || type == TileType.Wall || type == TileType.Hole)
                GetOrCreateTile(i);
        }

        paintUI.SetPaintButtons(board, HandleTileClicked);
        SetupItemButtons(board);
        PlaceCurtainBlocks(board);

        objectMarkers = Instantiate(objectMarkersPrefab, TileParent);
        objectMarkers.Init(board);
    }

    // 칠할 수 없는 타일 위치에 curtainBlock을 배치한다. 판정은 Board.IsPaintable이 담당한다.
    private void PlaceCurtainBlocks(Board board)
    {
        foreach (int tileIndex in board.GetPlayableTileIndices())
        {
            if (board.IsPaintable(tileIndex)) continue;

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

    #endregion

    #region Item Buttons

    private void SetupItemButtons(Board board)
    {
        List<ItemButton> buttons = paintUI.SetItemButtons(board.BoardData.AvailableItems, HandleItemButtonClicked);

        allItemButtons[PaintMode.Pencil] = new List<ItemButton>();
        allItemButtons[PaintMode.Eraser] = new List<ItemButton>();

        foreach (ItemButton button in buttons)
            allItemButtons[button.ItemMode].Add(button);

        availableItemButtons[PaintMode.Pencil] = new List<ItemButton>(allItemButtons[PaintMode.Pencil]);
        availableItemButtons[PaintMode.Eraser] = new List<ItemButton>(allItemButtons[PaintMode.Eraser]);
    }

    // 아이템 버튼 클릭은 토글이다: 이미 선택된 그 버튼을 다시 누르면 기본(Paint) 모드로 돌아간다.
    // 같은 종류의 버튼이 여러 개 있어도, 다른 버튼을 누르면 선택이 그 버튼으로 옮겨간다.
    private void HandleItemButtonClicked(ItemButton button)
    {
        if (_selectedItemButton == button)
        {
            SelectItemButton(null);
            SetMode(PaintMode.Paint);
            return;
        }

        SetMode(button.ItemMode);
        SelectItemButton(button);
    }

    private void SelectItemButton(ItemButton button)
    {
        if (_selectedItemButton != null) _selectedItemButton.SetSelected(false);
        _selectedItemButton = button;
        if (_selectedItemButton != null) _selectedItemButton.SetSelected(true);
    }

    // 유저가 선택해 둔 그 버튼을 소모한다(다른 버튼이 아니라 정확히 클릭했던 버튼). 선택된 버튼이 없거나 모드가 다르면 false.
    private bool ConsumeItem(PaintMode mode, out ItemButton consumedButton)
    {
        consumedButton = null;
        if (_selectedItemButton == null || _selectedItemButton.ItemMode != mode) return false;
        if (!availableItemButtons.TryGetValue(mode, out List<ItemButton> pool) || !pool.Remove(_selectedItemButton)) return false;

        consumedButton = _selectedItemButton;
        consumedButton.SetSelected(false, instant: true);
        consumedButton.gameObject.SetActive(false);

        SelectItemButton(null);
        SetMode(PaintMode.Paint);
        return true;
    }

    // Undo로 아이템 소모를 되돌린다. 선택 상태로는 되돌리지 않고, 다시 고를 수 있는 상태로만 되돌린다.
    private void RestoreItem(ItemButton button)
    {
        if (button == null) return;

        availableItemButtons[button.ItemMode].Add(button);
        button.gameObject.SetActive(true);
        button.SetSelected(false, instant: true);
    }

    // Reset 시 스테이지 로드 시점에 생성됐던 아이템 버튼 전체를 되살린다.
    private void ResetItemButtons()
    {
        SelectItemButton(null);

        foreach (KeyValuePair<PaintMode, List<ItemButton>> kvp in allItemButtons)
        {
            availableItemButtons[kvp.Key] = new List<ItemButton>(kvp.Value);
            foreach (ItemButton button in kvp.Value)
            {
                button.gameObject.SetActive(true);
                button.SetSelected(false, instant: true);
            }
        }
    }

    #endregion

    #region Tiles

    private PaintedController GetOrCreateTile(int id)
    {
        if (tileControllers[id] != null) return tileControllers[id];

        Board board = CurrentBoard;
        PaintedController tc = Instantiate(tileControllerPrefab, TileParent);
        tc.transform.position = board.GetWorldPosition(id);
        tc.Init(board.allTiles[id], id, board);
        tc.OnTilePainted += AddPaintAction;
        tileControllers[id] = tc;
        return tc;
    }

    // 칠해진 타일이나 Fixed 타일(연필로 만들었든 레벨(JSON)에서 왔든)을 지워 Empty로 되돌린다.
    // Wall/Hole/Start/End/Star/Key 등은 지우지 않고 표시만 갱신한다.
    private void ClearPaintedTile(int index)
    {
        PaintedController tc = tileControllers[index];
        if (tc == null) return;

        if (!tc.Tile.IsPainted && tc.Tile.type != TileType.Fixed)
        {
            tc.Refresh();
            return;
        }

        tc.ChangeTileType(0);
        Destroy(tc.gameObject);
        tileControllers[index] = null;
        userFixedTiles.Remove(index);
    }

    // Platformer 모드로 넘어갈 때 호출. Hole은 실제 HoleController가 대신 보여줘야 하므로
    // Paint 모드용 타일 오브젝트는 지운다.
    public void DestroyHoleTiles()
    {
        Board board = CurrentBoard;
        for (int i = 0; i < tileControllers.Count; i++)
        {
            if (tileControllers[i] == null || board.allTiles[i].type != TileType.Hole) continue;
            Destroy(tileControllers[i].gameObject);
            tileControllers[i] = null;
        }
    }

    // Paint 모드로 돌아올 때 호출. 지워졌던 Hole 타일 오브젝트를 다시 만든다.
    public void RecreateHoleTiles()
    {
        Board board = CurrentBoard;
        for (int i = 0; i < board.allTiles.Length; i++)
            if (board.allTiles[i].type == TileType.Hole)
                GetOrCreateTile(i);
    }

    #endregion

    #region Painting

    // Paint 버튼(그리드) 클릭은 전부 여기로 들어온다. 실제 동작은 현재 모드에 따라 갈린다.
    public void HandleTileClicked(int id)
    {
        switch (_currentMode)
        {
            case PaintMode.Paint: Paint(id); break;
            case PaintMode.Pencil: Pencil(id); break;
            case PaintMode.Eraser: Erase(id); break;
        }
    }

    public void SetMode(PaintMode mode)
    {
        if (_currentMode == mode) return;
        _currentMode = mode;
        OnPaintModeChanged?.Invoke(mode);
    }

    public void Paint(int id)
    {
        if (CurrentBoard.allTiles[id].type != TileType.Empty) return;

        PaintedController tc = GetOrCreateTile(id);
        tc.PaintTile(); // 내부에서 OnTilePainted -> AddPaintAction(id)로 이어져 undo 스택에 쌓인다.

        PaintCount++;
        OnPaintCountChanged?.Invoke(PaintCount);
        OnTilePainted?.Invoke(id);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRandomSFX(new[] { "paint_1", "paint_2" });
    }

    // 연필: 빈 타일을 고정(Fixed) 타일로 만든다. Paint와 마찬가지로 PaintCount에 포함되고 undo 가능하다.
    // 스테이지가 지정한 개수만큼만 사용 가능한 소모성 아이템이라, 남은 연필이 없으면 아무 일도 일어나지 않는다.
    public void Pencil(int id)
    {
        if (CurrentBoard.allTiles[id].type != TileType.Empty) return;
        if (!ConsumeItem(PaintMode.Pencil, out ItemButton consumedButton)) return;

        PaintedController tc = GetOrCreateTile(id);
        tc.ChangeTileType((int)TileType.Fixed);
        userFixedTiles.Add(id);

        AddPaintAction(id, consumedButton);

        PaintCount++;
        OnPaintCountChanged?.Invoke(PaintCount);
        OnTilePainted?.Invoke(id);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRandomSFX(new[] { "paint_1", "paint_2" });
    }

    // 지우개: Paint/Paint_Dec은 물론 Fixed 타일도 지울 수 있다 (연필로 만든 것이든 레벨(JSON)에 원래 있던 것이든 동일하게).
    // Wall/Hole/Start/End/Star/Key는 대상이 아니다. PaintCount는 건드리지 않는다
    // (지우고 다시 칠해서 이동 수를 줄이는 걸 막기 위함) — Undo로 복원해도 마찬가지.
    // 레벨 Fixed를 지워도 Reset을 누르면 originalTileTypes 기준으로 다시 복원된다.
    // 연필과 마찬가지로 스테이지가 지정한 개수만큼만 사용 가능한 소모성 아이템이다.
    public void Erase(int id)
    {
        Tile tile = CurrentBoard.allTiles[id];
        bool erasable = tile.IsPainted || tile.type == TileType.Fixed;
        if (!erasable) return;
        if (!ConsumeItem(PaintMode.Eraser, out ItemButton consumedButton)) return;

        TileSnapshot snapshot = new TileSnapshot
        {
            index = id,
            prevType = tile.type,
            prevColorId = tile.paint_color_id,
            wasUserFixed = userFixedTiles.Contains(id)
        };

        ClearPaintedTile(id);

        paintActions.Push(new TilePaintAction(new List<TileSnapshot> { snapshot }, countsTowardPaintCount: false, consumedItemButton: consumedButton));

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRandomSFX(new[] { "paint_1", "paint_2" });
    }

    // paintedTiles는 전부 Empty에서 새로 생긴 타일(Paint/Pencil/접기 결과)이므로, undo 시 전부 Empty로 되돌아간다.
    public void AddPaintAction(List<int> paintedTiles, bool countsTowardPaintCount = true)
        => AddPaintAction(paintedTiles, countsTowardPaintCount, null);

    private void AddPaintAction(List<int> paintedTiles, bool countsTowardPaintCount, ItemButton consumedItemButton)
    {
        List<TileSnapshot> snapshots = paintedTiles.ConvertAll(i => new TileSnapshot
        {
            index = i,
            prevType = TileType.Empty,
            prevColorId = -1,
            wasUserFixed = false
        });
        paintActions.Push(new TilePaintAction(snapshots, countsTowardPaintCount, consumedItemButton));
    }

    public void AddPaintAction(int paintedTileID) => AddPaintAction(new List<int> { paintedTileID });

    private void AddPaintAction(int paintedTileID, ItemButton consumedItemButton)
        => AddPaintAction(new List<int> { paintedTileID }, true, consumedItemButton);

    public void UndoPaintAction()
    {
        if (paintActions.Count == 0)
        {
            Debug.Log("No paint actions to undo");
            return;
        }

        TilePaintAction lastAction = paintActions.Pop();

        foreach (TileSnapshot snapshot in lastAction.Snapshots)
            RestoreSnapshot(snapshot);

        RestoreItem(lastAction.ConsumedItemButton);

        if (!lastAction.CountsTowardPaintCount) return;

        PaintCount--;
        OnPaintCountChanged?.Invoke(PaintCount);
    }

    // Undo 대상 스냅샷 하나를 복원한다. prevType이 Empty면 지우기(Paint/Pencil/접기 undo),
    // 그 외에는 지우개로 지우기 전 상태(타입/색상/연필여부)를 그대로 되살린다(Erase undo).
    private void RestoreSnapshot(TileSnapshot snapshot)
    {
        if (snapshot.prevType == TileType.Empty)
        {
            ClearPaintedTile(snapshot.index);
            return;
        }

        CurrentBoard.allTiles[snapshot.index].ChangeTileType(snapshot.prevType, snapshot.prevColorId);
        GetOrCreateTile(snapshot.index);

        if (snapshot.wasUserFixed) userFixedTiles.Add(snapshot.index);
    }

    public void ResetPaint()
    {
        SetMode(PaintMode.Paint);

        PaintCount = 0;
        OnPaintCountChanged?.Invoke(PaintCount);

        for (int i = 0; i < tileControllers.Count; i++)
            RestoreOriginalTile(i);

        ResumePaint();
        paintActions.Clear();
        userFixedTiles.Clear();
        ResetItemButtons();
    }

    // 지금 상태가 뭐든(칠해졌든, 연필로 그렸든, 레벨 Fixed가 지워졌든) 스테이지 로드 시점의 원래 타입으로 되돌린다.
    private void RestoreOriginalTile(int index)
    {
        TileType originalType = originalTileTypes[index];
        Tile tile = CurrentBoard.allTiles[index];

        if (tile.type == originalType)
        {
            if (tileControllers[index] != null) tileControllers[index].Refresh();
            return;
        }

        if (originalType == TileType.Empty)
        {
            ClearPaintedTile(index); // Paint/Paint_Dec/연필 Fixed였던 경우 -> Empty로
            return;
        }

        // 레벨 Fixed 등이 지워져서 Empty가 된 경우 -> 원래 타입으로 복원
        tile.ChangeTileType(originalType);
        GetOrCreateTile(index);
    }

    public void ResumePaint()
    {
        paintUI.SetBasePaintUI(true).Forget();
        SetCurtainBlocksActive(true);
        if (objectMarkers != null) objectMarkers.SetVisible(true);
    }

    public void PausePaint()
    {
        paintUI.SetBasePaintUI(false).Forget();
        SetCurtainBlocksActive(false);
        if (objectMarkers != null) objectMarkers.SetVisible(false);
    }

    public void SetPaperBoardSurfaceTransparent(bool transparent) => paperBoard.SetSurfaceTransparent(transparent);

    #endregion

    #region Folding

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
            () => CurrentBoard.FoldVertical());
    }

    public async void FoldHorizontal()
    {
        if (_isFolding) return;
        await ExecuteFold(
            (paperBoard.LowerLeft, paperBoard.UpperLeft),
            GetHorizontalAxisFoldRotation,
            HorizontalFoldAxis,
            GetHorizontalAxisCreaseRotation,
            () => CurrentBoard.FoldHorizontal());
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

    #endregion
}

// Undo 한 번(액션 하나)에 필요한 정보. 스냅샷마다 "되돌릴 때 복원할 상태"를 담고 있어서,
// Paint/Pencil/접기(Empty로 복원)와 지우개(지우기 전 상태로 복원) 모두 같은 구조로 undo할 수 있다.
public class TilePaintAction
{
    public List<TileSnapshot> Snapshots;
    public bool CountsTowardPaintCount; // undo할 때 PaintCount를 증감시킬지 (Erase 액션은 항상 false)
    public ItemButton ConsumedItemButton; // 이 액션으로 소모된 아이템 버튼(연필/지우개). 없으면 null

    public TilePaintAction(List<TileSnapshot> snapshots, bool countsTowardPaintCount = true, ItemButton consumedItemButton = null)
    {
        Snapshots = snapshots;
        CountsTowardPaintCount = countsTowardPaintCount;
        ConsumedItemButton = consumedItemButton;
    }
}

public struct TileSnapshot
{
    public int index;
    public TileType prevType;   // undo 시 복원할 타입 (Paint/Pencil/접기는 항상 Empty)
    public int prevColorId;     // Paint/Paint_Dec였다면 색상 id, 아니면 -1
    public bool wasUserFixed;   // 연필로 만든 Fixed였는지 (복원 시 userFixedTiles에 다시 등록하기 위함)
}
