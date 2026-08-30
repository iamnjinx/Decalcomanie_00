using UnityEngine;

/// <summary>
/// XY 평면에 절차적 메시를 생성하고, 중앙 라인을 기준으로
/// 가로 접기(y=0 기준) / 세로 접기(x=0 기준)를 수행한다.
/// 슬라이더 값 1 = 180도(완전히 포갬), 0.5 = 90도, 음수 = 반대 방향.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FoldablePlane : MonoBehaviour
{
    [Header("Mesh")]
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 2f;
    [SerializeField, Range(2, 200)] private int segmentsX = 20; // 짝수로 강제됨
    [SerializeField, Range(2, 200)] private int segmentsY = 20;

    [Header("Fold")]
    [Tooltip("가로 접기: 중앙 가로선(y=0) 기준으로 위쪽 절반을 접는다.")]
    [SerializeField, Range(-1f, 1f)] private float foldHorizontal = 0f;

    [Tooltip("세로 접기: 중앙 세로선(x=0) 기준으로 오른쪽 절반을 접는다.")]
    [SerializeField, Range(-1f, 1f)] private float foldVertical = 0f;

    [Tooltip("접힘선 주변을 부드럽게 휘게 한다. 0이면 칼주름. 단위는 월드 거리.")]
    [SerializeField, Min(0f)] private float creaseSoftness = 0f;

    [Tooltip("완전히 접혔을 때 Z-fighting을 막기 위한 두께 오프셋.")]
    [SerializeField] private float thickness = 0.002f;

    [Header("Optional")]
    [SerializeField] private bool updateMeshCollider = false;

    private Mesh mesh;
    private Vector3[] baseVerts;   // 접기 전 원본 (읽기 전용으로 유지)
    private Vector3[] workVerts;   // 매 프레임 갱신되는 결과
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    // 메시 재생성 필요 여부 판단용 캐시
    private float cachedW, cachedH;
    private int cachedSX, cachedSY;

    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        BuildMesh();
        ApplyFold();
    }

    private void OnValidate()
    {
        // 접힘선 위에 버텍스가 정확히 놓이도록 세그먼트는 짝수여야 한다.
        segmentsX = Mathf.Max(2, segmentsX / 2 * 2);
        segmentsY = Mathf.Max(2, segmentsY / 2 * 2);

        if (!isActiveAndEnabled) return;

        if (mesh == null || cachedW != width || cachedH != height ||
            cachedSX != segmentsX || cachedSY != segmentsY)
        {
            BuildMesh();
        }
        ApplyFold();
    }

    private void Update()
    {
        // 런타임에 값이 바뀌는 경우를 위해. 정적이라면 이 블록을 지우고
        // SetFold() 호출 시에만 갱신해도 된다.
        ApplyFold();
    }

    /// <summary>외부에서 접힘 값을 지정한다. (-1 ~ 1)</summary>
    public void SetFold(float horizontal, float vertical)
    {
        foldHorizontal = Mathf.Clamp(horizontal, -1f, 1f);
        foldVertical = Mathf.Clamp(vertical, -1f, 1f);
        ApplyFold();
    }

    // ────────────────────────────────────────────────────────────
    // 메시 생성
    // ────────────────────────────────────────────────────────────
    private void BuildMesh()
    {
        int vx = segmentsX + 1;
        int vy = segmentsY + 1;

        baseVerts = new Vector3[vx * vy];
        var uvs = new Vector2[vx * vy];
        var tris = new int[segmentsX * segmentsY * 6];

        for (int y = 0; y < vy; y++)
        {
            for (int x = 0; x < vx; x++)
            {
                int i = y * vx + x;
                float u = x / (float)segmentsX;
                float v = y / (float)segmentsY;

                baseVerts[i] = new Vector3((u - 0.5f) * width, (v - 0.5f) * height, 0f);
                uvs[i] = new Vector2(u, v);
            }
        }

        int t = 0;
        for (int y = 0; y < segmentsY; y++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int i0 = y * vx + x;
                int i1 = i0 + 1;
                int i2 = i0 + vx;
                int i3 = i2 + 1;

                // +Z를 바라보는 시계방향 와인딩
                tris[t++] = i0; tris[t++] = i2; tris[t++] = i1;
                tris[t++] = i2; tris[t++] = i3; tris[t++] = i1;
            }
        }

        mesh = new Mesh { name = "FoldablePlane" };
        mesh.MarkDynamic();
        if (baseVerts.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = baseVerts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        workVerts = (Vector3[])baseVerts.Clone();
        meshFilter.sharedMesh = mesh;

        cachedW = width; cachedH = height;
        cachedSX = segmentsX; cachedSY = segmentsY;
    }

    // ────────────────────────────────────────────────────────────
    // 접기 적용
    // ────────────────────────────────────────────────────────────
    private void ApplyFold()
    {
        if (mesh == null || baseVerts == null) return;

        float aH = foldHorizontal * Mathf.PI;   // 가로 접기 각도
        float aV = -foldVertical * Mathf.PI;    // 세로 접기 각도 (부호 반전: +값이 +Z로 접히도록)

        bool doH = Mathf.Abs(aH) > 1e-5f;
        bool doV = Mathf.Abs(aV) > 1e-5f;

        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 p = baseVerts[i];

            // 1) 가로 접기 — 중앙 가로선(X축) 기준 회전
            if (doH && p.y > 0f)
            {
                float w = Weight(p.y);
                float ang = aH * w;
                float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
                float y = p.y, z = p.z;
                p.y = y * c - z * s;
                p.z = y * s + z * c;
                p.z += thickness * foldHorizontal * w;
            }

            // 2) 세로 접기 — 중앙 세로선(Y축) 기준 회전
            //    x는 1단계에서 변하지 않으므로 원본 x 부호로 판정해도 동일하다.
            if (doV && p.x > 0f)
            {
                float w = Weight(p.x);
                float ang = aV * w;
                float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
                float x = p.x, z = p.z;
                p.x = x * c + z * s;
                p.z = -x * s + z * c;
                p.z += thickness * foldVertical * w;
            }

            workVerts[i] = p;
        }

        mesh.vertices = workVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (updateMeshCollider && meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    /// <summary>접힘선으로부터의 거리에 따른 회전 가중치. creaseSoftness=0이면 항상 1(칼주름).</summary>
    private float Weight(float distanceFromCrease)
    {
        if (creaseSoftness <= 1e-5f) return 1f;
        return Mathf.Clamp01(distanceFromCrease / creaseSoftness);
    }

    private void OnDisable()
    {
        if (mesh != null && !Application.isPlaying)
            DestroyImmediate(mesh);
    }
}
