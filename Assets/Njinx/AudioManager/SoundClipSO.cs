using UnityEngine;

/// <summary>
/// 사운드 클립 ScriptableObject
/// 
/// 사용법:
/// 1. Project 창에서 우클릭 → Create → Audio → Sound Clip
/// 2. 생성된 에셋에서 클립, 볼륨, 피치 등 설정
/// 3. AudioManager의 BGM/SFX 배열에 드래그하여 등록
/// 
/// 장점:
/// - 각 사운드가 독립 에셋 → 여러 곳에서 재사용 가능
/// - 프리팹 수정 없이 사운드 밸런싱 가능
/// - 에셋 검색/필터링으로 관리 편리
/// - 팀 작업 시 머지 충돌 최소화
/// </summary>
[CreateAssetMenu(fileName = "NewSoundClip", menuName = "Audio/Sound Clip", order = 0)]
public class SoundClipSO : ScriptableObject
{
    [Header("=== 기본 설정 ===")]
    [Tooltip("코드에서 이 클립을 참조할 고유 키 (예: Footstep_01, BGM_MainTheme)")]
    public string key;

    [Tooltip("실제 오디오 클립")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("기본 볼륨")]
    public float volume = 1f;

    [Range(0.1f, 3f)]
    [Tooltip("기본 피치")]
    public float pitch = 1f;

    [Range(0, 256)]
    [Tooltip("우선순위 (0=최고, 256=최저)")]
    public int priority = 128;

    [Header("=== 3D 설정 ===")]
    [Tooltip("체크하면 아래 커스텀 3D 범위를 사용합니다")]
    public bool useCustom3DRange = false;

    [Tooltip("3D 최소 거리 (이 거리 안에서 최대 볼륨)")]
    public float minDistance3D = 1f;

    [Tooltip("3D 최대 거리 (이 거리 밖에서 무음)")]
    public float maxDistance3D = 50f;

    /// <summary>
    /// 커스텀 3D 최소 거리 반환 (미사용 시 null)
    /// </summary>
    public float? GetMinDistance() => useCustom3DRange ? minDistance3D : null;

    /// <summary>
    /// 커스텀 3D 최대 거리 반환 (미사용 시 null)
    /// </summary>
    public float? GetMaxDistance() => useCustom3DRange ? maxDistance3D : null;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        // key가 비어있으면 에셋 이름으로 자동 채움
        if (string.IsNullOrEmpty(key))
        {
            key = name;
        }
    }
    #endif
}