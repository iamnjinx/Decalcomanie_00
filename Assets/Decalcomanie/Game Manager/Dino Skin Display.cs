using DG.Tweening;
using UnityEngine;

// 저장된 skinID에 해당하는 공룡 스킨을 SpriteRenderer에 적용한다.
// Paint 마커, 플레이어 등 스킨을 보여주는 곳에서 공통으로 쓰기 위한 컴포넌트.
// 컴포넌트를 붙이기 애매한 곳에서는 static 헬퍼(GetSkin/GetCurrentSkin)만 써도 된다.
public class DinoSkinDisplay : MonoBehaviour
{
    [SerializeField] private SpriteRenderer skinRenderer;
    [SerializeField] private DinoSkinSO dinoSkinSO;

    // 스킨이 없을 때(skinID == -1) GameObject 자체를 끌지, SpriteRenderer만 끌지.
    [SerializeField] private bool disableGameObjectWhenEmpty = true;

    [Header("Punch")]
    // 스킨이 나타나는 순간 스탬프와 같은 통통 튀는 연출을 준다. (Stage UI의 스탬프 연출과 동일한 값)
    [SerializeField] private bool punchOnShow = true;
    [SerializeField] private float punchStrength = 0.3f;
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private int punchVibrato = 6;
    [SerializeField] private float punchElasticity = 0.5f;

    private Vector3 baseScale = Vector3.one;
    private bool baseScaleCached;

    private void Reset()
    {
        skinRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        CacheBaseScale();
    }

    private void OnDestroy()
    {
        if (skinRenderer != null)
            skinRenderer.transform.DOKill();
    }

    // 펀치 연출은 localScale을 건드리므로 원래 크기를 한 번만 기억해 둔다.
    private void CacheBaseScale()
    {
        if (baseScaleCached || skinRenderer == null) return;
        baseScale = skinRenderer.transform.localScale;
        baseScaleCached = true;
    }

    // 스킨이 방금 나타났을 때 호출. 이전 펀치가 남아 있으면 지우고 원래 크기에서 다시 시작한다.
    public void PlayPunch()
    {
        if (skinRenderer == null) return;

        CacheBaseScale();

        Transform target = skinRenderer.transform;
        target.DOKill();
        target.localScale = baseScale;
        target.DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato, punchElasticity);
    }

    // 저장된 skinID 기준으로 갱신. 애니메이션 이벤트에서 호출하는 진입점.
    // 애니메이션 이벤트는 이름으로만 메서드를 찾으므로 이 클래스에서 오버로드를 만들지 말 것.
    public void Apply()
    {
        ApplySkin(GameProgressData.Load().skinID);
    }

    // 특정 skinID로 갱신. 잠긴/없는 ID면 스킨을 숨긴다.
    public void ApplySkin(int skinID)
    {
        if (skinRenderer == null) return;

        Sprite skin = GetSkin(dinoSkinSO, skinID);
        skinRenderer.sprite = skin;

        if (disableGameObjectWhenEmpty)
            skinRenderer.gameObject.SetActive(skin != null);
        else
            skinRenderer.enabled = skin != null;

        // 활성화된 뒤에 재생해야 트윈이 정상 동작한다.
        if (punchOnShow && skin != null)
            PlayPunch();
    }

    // 인덱스 범위/null을 모두 걸러낸 뒤 스프라이트를 돌려준다. 없으면 null.
    public static Sprite GetSkin(DinoSkinSO dinoSkinSO, int skinID)
    {
        if (dinoSkinSO == null || dinoSkinSO.dinoSkins == null) return null;
        if (skinID < 0 || skinID >= dinoSkinSO.dinoSkins.Length) return null;
        return dinoSkinSO.dinoSkins[skinID];
    }

    // 현재 저장된 skinID의 스프라이트. 선택된 스킨이 없으면 null.
    public static Sprite GetCurrentSkin(DinoSkinSO dinoSkinSO)
    {
        return GetSkin(dinoSkinSO, GameProgressData.Load().skinID);
    }
}
