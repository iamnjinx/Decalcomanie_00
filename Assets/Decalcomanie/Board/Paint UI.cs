using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;

public class PaintUI : MonoBehaviour
{
    [SerializeField] private Transform paintButtonParent;
    [SerializeField] private PaintButton paintButtonPrefab;
    [SerializeField] private BaseUI basePaintUI;
    [SerializeField] private BaseUI shadow;
    //[SerializeField] private List<BaseUI> curtains;

    public void SetPaintButtons(Board board, System.Action<int> onPaintButtonClicked)
    {
        foreach (int tileIndex in board.GetPlayableTileIndices())
        {
            PaintButton newButton = Instantiate(paintButtonPrefab, paintButtonParent);
            newButton.SetPaintButtonID(tileIndex);
            newButton.onPaintButtonClicked = onPaintButtonClicked;

            // 화면 좌우가 board의 Quadrant 좌표계와 반전되어 있어(상하는 동일),
            // 화면상 Upper Left/Lower Right에 해당하는 실제 좌표는 Quadrant1/Quadrant3.
            bool isUpperLeftOrLowerRight = board.Quadrant1.Contains(tileIndex) || board.Quadrant3.Contains(tileIndex);
            newButton.SetBlocked(!isUpperLeftOrLowerRight);
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

    public void SetShadow(bool is_active)
    {
        if (is_active) shadow.ShowUI(.1f).Forget();
        else shadow.HideUI(.1f).Forget();
    }
}
