using System;
using Njinx.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Demo" 그룹: 데모 안내 UI와 스토어 링크 버튼을 담당한다.
/// </summary>
[Serializable]
public class StageDemoView
{
    [SerializeField] private GameObject demoRoot;
    [SerializeField] private Image demoImage;
    [SerializeField] private ButtonUI demoButton;
    [SerializeField] private string demoLinkUrl;

    /// <summary>데모 이미지를 현재 언어에 맞게 설정하고 링크 버튼을 연결한다.</summary>
    public void Initialize()
    {
        demoButton.OnSingleClick += () => Application.OpenURL(demoLinkUrl);
        demoImage.sprite = GameManager.Instance.CurrentLocalizedData.demoSprite;
    }

    /// <summary>데모 빌드이면서 마지막 챕터일 때만 데모 UI를 노출한다.</summary>
    public void UpdateVisibility(int chapterIndex, int maxChapterIndex)
    {
        if (demoRoot == null) return;
        demoRoot.SetActive(GameManager.Instance.IsDemo && chapterIndex == maxChapterIndex);
    }
}
