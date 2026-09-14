using UnityEngine;

/// <summary>
/// 씬/메타 단축키의 키 매핑을 한곳에 모읍니다.
/// 각 매니저는 KeyCode를 직접 알 필요 없이 의미 단위 프로퍼티만 폴링합니다.
///
/// 여기서 다루지 않는 입력:
/// - UI 클릭/드래그: EventSystem이 라우팅합니다 (IPointerDownHandler 등).
/// - 플랫포머 이동/점프: PlayerControllerT.GatherInput()이 자체적으로 모읍니다.
/// </summary>
public static class GameInput
{
    #region 키 정의

    // Pause와 Back은 현재 같은 ESC를 공유하지만 게임상 다른 행동입니다.
    // (Pause = 설정 열기/닫기, Back = 이전 화면으로) 나중에 갈라질 수 있어 따로 둡니다.
    public static KeyCode Pause = KeyCode.Escape;
    public static KeyCode Back = KeyCode.Escape;

    public static KeyCode Proceed = KeyCode.Space;
    public static KeyCode Switch = KeyCode.Tab;
    public static KeyCode Undo = KeyCode.Z;
    public static KeyCode Reset = KeyCode.R;
    public static KeyCode FlipHorizontal = KeyCode.Alpha1;
    public static KeyCode FlipVertical = KeyCode.Alpha2;

    public static KeyCode NavigateLeft = KeyCode.LeftArrow;
    public static KeyCode NavigateLeftAlt = KeyCode.A;
    public static KeyCode NavigateRight = KeyCode.RightArrow;
    public static KeyCode NavigateRightAlt = KeyCode.D;

    private const int ConfirmMouseButton = 0;
    private const int UndoMouseButton = 1;

    #endregion

    #region 잠금

    /// <summary>
    /// true인 동안 아래 조회는 전부 false를 반환하고, 플레이어 이동/점프도 멈춥니다.
    /// 씬 전환처럼 화면이 넘어가는 중에 조작이 먹는 것을 막는 용도입니다.
    /// 켠 쪽이 반드시 다시 꺼야 하므로 try/finally로 감싸서 쓰세요.
    /// </summary>
    public static bool IsLocked { get; set; }

    // 잠금 검사를 한곳에서만 하도록 모든 조회가 이 세 함수를 거칩니다.
    private static bool KeyDown(KeyCode key) => !IsLocked && Input.GetKeyDown(key);
    private static bool KeyHeld(KeyCode key) => !IsLocked && Input.GetKey(key);
    private static bool MouseDown(int button) => !IsLocked && Input.GetMouseButtonDown(button);

    #endregion

    #region 조회

    public static bool PausePressed => KeyDown(Pause);
    public static bool BackPressed => KeyDown(Back);

    /// <summary>다음으로 넘기기(스테이지 진행 등). 누르는 순간.</summary>
    public static bool ProceedPressed => KeyDown(Proceed);

    /// <summary>빨리감기·홀드 스킵처럼 누르고 있는 동안 유효한 진행.</summary>
    public static bool ProceedHeld => KeyHeld(Proceed);

    public static bool SwitchPressed => KeyDown(Switch);
    public static bool ResetPressed => KeyDown(Reset);
    public static bool FlipHorizontalPressed => KeyDown(FlipHorizontal);
    public static bool FlipVerticalPressed => KeyDown(FlipVertical);

    public static bool UndoPressed => MouseDown(UndoMouseButton) || KeyDown(Undo);

    /// <summary>마우스 좌클릭. 크레딧 넘기기 등 "아무거나 눌러 진행"에 씁니다.</summary>
    public static bool ConfirmPressed => MouseDown(ConfirmMouseButton);

    public static bool NavigateLeftPressed => KeyDown(NavigateLeft) || KeyDown(NavigateLeftAlt);
    public static bool NavigateRightPressed => KeyDown(NavigateRight) || KeyDown(NavigateRightAlt);

    #endregion
}
