using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 유니티 오디오 매니저 - 소규모 인디 프로젝트용 올인원 솔루션
/// 
/// 핵심 기능:
/// 1. BGM 전환 / 크로스페이드
/// 2. 3D 사운드 / 공간 오디오
/// 3. 대량 SFX 동시 재생 관리 (오브젝트 풀링)
/// 4. 볼륨/설정 저장 시스템 (PlayerPrefs)
/// 
/// 사용법:
///   AudioManager.Instance.PlayBGM("MainTheme");
///   AudioManager.Instance.PlaySFX("Explosion");
///   AudioManager.Instance.PlaySFX3D("Footstep", transform.position);
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region ── Singleton ──────────────────────────────────────────

    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    Debug.LogWarning("[AudioManager] 씬에 AudioManager가 없습니다!");
                }
            }
            return _instance;
        }
    }

    #endregion

    #region ── Inspector 설정 ─────────────────────────────────────

    [Header("=== Audio Mixer (선택사항) ===")]
    [Tooltip("AudioMixer를 사용하면 더 세밀한 볼륨 제어가 가능합니다")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string bgmVolumeParam = "BGMVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    [Header("=== 오디오 클립 라이브러리 ===")]
    [SerializeField] private SoundClipSO[] bgmClips;
    [SerializeField] private SoundClipSO[] sfxClips;

    [Header("=== BGM 설정 ===")]
    [SerializeField] private float crossfadeDuration = 1.5f;

    [Header("=== SFX 풀링 설정 ===")]
    [SerializeField] private int initialPoolSize = 16;
    [SerializeField] private int maxPoolSize = 32;
    [SerializeField] private int maxConcurrentSameSFX = 3;  // 동일 SFX 동시 재생 제한

    [Header("=== 3D 사운드 기본 설정 ===")]
    [SerializeField] private float default3DMinDistance = 1f;
    [SerializeField] private float default3DMaxDistance = 50f;
    [SerializeField] private AudioRolloffMode defaultRolloff = AudioRolloffMode.Logarithmic;

    #endregion

    #region ── 내부 변수 ──────────────────────────────────────────

    // BGM용 듀얼 AudioSource (크로스페이드)
    private AudioSource _bgmSourceA;
    private AudioSource _bgmSourceB;
    private AudioSource _activeBgmSource;
    private Coroutine _crossfadeCoroutine;
    private string _currentBgmKey;

    // SFX 오브젝트 풀
    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private readonly List<AudioSource> _activeSfxSources = new List<AudioSource>();
    private Transform _sfxPoolParent;

    // 클립 딕셔너리 (O(1) 검색)
    private readonly Dictionary<string, SoundClipSO> _bgmDict = new Dictionary<string, SoundClipSO>();
    private readonly Dictionary<string, SoundClipSO> _sfxDict = new Dictionary<string, SoundClipSO>();

    // 동일 SFX 동시 재생 카운트
    private readonly Dictionary<string, int> _sfxPlayCount = new Dictionary<string, int>();

    // 볼륨 설정
    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;
    private bool _isMuted = false;

    // PlayerPrefs 키
    private const string PREF_MASTER_VOL = "Audio_MasterVolume";
    private const string PREF_BGM_VOL = "Audio_BGMVolume";
    private const string PREF_SFX_VOL = "Audio_SFXVolume";
    private const string PREF_MUTED = "Audio_IsMuted";

    #endregion

    #region ── 프로퍼티 ───────────────────────────────────────────

    public float MasterVolume => _masterVolume;
    public float BgmVolume => _bgmVolume;
    public float SfxVolume => _sfxVolume;
    public bool IsMuted => _isMuted;
    public string CurrentBGMKey => _currentBgmKey;
    public bool IsBGMPlaying => _activeBgmSource != null && _activeBgmSource.isPlaying;

    #endregion

    #region ── 라이프사이클 ───────────────────────────────────────

    private void Awake()
    {
        // 싱글톤 보장
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDictionaries();
        InitializeBGMSources();
        InitializeSFXPool();
        LoadVolumeSettings();
    }

    private void Update()
    {
        // 재생이 끝난 SFX AudioSource를 풀로 반환
        ReturnFinishedSFXToPool();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region ── 초기화 ─────────────────────────────────────────────

    private void InitializeDictionaries()
    {
        _bgmDict.Clear();
        if (bgmClips != null)
        {
            foreach (var clip in bgmClips)
            {
                if (clip.clip == null)
                {
                    Debug.LogWarning($"[AudioManager] BGM '{clip.key}' 에 AudioClip이 할당되지 않았습니다.");
                    continue;
                }
                if (!_bgmDict.TryAdd(clip.key, clip))
                {
                    Debug.LogWarning($"[AudioManager] 중복된 BGM 키: {clip.key}");
                }
            }
        }

        _sfxDict.Clear();
        if (sfxClips != null)
        {
            foreach (var clip in sfxClips)
            {
                if (clip.clip == null)
                {
                    Debug.LogWarning($"[AudioManager] SFX '{clip.key}' 에 AudioClip이 할당되지 않았습니다.");
                    continue;
                }
                if (!_sfxDict.TryAdd(clip.key, clip))
                {
                    Debug.LogWarning($"[AudioManager] 중복된 SFX 키: {clip.key}");
                }
            }
        }
    }

    private void InitializeBGMSources()
    {
        var bgmParent = new GameObject("BGM_Sources");
        bgmParent.transform.SetParent(transform);

        _bgmSourceA = bgmParent.AddComponent<AudioSource>();
        _bgmSourceB = bgmParent.AddComponent<AudioSource>();

        ConfigureBGMSource(_bgmSourceA);
        ConfigureBGMSource(_bgmSourceB);

        _activeBgmSource = _bgmSourceA;
    }

    private void ConfigureBGMSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // BGM은 항상 2D
        source.priority = 0;     // 최고 우선순위

        if (audioMixer != null)
        {
            // Mixer를 사용할 경우 BGM 그룹에 연결
            var groups = audioMixer.FindMatchingGroups("BGM");
            if (groups.Length > 0)
                source.outputAudioMixerGroup = groups[0];
        }
    }

    private void InitializeSFXPool()
    {
        _sfxPoolParent = new GameObject("SFX_Pool").transform;
        _sfxPoolParent.SetParent(transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledAudioSource();
        }
    }

    private AudioSource CreatePooledAudioSource()
    {
        var go = new GameObject("SFX_Source");
        go.transform.SetParent(_sfxPoolParent);
        go.SetActive(false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        if (audioMixer != null)
        {
            var groups = audioMixer.FindMatchingGroups("SFX");
            if (groups.Length > 0)
                source.outputAudioMixerGroup = groups[0];
        }

        _sfxPool.Enqueue(source);
        return source;
    }

    #endregion

    #region ── BGM 재생 ───────────────────────────────────────────

    /// <summary>
    /// BGM을 재생합니다. 이미 같은 BGM이 재생 중이면 무시합니다.
    /// </summary>
    /// <param name="key">SoundClip에 등록된 키</param>
    /// <param name="fadeIn">페이드인 사용 여부 (기본: true)</param>
    public void PlayBGM(string key, bool fadeIn = true)
    {
        if (key == _currentBgmKey && IsBGMPlaying)
            return;

        if (!_bgmDict.TryGetValue(key, out var soundClip))
        {
            Debug.LogWarning($"[AudioManager] BGM 키를 찾을 수 없습니다: {key}");
            return;
        }

        if (_crossfadeCoroutine != null)
            StopCoroutine(_crossfadeCoroutine);

        if (IsBGMPlaying && fadeIn)
        {
            // 크로스페이드
            _crossfadeCoroutine = StartCoroutine(CrossfadeBGM(soundClip));
        }
        else
        {
            // 즉시 재생
            _activeBgmSource.clip = soundClip.clip;
            _activeBgmSource.volume = soundClip.volume * _bgmVolume * _masterVolume;
            _activeBgmSource.pitch = soundClip.pitch;
            _activeBgmSource.Play();
        }

        _currentBgmKey = key;
    }

    /// <summary>
    /// 현재 BGM을 정지합니다.
    /// </summary>
    /// <param name="fadeOut">페이드아웃 사용 여부 (기본: true)</param>
    public void StopBGM(bool fadeOut = true)
    {
        if (!IsBGMPlaying) return;

        if (_crossfadeCoroutine != null)
            StopCoroutine(_crossfadeCoroutine);

        if (fadeOut)
        {
            _crossfadeCoroutine = StartCoroutine(FadeOutBGM(_activeBgmSource, crossfadeDuration));
        }
        else
        {
            _activeBgmSource.Stop();
        }

        _currentBgmKey = null;
    }

    /// <summary>
    /// BGM 일시정지 / 재개
    /// </summary>
    public void PauseBGM()
    {
        if (IsBGMPlaying)
            _activeBgmSource.Pause();
    }

    public void ResumeBGM()
    {
        _activeBgmSource.UnPause();
    }

    private IEnumerator CrossfadeBGM(SoundClipSO newClip)
    {
        // 비활성 소스를 새 BGM으로 설정
        var oldSource = _activeBgmSource;
        var newSource = (_activeBgmSource == _bgmSourceA) ? _bgmSourceB : _bgmSourceA;

        newSource.clip = newClip.clip;
        newSource.pitch = newClip.pitch;
        newSource.volume = 0f;
        newSource.Play();

        float targetVolume = newClip.volume * _bgmVolume * _masterVolume;
        float elapsed = 0f;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / crossfadeDuration;
            float smoothT = t * t * (3f - 2f * t); // SmoothStep

            newSource.volume = Mathf.Lerp(0f, targetVolume, smoothT);
            oldSource.volume = Mathf.Lerp(oldSource.volume, 0f, smoothT);

            yield return null;
        }

        oldSource.Stop();
        oldSource.clip = null;
        newSource.volume = targetVolume;

        _activeBgmSource = newSource;
        _crossfadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = 0f;
        _crossfadeCoroutine = null;
    }

    #endregion

    #region ── SFX 재생 (2D) ──────────────────────────────────────

    /// <summary>
    /// 2D SFX를 재생합니다.
    /// </summary>
    /// <param name="key">SoundClip에 등록된 키</param>
    /// <param name="volumeScale">추가 볼륨 스케일 (기본: 1)</param>
    /// <param name="pitchVariation">피치 랜덤 변동폭 (기본: 0, 예: 0.1이면 ±0.1)</param>
    /// <returns>재생 중인 AudioSource (null이면 재생 실패)</returns>
    public AudioSource PlaySFX(string key, float volumeScale = 1f, float pitchVariation = 0f)
    {
        if (!_sfxDict.TryGetValue(key, out var soundClip))
        {
            Debug.LogWarning($"[AudioManager] SFX 키를 찾을 수 없습니다: {key}");
            return null;
        }

        // 동일 SFX 동시 재생 제한 확인
        if (!CanPlaySFX(key))
            return null;

        var source = GetPooledSource();
        if (source == null) return null;

        source.spatialBlend = 0f; // 2D
        source.transform.localPosition = Vector3.zero;

        ConfigureAndPlaySFX(source, soundClip, volumeScale, pitchVariation, key);
        return source;
    }

    #endregion

    #region ── SFX 재생 (3D 공간 오디오) ──────────────────────────

    /// <summary>
    /// 3D 공간 오디오로 SFX를 재생합니다.
    /// </summary>
    /// <param name="key">SoundClip에 등록된 키</param>
    /// <param name="position">월드 좌표 위치</param>
    /// <param name="volumeScale">추가 볼륨 스케일</param>
    /// <param name="pitchVariation">피치 랜덤 변동폭</param>
    /// <param name="minDistance">3D 최소 거리 (null이면 기본값 사용)</param>
    /// <param name="maxDistance">3D 최대 거리 (null이면 기본값 사용)</param>
    /// <returns>재생 중인 AudioSource (null이면 재생 실패)</returns>
    public AudioSource PlaySFX3D(string key, Vector3 position,
        float volumeScale = 1f, float pitchVariation = 0f,
        float? minDistance = null, float? maxDistance = null)
    {
        if (!_sfxDict.TryGetValue(key, out var soundClip))
        {
            Debug.LogWarning($"[AudioManager] SFX 키를 찾을 수 없습니다: {key}");
            return null;
        }

        if (!CanPlaySFX(key))
            return null;

        var source = GetPooledSource();
        if (source == null) return null;

        // 3D 설정
        source.spatialBlend = 1f; // 완전 3D
        source.transform.position = position;
        source.minDistance = minDistance ?? soundClip.GetMinDistance() ?? default3DMinDistance;
        source.maxDistance = maxDistance ?? soundClip.GetMaxDistance() ?? default3DMaxDistance;
        source.rolloffMode = defaultRolloff;
        source.dopplerLevel = 0.5f;
        source.spread = 0f;

        ConfigureAndPlaySFX(source, soundClip, volumeScale, pitchVariation, key);
        return source;
    }

    /// <summary>
    /// 특정 Transform을 따라다니며 3D SFX를 재생합니다.
    /// </summary>
    public AudioSource PlaySFX3DAttached(string key, Transform followTarget,
        float volumeScale = 1f, float pitchVariation = 0f)
    {
        var source = PlaySFX3D(key, followTarget.position, volumeScale, pitchVariation);
        if (source != null)
        {
            // AudioSource를 대상 Transform의 자식으로 이동
            source.transform.SetParent(followTarget);
            source.transform.localPosition = Vector3.zero;
        }
        return source;
    }

    #endregion

    #region ── SFX 풀링 내부 로직 ─────────────────────────────────

    private AudioSource GetPooledSource()
    {
        AudioSource source = null;

        // 풀에서 꺼내기
        while (_sfxPool.Count > 0)
        {
            source = _sfxPool.Dequeue();
            if (source != null) break;
        }

        // 풀이 비었으면 새로 생성 (최대 한도 체크)
        if (source == null)
        {
            int totalCount = _sfxPool.Count + _activeSfxSources.Count;
            if (totalCount >= maxPoolSize)
            {
                // 가장 오래된 활성 소스를 강제 반환
                if (_activeSfxSources.Count > 0)
                {
                    source = _activeSfxSources[0];
                    DecrementPlayCount(source);
                    _activeSfxSources.RemoveAt(0);
                    source.Stop();
                }
                else
                {
                    Debug.LogWarning("[AudioManager] SFX 풀 한도 초과!");
                    return null;
                }
            }
            else
            {
                source = CreatePooledAudioSource();
                _sfxPool.Dequeue(); // 방금 넣은 것을 다시 꺼냄
            }
        }

        source.gameObject.SetActive(true);
        source.transform.SetParent(_sfxPoolParent); // 기본 부모 복원
        _activeSfxSources.Add(source);
        return source;
    }

    private void ConfigureAndPlaySFX(AudioSource source, SoundClipSO clip,
        float volumeScale, float pitchVariation, string key)
    {
        source.clip = clip.clip;
        source.volume = clip.volume * _sfxVolume * _masterVolume * volumeScale;
        source.pitch = clip.pitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
        source.loop = false;
        source.priority = clip.priority;

        // 재생 카운트 기록
        source.gameObject.name = $"SFX_{key}";
        IncrementPlayCount(key);

        source.Play();
    }

    private void ReturnFinishedSFXToPool()
    {
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            var source = _activeSfxSources[i];
            if (source == null)
            {
                _activeSfxSources.RemoveAt(i);
                continue;
            }

            if (!source.isPlaying)
            {
                DecrementPlayCount(source);
                ReturnToPool(source);
                _activeSfxSources.RemoveAt(i);
            }
        }
    }

    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.transform.SetParent(_sfxPoolParent);
        source.transform.localPosition = Vector3.zero;
        source.spatialBlend = 0f;
        source.gameObject.SetActive(false);
        source.gameObject.name = "SFX_Source";
        _sfxPool.Enqueue(source);
    }

    /// <summary>
    /// 특정 키의 모든 SFX를 즉시 정지합니다.
    /// </summary>
    public void StopSFX(string key)
    {
        string targetName = $"SFX_{key}";
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (_activeSfxSources[i].gameObject.name == targetName)
            {
                DecrementPlayCount(_activeSfxSources[i]);
                ReturnToPool(_activeSfxSources[i]);
                _activeSfxSources.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 모든 SFX를 즉시 정지합니다.
    /// </summary>
    public void StopAllSFX()
    {
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (_activeSfxSources[i] != null)
            {
                ReturnToPool(_activeSfxSources[i]);
            }
            _activeSfxSources.RemoveAt(i);
        }
        _sfxPlayCount.Clear();
    }

    #endregion

    #region ── 동일 SFX 동시 재생 제한 ────────────────────────────

    private bool CanPlaySFX(string key)
    {
        if (_sfxPlayCount.TryGetValue(key, out int count))
        {
            return count < maxConcurrentSameSFX;
        }
        return true;
    }

    private void IncrementPlayCount(string key)
    {
        if (!_sfxPlayCount.ContainsKey(key))
            _sfxPlayCount[key] = 0;
        _sfxPlayCount[key]++;
    }

    private void DecrementPlayCount(AudioSource source)
    {
        string name = source.gameObject.name;
        if (name.StartsWith("SFX_"))
        {
            string key = name.Substring(4);
            if (_sfxPlayCount.ContainsKey(key))
            {
                _sfxPlayCount[key] = Mathf.Max(0, _sfxPlayCount[key] - 1);
                if (_sfxPlayCount[key] == 0)
                    _sfxPlayCount.Remove(key);
            }
        }
    }

    #endregion

    #region ── 볼륨 제어 & 설정 저장 ──────────────────────────────

    /// <summary>
    /// 마스터 볼륨을 설정합니다. (0~1)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeChanges();
        SaveVolumeSettings();
    }

    /// <summary>
    /// BGM 볼륨을 설정합니다. (0~1)
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumeChanges();
        SaveVolumeSettings();
    }

    /// <summary>
    /// SFX 볼륨을 설정합니다. (0~1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeChanges();
        SaveVolumeSettings();
    }

    /// <summary>
    /// 전체 음소거 토글
    /// </summary>
    public void ToggleMute()
    {
        _isMuted = !_isMuted;
        AudioListener.volume = _isMuted ? 0f : 1f;
        PlayerPrefs.SetInt(PREF_MUTED, _isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 음소거 설정
    /// </summary>
    public void SetMute(bool muted)
    {
        _isMuted = muted;
        AudioListener.volume = _isMuted ? 0f : 1f;
        PlayerPrefs.SetInt(PREF_MUTED, _isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyVolumeChanges()
    {
        // BGM 볼륨 즉시 반영
        if (_activeBgmSource != null && _activeBgmSource.isPlaying && _activeBgmSource.clip != null)
        {
            string bgmKey = _currentBgmKey;
            if (bgmKey != null && _bgmDict.TryGetValue(bgmKey, out var bgmClip))
            {
                _activeBgmSource.volume = bgmClip.volume * _bgmVolume * _masterVolume;
            }
        }

        // AudioMixer 볼륨 반영 (사용 시)
        if (audioMixer != null)
        {
            // dB 변환: 0은 -80dB (무음), 1은 0dB
            audioMixer.SetFloat(masterVolumeParam, LinearToDecibel(_masterVolume));
            audioMixer.SetFloat(bgmVolumeParam, LinearToDecibel(_bgmVolume));
            audioMixer.SetFloat(sfxVolumeParam, LinearToDecibel(_sfxVolume));
        }
    }

    private float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, _masterVolume);
        PlayerPrefs.SetFloat(PREF_BGM_VOL, _bgmVolume);
        PlayerPrefs.SetFloat(PREF_SFX_VOL, _sfxVolume);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1f);
        _bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 1f);
        _isMuted = PlayerPrefs.GetInt(PREF_MUTED, 0) == 1;

        AudioListener.volume = _isMuted ? 0f : 1f;
        ApplyVolumeChanges();
    }

    #endregion

    #region ── 유틸리티 ───────────────────────────────────────────

    /// <summary>
    /// 랜덤 SFX 재생 (발소리, 타격음 등 변주에 유용)
    /// </summary>
    /// <param name="keys">재생할 SFX 키 배열</param>
    public AudioSource PlayRandomSFX(string[] keys, float volumeScale = 1f, float pitchVariation = 0.05f)
    {
        if (keys == null || keys.Length == 0) return null;
        string key = keys[UnityEngine.Random.Range(0, keys.Length)];
        return PlaySFX(key, volumeScale, pitchVariation);
    }

    /// <summary>
    /// 지연 후 SFX 재생
    /// </summary>
    public void PlaySFXDelayed(string key, float delay, float volumeScale = 1f)
    {
        StartCoroutine(PlaySFXDelayedCoroutine(key, delay, volumeScale));
    }

    private IEnumerator PlaySFXDelayedCoroutine(string key, float delay, float volumeScale)
    {
        yield return new WaitForSeconds(delay);
        PlaySFX(key, volumeScale);
    }

    /// <summary>
    /// 현재 재생 중인 SFX 개수
    /// </summary>
    public int GetActiveSFXCount() => _activeSfxSources.Count;

    /// <summary>
    /// 특정 SFX가 현재 재생 중인지 확인
    /// </summary>
    public bool IsSFXPlaying(string key)
    {
        return _sfxPlayCount.ContainsKey(key) && _sfxPlayCount[key] > 0;
    }

    /// <summary>
    /// SFX 클립 데이터를 가져옵니다. (AmbientZone, AmbientSoundPoint 등에서 사용)
    /// </summary>
    public SoundClipSO GetSFXClip(string key)
    {
        _sfxDict.TryGetValue(key, out var clip);
        return clip;
    }

    /// <summary>
    /// BGM 클립 데이터를 가져옵니다.
    /// </summary>
    public SoundClipSO GetBGMClip(string key)
    {
        _bgmDict.TryGetValue(key, out var clip);
        return clip;
    }

    #endregion
}