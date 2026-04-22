using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

namespace Njinx.UI
{
public class SliderUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // ──────────────────────────────────────────────
    //  Inspector 설정
    // ──────────────────────────────────────────────

    [Header("=== UI 요소 연결 ===")]
    [Tooltip("슬라이더 전체 영역 (클릭/드래그 판정용 RectTransform)")]
    [SerializeField] private RectTransform sliderRect;

    [Tooltip("채워지는 바 이미지")]
    [SerializeField] private Image fillImage;

    [Tooltip("드래그 핸들 RectTransform")]
    [SerializeField] private RectTransform handleRect;

    [Header("=== 값 설정 ===")]
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 1f;
    [SerializeField] private float defaultValue = 0.5f;

    [Tooltip("true면 정수 단위로만 변경됩니다")]
    [SerializeField] private bool wholeNumbers = false;

    [Tooltip("스냅 간격 (0이면 비활성). 예: 0.1이면 0, 0.1, 0.2 ... 단위로 스냅")]
    [SerializeField] private float snapStep = 0f;

    [Header("=== 핸들 커스텀 ===")]
    [Tooltip("핸들 기본 크기")]
    [SerializeField] private float handleDefaultScale = 1f;

    [Tooltip("드래그 중 핸들 크기 (확대 효과)")]
    [SerializeField] private float handleDragScale = 1.1f;

    [Tooltip("핸들 기본 색상")]
    [SerializeField] private Color handleDefaultColor = Color.white;

    [Tooltip("핸들 드래그 중 색상")]
    [SerializeField] private Color handleDragColor = new Color(0.8f, 0.9f, 1f, 1f);

    [Tooltip("핸들 비활성 색상")]
    [SerializeField] private Color handleDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("핸들 크기/색상 전환 속도")]
    [SerializeField] private float handleTransitionSpeed = 10f;

    [Tooltip("핸들에 사용할 커스텀 스프라이트 (비워두면 기본 이미지 사용)")]
    [SerializeField] private Sprite handleSprite;

    public UnityEvent<float> onValueChanged { get; private set; } = new UnityEvent<float>();
    public UnityEvent onDragStart { get; private set; } = new UnityEvent();
    public UnityEvent onDragEnd { get; private set; } = new UnityEvent();

    // ──────────────────────────────────────────────
    //  프로퍼티
    // ──────────────────────────────────────────────

    private float _currentValue;

    /// <summary>현재 슬라이더 값 (minValue ~ maxValue)</summary>
    public float Value
    {
        get => _currentValue;
        set => SetValue(value, sendCallback: true);
    }

    /// <summary>0 ~ 1 사이의 정규화된 값</summary>
    public float NormalizedValue
    {
        get => Mathf.InverseLerp(minValue, maxValue, _currentValue);
        set => Value = Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(value));
    }

    public bool Interactable
    {
        get => _interactable;
        set
        {
            _interactable = value;
            UpdateHandleVisual();
        }
    }

    // ──────────────────────────────────────────────
    //  내부 변수
    // ──────────────────────────────────────────────

    private bool _interactable = true;
    private bool _isDragging = false;
    private Image _handleImage;
    private Camera _uiCamera;

    // 핸들 애니메이션 목표값
    private float _targetHandleSize;
    private Color _targetHandleColor;

    // ──────────────────────────────────────────────
    //  Unity 생명주기
    // ──────────────────────────────────────────────

    private void Awake()
    {
        // 핸들 이미지 컴포넌트 캐싱
        if (handleRect != null)
        {
            _handleImage = handleRect.GetComponent<Image>();

            if (handleSprite != null && _handleImage != null)
                _handleImage.sprite = handleSprite;
        }

        // Canvas의 Render Mode에 따라 카메라 설정
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _uiCamera = canvas.worldCamera;

        // 초기값 설정
        _targetHandleSize = handleDefaultScale;
        _targetHandleColor = handleDefaultColor;

        SetValue(defaultValue, sendCallback: false);
    }

    private void Update()
    {
        AnimateHandle();
    }

    // ──────────────────────────────────────────────
    //  이벤트 핸들러 (IPointerDown / IDrag / IPointerUp)
    // ──────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_interactable) return;

        _isDragging = true;

        // 드래그 시작 시 핸들 확대 + 색상 변경
        _targetHandleSize = handleDragScale;
        _targetHandleColor = handleDragColor;

        UpdateValueFromPointer(eventData);
        onDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_interactable || !_isDragging) return;

        UpdateValueFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;

        // 드래그 종료 시 핸들 원래 크기/색상으로 복귀
        _targetHandleSize = handleDefaultScale;
        _targetHandleColor = _interactable ? handleDefaultColor : handleDisabledColor;

        onDragEnd?.Invoke();
    }

    // ──────────────────────────────────────────────
    //  핵심 로직
    // ──────────────────────────────────────────────

    /// <summary>포인터 위치로부터 슬라이더 값 계산</summary>
    private void UpdateValueFromPointer(PointerEventData eventData)
    {
        if (sliderRect == null) return;

        // 포인터 위치를 슬라이더 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderRect, eventData.position, _uiCamera, out Vector2 localPoint);

        float sliderWidth = sliderRect.rect.width;
        if (sliderWidth <= 0f) return;

        // 로컬 좌표 → 0~1 비율 (피벗 고려)
        float normalizedPos = Mathf.Clamp01(
            (localPoint.x - sliderRect.rect.xMin) / sliderWidth);

        float newValue = Mathf.Lerp(minValue, maxValue, normalizedPos);
        SetValue(newValue, sendCallback: true);
    }

    /// <summary>값 설정 및 비주얼 업데이트</summary>
    private void SetValue(float newValue, bool sendCallback)
    {
        // 클램프
        newValue = Mathf.Clamp(newValue, minValue, maxValue);

        // 정수 모드
        if (wholeNumbers)
            newValue = Mathf.Round(newValue);

        // 스냅 처리
        if (snapStep > 0f && !wholeNumbers)
        {
            newValue = Mathf.Round(newValue / snapStep) * snapStep;
            newValue = Mathf.Clamp(newValue, minValue, maxValue);
        }

        // 값이 변경되지 않았으면 스킵
        if (Mathf.Approximately(_currentValue, newValue)) return;

        _currentValue = newValue;

        UpdateVisuals();

        if (sendCallback)
            onValueChanged?.Invoke(_currentValue);
    }

    /// <summary>Fill 바 + 핸들 위치 업데이트</summary>
    private void UpdateVisuals()
    {
        float normalized = NormalizedValue;

        // Fill 이미지 (Filled 타입 사용 시)
        if (fillImage != null)
            fillImage.fillAmount = normalized;

        // 핸들 위치
        if (handleRect != null && sliderRect != null)
        {
            float xMin = sliderRect.rect.xMin;
            float xMax = sliderRect.rect.xMax;
            float xPos = Mathf.Lerp(xMin, xMax, normalized);

            handleRect.anchoredPosition = new Vector2(
                xPos, handleRect.anchoredPosition.y);
        }
    }

    /// <summary>핸들 크기/색상 부드러운 전환</summary>
    private void AnimateHandle()
    {
        if (handleRect == null) return;

        float t = handleTransitionSpeed * Time.unscaledDeltaTime;

        // 스케일 보간
        float current = handleRect.localScale.x;
        float next = Mathf.Lerp(current, _targetHandleSize, t);
        handleRect.localScale = Vector3.one * next;

        // 색상 보간
        if (_handleImage != null)
            _handleImage.color = Color.Lerp(_handleImage.color, _targetHandleColor, t);
    }

    private void UpdateHandleVisual()
    {
        if (!_interactable)
        {
            _targetHandleColor = handleDisabledColor;
            _targetHandleSize = handleDefaultScale;
        }
        else
        {
            _targetHandleColor = _isDragging ? handleDragColor : handleDefaultColor;
            _targetHandleSize = _isDragging ? handleDragScale : handleDefaultScale;
        }
    }

    // ──────────────────────────────────────────────
    //  공개 유틸리티 메서드
    // ──────────────────────────────────────────────

    /// <summary>슬라이더를 기본값으로 초기화</summary>
    public void ResetToDefault()
    {
        SetValue(defaultValue, sendCallback: true);
    }

    /// <summary>콜백 없이 값만 설정 (초기 로드 시 유용)</summary>
    public void SetValueWithoutNotify(float newValue)
    {
        SetValue(newValue, sendCallback: false);
    }

    /// <summary>런타임에 핸들 스프라이트 교체</summary>
    public void SetHandleSprite(Sprite sprite)
    {
        handleSprite = sprite;
        if (_handleImage != null)
            _handleImage.sprite = sprite;
    }

    /// <summary>런타임에 핸들 색상 테마 변경</summary>
    public void SetHandleColors(Color defaultCol, Color dragCol, Color disabledCol)
    {
        handleDefaultColor = defaultCol;
        handleDragColor = dragCol;
        handleDisabledColor = disabledCol;
        UpdateHandleVisual();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 값 변경 시 미리보기
        if (minValue > maxValue) minValue = maxValue;
        defaultValue = Mathf.Clamp(defaultValue, minValue, maxValue);

        if (Application.isPlaying)
            SetValue(_currentValue, sendCallback: false);
    }
#endif
}
}
