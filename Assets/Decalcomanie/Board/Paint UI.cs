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
    [SerializeField] private Transform itemButtonContainer;
    [SerializeField] private ItemButton pencilItemButtonPrefab;
    [SerializeField] private ItemButton eraserItemButtonPrefab;

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

    // 스테이지 JSON의 AvailableItems 순서대로 아이템 버튼을 itemButtonContainer 아래에 생성한다.
    // 생성된 버튼들의 소모/복구/Undo/Reset 처리는 PaintManager가 맡는다.
    public List<ItemButton> SetItemButtons(List<string> availableItems, System.Action<ItemButton> onItemButtonClicked)
    {
        foreach (Transform child in itemButtonContainer)
            Destroy(child.gameObject);

        List<ItemButton> buttons = new List<ItemButton>();
        if (availableItems == null) return buttons;

        foreach (string itemName in availableItems)
        {
            PaintManager.PaintMode mode = ParseItemMode(itemName);
            ItemButton prefab = GetItemButtonPrefab(mode);
            if (prefab == null)
            {
                Debug.LogWarning($"[PaintUI] Unknown item type in stage JSON: {itemName}");
                continue;
            }

            ItemButton newButton = Instantiate(prefab, itemButtonContainer);
            newButton.SetItemMode(mode);
            newButton.onItemButtonClicked = onItemButtonClicked;
            buttons.Add(newButton);
        }
        return buttons;
    }

    private ItemButton GetItemButtonPrefab(PaintManager.PaintMode mode)
    {
        switch (mode)
        {
            case PaintManager.PaintMode.Pencil: return pencilItemButtonPrefab;
            case PaintManager.PaintMode.Eraser: return eraserItemButtonPrefab;
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
