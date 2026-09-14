using Njinx.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorPlayManager : MonoBehaviour
{
    public ButtonUI ExitButton;

    [Header("Background")]
    // 배경 Plane의 MeshRenderer. EditorManager에서 고른 배경으로 텍스처만 갈아끼운다.
    [SerializeField] private MeshRenderer backgroundRenderer;

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    void Start()
    {
        ExitButton.OnSingleClick += Exit;

        ApplyBackground(EditorManager.SavedBackgroundId);
    }

    private void ApplyBackground(int backgroundId)
    {
        if (backgroundRenderer == null) return;

        var sprites = EditorManager.BackgroundSprites;
        if (sprites == null || sprites.Count == 0) return;

        backgroundId = Mathf.Clamp(backgroundId, 0, sprites.Count - 1);
        var sprite = sprites[backgroundId];
        if (sprite == null || sprite.texture == null) return;

        // sharedMaterial을 건드리면 에셋 원본(BackgroundMat)이 더럽혀지므로 인스턴스를 쓴다.
        var material = backgroundRenderer.material;
        if (material.HasProperty(BaseMapId)) material.SetTexture(BaseMapId, sprite.texture);
        if (material.HasProperty(MainTexId)) material.SetTexture(MainTexId, sprite.texture);
    }

    private void Exit()
    {
        SceneManager.LoadScene("Editor Scene");
    }
}
