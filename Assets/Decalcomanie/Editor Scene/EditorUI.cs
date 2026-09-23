using System;
using System.Collections.Generic;
using Njinx.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Editor Scene의 UI 참조와 뷰 로직을 전담한다. 보드 데이터/저장 로직은 EditorManager가 갖는다.
public class EditorUI : MonoBehaviour
{
    [SerializeField] private ButtonUI quitButton;

    [SerializeField] private Transform droppableParent;

    public ButtonUI ResetButton;
    public ButtonUI SwitchButton;
    public ButtonUI SaveButton;

    public ButtonUI presetButton1;

    [Header("Adjust Board Size")]
    public VerticalLayoutGroup horizontalLineParent;
    public VerticalLayoutGroup verticalLineParent;
    public GameObject linePrefab;
    public ButtonUI increaseSizeButton;
    public ButtonUI decreaseSizeButton;
    public TextMeshProUGUI curSizeText;

    [Header("Item Settings")]
    // [0]이 증가(+), [1]이 감소(-) 버튼이다. 인스펙터에서 순서를 바꾸면 +/-도 같이 바뀐다.
    public ButtonUI[] item_Pencil_buttons;
    public ButtonUI[] item_Eraser_buttons;
    public TextMeshProUGUI pencilCountText;
    public TextMeshProUGUI eraserCountText;

    // 보드(Tile Grid)의 가로/세로 픽셀 크기와 line prefab의 두께.
    // 라인 간격 = boardPixelSize / boardSize - lineThickness (6:127, 8:94, 10:74)
    [SerializeField] private float boardPixelSize = 800f;
    [SerializeField] private float lineThickness = 6f;

    [Header("Background")]
    // 배경 목록은 GameData.chapterBackgroundSprites를 그대로 쓴다. 개수는 하드코딩하지 않는다.
    public ButtonUI backgroundButton;
    // 선택한 배경을 에디터에서 확인하기 위한 미리보기(선택 사항).
    [SerializeField] private Image backgroundPreviewImage;

    // 버튼 클릭을 EditorManager로 넘기는 통로.
    public event Action OnQuitClicked;
    public event Action OnResetClicked;
    public event Action OnSwitchClicked;
    public event Action OnSaveClicked;
    public event Action OnPresetClicked;
    public event Action OnIncreaseSizeClicked;
    public event Action OnDecreaseSizeClicked;
    public event Action OnBackgroundClicked;
    // 아이템 개수 증감. 인자는 +1 또는 -1이다.
    public event Action<int> OnPencilCountDelta;
    public event Action<int> OnEraserCountDelta;

    // 아이템 버튼처럼 인덱스별 핸들러를 걸어 둔 구독을 OnDestroy에서 한꺼번에 되돌리기 위한 목록.
    private readonly List<Action> unbindActions = new List<Action>();

    // 타일(EditorTileDroppable)이 생성될 부모. 생성 자체는 EditorManager가 한다.
    public Transform DroppableParent => droppableParent;

    void Start()
    {
        Bind(quitButton, RaiseQuit);
        Bind(ResetButton, RaiseReset);
        Bind(SwitchButton, RaiseSwitch);
        Bind(SaveButton, RaiseSave);
        Bind(presetButton1, RaisePreset);
        Bind(increaseSizeButton, RaiseIncreaseSize);
        Bind(decreaseSizeButton, RaiseDecreaseSize);
        // 배경 버튼은 씬에 아직 없을 수 있으므로 null을 허용한다.
        Bind(backgroundButton, RaiseBackground);

        BindCountButtons(item_Pencil_buttons, RaisePencilCountDelta);
        BindCountButtons(item_Eraser_buttons, RaiseEraserCountDelta);
    }

    void OnDestroy()
    {
        Unbind(quitButton, RaiseQuit);
        Unbind(ResetButton, RaiseReset);
        Unbind(SwitchButton, RaiseSwitch);
        Unbind(SaveButton, RaiseSave);
        Unbind(presetButton1, RaisePreset);
        Unbind(increaseSizeButton, RaiseIncreaseSize);
        Unbind(decreaseSizeButton, RaiseDecreaseSize);
        Unbind(backgroundButton, RaiseBackground);

        foreach (Action unbind in unbindActions) unbind();
        unbindActions.Clear();
    }

    private static void Bind(ButtonUI button, Action handler)
    {
        if (button == null) return;
        button.OnSingleClick += handler;
    }

    private static void Unbind(ButtonUI button, Action handler)
    {
        if (button == null) return;
        button.OnSingleClick -= handler;
    }

    // 배열의 첫 번째 버튼이 +1, 나머지가 -1이다.
    private void BindCountButtons(ButtonUI[] buttons, Action<int> raise)
    {
        if (buttons == null) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            ButtonUI button = buttons[i];
            if (button == null) continue;

            int delta = i == 0 ? 1 : -1;
            Action handler = () => raise(delta);
            button.OnSingleClick += handler;
            unbindActions.Add(() => button.OnSingleClick -= handler);
        }
    }

    private void RaiseQuit() => OnQuitClicked?.Invoke();
    private void RaiseReset() => OnResetClicked?.Invoke();
    private void RaiseSwitch() => OnSwitchClicked?.Invoke();
    private void RaiseSave() => OnSaveClicked?.Invoke();
    private void RaisePreset() => OnPresetClicked?.Invoke();
    private void RaiseIncreaseSize() => OnIncreaseSizeClicked?.Invoke();
    private void RaiseDecreaseSize() => OnDecreaseSizeClicked?.Invoke();
    private void RaiseBackground() => OnBackgroundClicked?.Invoke();
    private void RaisePencilCountDelta(int delta) => OnPencilCountDelta?.Invoke(delta);
    private void RaiseEraserCountDelta(int delta) => OnEraserCountDelta?.Invoke(delta);

    // 보드 칸 수가 바뀔 때 그리드 셀 크기, 격자 라인, 사이즈 텍스트를 한 번에 맞춘다.
    public void ApplyBoardLayout(int boardSize)
    {
        ApplyGridCellSize(boardSize);
        CreateBoardLines(boardSize);
        SetSizeText(boardSize);
    }

    // 보드 전체 크기는 고정이므로 타일 한 칸의 크기를 boardSize에 맞춰 줄인다.
    private void ApplyGridCellSize(int boardSize)
    {
        if (droppableParent == null) return;

        var grid = droppableParent.GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        float cellSize = boardPixelSize / boardSize;
        grid.cellSize = new Vector2(cellSize, cellSize);
    }

    private void CreateBoardLines(int boardSize)
    {
        // 칸 하나의 간격에서 라인 두께를 뺀 값이 라인 사이 간격이 된다.
        float spacing = boardPixelSize / boardSize - lineThickness;
        CreateLines(horizontalLineParent, boardSize, spacing);
        CreateLines(verticalLineParent, boardSize, spacing);
    }

    private void CreateLines(VerticalLayoutGroup lineParent, int boardSize, float spacing)
    {
        if (lineParent == null || linePrefab == null) return;

        Transform parent = lineParent.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        lineParent.spacing = spacing;

        // 칸이 boardSize개면 라인은 양쪽 끝을 포함해 boardSize + 1개다.
        for (int i = 0; i <= boardSize; i++)
            Instantiate(linePrefab, parent);
    }

    public void SetSizeText(int boardSize)
    {
        if (curSizeText != null)
            curSizeText.text = boardSize.ToString();
    }

    // 저장/플레이 시 이 개수만큼 아이템 버튼이 생긴다.
    public void SetItemCountTexts(int pencilCount, int eraserCount)
    {
        if (pencilCountText != null)
            pencilCountText.text = pencilCount.ToString();

        if (eraserCountText != null)
            eraserCountText.text = eraserCount.ToString();
    }

    public void SetBackgroundPreview(Sprite sprite)
    {
        if (backgroundPreviewImage != null)
            backgroundPreviewImage.sprite = sprite;
    }
}
