using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

public class PaintUI : MonoBehaviour
{
    [SerializeField] private Transform paintButtonParent;
    [SerializeField] private PaintButton paintButtonPrefab;
    [SerializeField] private BaseUI basePaintUI;

    [SerializeField] private Transform lineHorizontalParent;
    [SerializeField] private Transform lineVerticalParent;
    [SerializeField] private Image linePrefab;

    [Header("Item Buttons")]
    // 프리팹이 아니라 씬에 미리 배치해 둔 버튼을 그대로 쓴다. 인스펙터에서 직접 연결할 것.
    [SerializeField] private ItemButton pencilItemButton;
    [SerializeField] private ItemButton eraserItemButton;

    [SerializeField] private float gridPixelSize = 116.5f * 8f; // 8x8 기준 grid 전체 크기(px). 보드 크기가 달라져도 이 크기를 유지하도록 셀 크기를 매번 재계산한다.

    private GridLayoutGroup paintButtonGrid;
    private VerticalLayoutGroup lineHorizontalLayout;
    private VerticalLayoutGroup lineVerticalLayout;

    public void SetPaintButtons(Board board, System.Action<int> onPaintButtonClicked)
    {
        if (paintButtonGrid == null) paintButtonGrid = paintButtonParent.GetComponent<GridLayoutGroup>();
        if (paintButtonGrid != null)
        {
            paintButtonGrid.constraintCount = board.playableSize;
            float cellSize = gridPixelSize / board.playableSize;
            paintButtonGrid.cellSize = new Vector2(cellSize, cellSize);
        }

        if (lineHorizontalLayout == null) lineHorizontalLayout = lineHorizontalParent.GetComponent<VerticalLayoutGroup>();
        if (lineVerticalLayout == null) lineVerticalLayout = lineVerticalParent.GetComponent<VerticalLayoutGroup>();

        // 칸이 board.playableSize개면 경계선(라인)은 그보다 1개 많은 board.playableSize+1개 필요(펜스포스트).
        // 라인 전체 길이(gridPixelSize)는 보드 크기와 무관하게 고정이므로, 그 안에 라인 개수만큼 채우도록 spacing을 역산한다.
        int lineCount = board.playableSize + 1;
        float lineHeight = linePrefab.rectTransform.sizeDelta.y;
        float lineSpacing = (gridPixelSize - lineCount * lineHeight) / board.playableSize;
        if (lineHorizontalLayout != null) lineHorizontalLayout.spacing = lineSpacing;
        if (lineVerticalLayout != null) lineVerticalLayout.spacing = lineSpacing;

        for (int i = 0; i < lineCount; i++)
        {
            Instantiate(linePrefab, lineHorizontalParent);
            Instantiate(linePrefab, lineVerticalParent);
        }

        foreach (int tileIndex in board.GetPlayableTileIndices())
        {
            PaintButton newButton = Instantiate(paintButtonPrefab, paintButtonParent);
            newButton.SetPaintButtonID(tileIndex);
            newButton.onPaintButtonClicked = onPaintButtonClicked;

            newButton.SetBlocked(!board.IsPaintable(tileIndex));
        }
    }

    // 스테이지 JSON의 AvailableItems에서 종류별 개수를 세어, 씬에 미리 배치된 아이템 버튼에 반영한다.
    // 버튼은 Instantiate/Destroy하지 않는다. 소모/복구/Undo/Reset 처리는 PaintManager가 맡는다.
    public Dictionary<PaintManager.PaintMode, ItemButton> SetItemButtons(
        List<string> availableItems, System.Action<ItemButton> onItemButtonClicked)
    {
        Dictionary<PaintManager.PaintMode, ItemButton> buttons = new Dictionary<PaintManager.PaintMode, ItemButton>();

        foreach (KeyValuePair<PaintManager.PaintMode, int> kvp in CountItems(availableItems))
        {
            ItemButton button = GetItemButton(kvp.Key);
            if (button == null) continue;

            button.SetItemMode(kvp.Key);
            button.onItemButtonClicked = onItemButtonClicked;

            // 스테이지가 아예 주지 않는 아이템은 자리도 차지하지 않도록 끈다(파괴하지는 않는다).
            button.gameObject.SetActive(kvp.Value > 0);
            button.SetCount(kvp.Value);

            buttons[kvp.Key] = button;
        }
        return buttons;
    }

    // AvailableItems는 개수만큼 같은 이름이 반복된 목록이다. 예: ["pencil", "pencil", "eraser"] -> 연필 2, 지우개 1.
    private static Dictionary<PaintManager.PaintMode, int> CountItems(List<string> availableItems)
    {
        Dictionary<PaintManager.PaintMode, int> counts = new Dictionary<PaintManager.PaintMode, int>
        {
            { PaintManager.PaintMode.Pencil, 0 },
            { PaintManager.PaintMode.Eraser, 0 },
        };

        if (availableItems == null) return counts;

        foreach (string itemName in availableItems)
        {
            PaintManager.PaintMode mode = ParseItemMode(itemName);
            if (!counts.ContainsKey(mode))
            {
                Debug.LogWarning($"[PaintUI] Unknown item type in stage JSON: {itemName}");
                continue;
            }
            counts[mode]++;
        }
        return counts;
    }

    private ItemButton GetItemButton(PaintManager.PaintMode mode)
    {
        switch (mode)
        {
            case PaintManager.PaintMode.Pencil: return pencilItemButton;
            case PaintManager.PaintMode.Eraser: return eraserItemButton;
            default: return null;
        }
    }

    private static PaintManager.PaintMode ParseItemMode(string itemName)
    {
        switch (itemName?.ToLowerInvariant())
        {
            case "pencil": return PaintManager.PaintMode.Pencil;
            case "eraser": return PaintManager.PaintMode.Eraser;
            default: return PaintManager.PaintMode.Paint;
        }
    }

    public async UniTask SetBasePaintUI(bool is_active)
    {
        if (is_active)
        {
            await basePaintUI.ShowUI(.1f);
        }
        else
        {
            await basePaintUI.HideUI(.1f);
        }
    }
}
