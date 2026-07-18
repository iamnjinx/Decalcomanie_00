using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;

public class PaintUI : MonoBehaviour
{
    [SerializeField] private Transform paintButtonParent;
    [SerializeField] private PaintButton paintButtonPrefab;
    [SerializeField] private BaseUI basePaintUI;
    [SerializeField] private List<BaseUI> curtains;

    public void SetPaintButtons(int count, System.Action<int> onPaintButtonClicked)
    {
        for (int i = 0; i < count; i++)
        {
            PaintButton newButton = Instantiate(paintButtonPrefab, paintButtonParent);
            newButton.SetPaintButtonID(i);
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

    public void SetCurtains(bool is_active)
    {
        foreach (var curtain in curtains)
        {
            if (is_active) curtain.ShowUI(.1f).Forget();
            else curtain.HideUI(.1f).Forget();
        }
    }
}
