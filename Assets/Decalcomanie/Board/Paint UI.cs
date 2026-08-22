using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;

public class PaintUI : MonoBehaviour
{
    [SerializeField] private Transform paintButtonParent;
    [SerializeField] private PaintButton paintButtonPrefab;
    [SerializeField] private BaseUI basePaintUI;
    [SerializeField] private BaseUI shadow;
    [SerializeField] private List<BaseUI> curtains;

    public void SetPaintButtons(List<int> tileIndices, System.Action<int> onPaintButtonClicked)
    {
        foreach (int tileIndex in tileIndices)
        {
            PaintButton newButton = Instantiate(paintButtonPrefab, paintButtonParent);
            newButton.SetPaintButtonID(tileIndex);
            newButton.onPaintButtonClicked = onPaintButtonClicked;
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

    public void SetCurtains(bool is_active)
    {
        foreach (var curtain in curtains)
        {
            if (is_active) curtain.ShowUI(.1f).Forget();
            else curtain.HideUI(.1f).Forget();
        }
    }
}
