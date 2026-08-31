using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PaperBoard : MonoBehaviour
{
    private const float SurfaceOpaque = 0f;
    private const float SurfaceTransparent = 1f;

    [SerializeField] private Transform lowerLeft;
    [SerializeField] private Transform upperLeft;
    [SerializeField] private Transform lowerRight;
    [SerializeField] private Transform upperRight;

    [SerializeField] List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    private List<Material> panelMaterials = new List<Material>();

    public Transform LowerLeft => lowerLeft;
    public Transform UpperLeft => upperLeft;
    public Transform LowerRight => lowerRight;
    public Transform UpperRight => upperRight;

    private void EnsurePanelMaterials()
    {
        if (panelMaterials.Count > 0) return;
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            if (meshRenderer == null) continue;
            panelMaterials.Add(meshRenderer.material);
        }
    }

    // URP Lit 셰이더 기준으로 패널 머티리얼들의 Surface Type을 Opaque <-> Transparent로 전환합니다.
    public void SetSurfaceTransparent(bool transparent)
    {
        EnsurePanelMaterials();
        foreach (Material material in panelMaterials)
        {
            ApplySurfaceType(material, transparent);
        }
    }

    public void ToggleSurfaceType()
    {
        EnsurePanelMaterials();
        if (panelMaterials.Count == 0) return;

        bool isTransparent = panelMaterials[0].GetFloat("_Surface") == SurfaceTransparent;
        SetSurfaceTransparent(!isTransparent);
    }

    private static void ApplySurfaceType(Material material, bool transparent)
    {
        if (material == null) return;

        if (transparent)
        {
            material.SetFloat("_Surface", SurfaceTransparent);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            material.SetFloat("_Surface", SurfaceOpaque);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Geometry;
        }
    }
}
