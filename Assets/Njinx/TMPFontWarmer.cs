using TMPro;
using UnityEngine;

public class TMPFontWarmer : MonoBehaviour
{
    [SerializeField] TMP_FontAsset koreanFont;

    void Start()
    {
        koreanFont.ClearFontAssetData(true);
        foreach (var t in FindObjectsOfType<TMP_Text>())
            t.ForceMeshUpdate(true);
    }
}