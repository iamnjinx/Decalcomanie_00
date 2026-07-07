using System;
using UnityEngine;

namespace TarodevController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerControllerT : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        [SerializeField] private bool _debugCornerCorrection = true;
        private Rigidbody2D _rb;
        private Collider2D _col;                  // ← Collider2D로 변경
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        // 콜라이더 타입 캐싱
        private CapsuleCollider2D _capsuleCol;
        private BoxCollider2D _boxCol;

        private float _cornerPushDir; // 0 = 코너 보정 없음, ±1 = 이번 프레임에 밀어야 할 방향

        #region Interface
        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        #endregion

        public bool InputEnabled { get; set; } = true;

        public void ForceStop()
        {
            _frameVelocity = Vector2.zero;
            _rb.velocity = Vector2.zero;
            _frameInput.Move = Vector2.zero;
            _jumpToConsume = false;
            _endedJumpEarly = false;
        }

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();    // ← Collider2D로 변경

            // 어떤 콜라이더인지 캐싱
            _capsuleCol = _col as CapsuleCollider2D;
            _boxCol = _col as BoxCollider2D;

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            if (!InputEnabled)
            {
                _frameInput = new FrameInput();
                return;
            }

            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.W),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.W),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            HandleJump();
            HandleDirection();
            HandleGravity();
            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        /// <summary>
        /// 콜라이더 타입에 맞는 Cast를 수행합니다.
        /// CapsuleCollider2D → CapsuleCast
        /// BoxCollider2D     → BoxCast
        /// 그 외             → CircleCast (fallback)
        /// </summary>
        private RaycastHit2D ColliderCast(Vector2 direction, float distance, int layerMask)
            => ColliderCastFrom(_col.bounds.center, direction, distance, layerMask);

        /// <summary>
        /// 콜라이더 타입에 맞는 Cast를 지정한 origin에서 수행합니다.
        /// CapsuleCollider2D → CapsuleCast
        /// BoxCollider2D     → BoxCast
        /// 그 외             → CircleCast (fallback)
        /// </summary>
        private RaycastHit2D ColliderCastFrom(Vector2 origin, Vector2 direction, float distance, int layerMask)
        {
            if (_capsuleCol != null)
            {
                return Physics2D.CapsuleCast(
                    origin, _capsuleCol.size, _capsuleCol.direction,
                    0, direction, distance, layerMask);
            }
            else if (_boxCol != null)
            {
                return Physics2D.BoxCast(
                    origin, _boxCol.size, 0,
                    direction, distance, layerMask);
            }
            else
            {
                // CircleCollider2D 등 나머지 fallback
                float radius = _col.bounds.extents.magnitude * 0.5f;
                return Physics2D.CircleCast(
                    origin, radius,
                    direction, distance, layerMask);
            }
        }

        /// <summary>
        /// 천장 충돌이 캐릭터 상단 가장자리(모서리)에만 걸친 경우, 옆으로 밀어내어 통과시킵니다.
        /// hit.point 좌표를 해석하는 대신, "캐릭터를 tolerance만큼 옆으로 옮겼다고 가정하면 실제로 천장에서
        /// 벗어나는가?"를 좌/우 각각 직접 캐스트로 검증합니다. 한쪽만 벗어나면 코너로 판단해 그쪽으로 밀어냅니다.
        /// </summary>
        private bool TryCornerCorrection(int mask)
        {
            var bounds = _col.bounds;
            Vector2 center = bounds.center;
            float tolerance = _stats.CornerCorrectionTolerance;
            float dist = _stats.GrounderDistance;

            bool stillBlockedIfRight = ColliderCastFrom(center + Vector2.right * tolerance, Vector2.up, dist, mask);
            bool stillBlockedIfLeft  = ColliderCastFrom(center + Vector2.left  * tolerance, Vector2.up, dist, mask);

            if (_debugCornerCorrection)
                Debug.Log($"[CornerCorrection] stillBlockedIfRight={stillBlockedIfRight}, stillBlockedIfLeft={stillBlockedIfLeft}, tolerance={tolerance:F3}");

            if (stillBlockedIfRight == stillBlockedIfLeft)
            {
                if (_debugCornerCorrection) Debug.Log("[CornerCorrection] 취소: 양쪽 다 tolerance로 안 뚫리거나(진짜 천장/벽), 양쪽 다 뚫림(판단 불가)");
                return false;
            }

            float pushDir = stillBlockedIfRight ? -1f : 1f; // 오른쪽으로 밀어도 여전히 막히면 왼쪽이 뚫린 것
            float frameDistance = _stats.CornerCorrectionSpeed * Time.fixedDeltaTime; // 이번 프레임에 이동할 거리만 확인

            if (ColliderCast(Vector2.right * pushDir, frameDistance, mask))
            {
                if (_debugCornerCorrection) Debug.Log($"[CornerCorrection] 취소: 미는 방향(pushDir={pushDir})에 벽이 있음");
                return false; // 미는 방향에 벽이 있으면 취소
            }

            // 순간이동 대신 velocity로 밀어서 여러 프레임에 걸쳐 자연스럽게 슬라이드되게 합니다.
            // 코너를 완전히 벗어날 때까지(ceilingHit이 false가 될 때까지) 매 프레임 재판단되어 계속 적용됩니다.
            _cornerPushDir = pushDir;

            if (_debugCornerCorrection) Debug.Log($"[CornerCorrection] 적용됨: pushDir={pushDir}");
            return true;
        }

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;
            Physics2D.queriesHitTriggers = false;

            int mask = ~_stats.PlayerLayer;
            bool groundHit  = ColliderCast(Vector2.down, _stats.GrounderDistance, mask);
            bool ceilingHit = ColliderCast(Vector2.up,   _stats.GrounderDistance, mask);

            if (ceilingHit)
            {
                if (_debugCornerCorrection) Debug.Log("[CornerCorrection] 천장 충돌 감지됨 (ceilingHit=true), 코너 보정 시도");
                if (TryCornerCorrection(mask))
                    _frameVelocity.y = Mathf.Max(_frameVelocity.y, _stats.CornerCorrectionUpwardBoost); // 정점 부근이라도 위로 살짝 밀어줌
                else
                    _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
            }

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesHitTriggers = true;
            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed = float.MinValue;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote    => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;
            if (_grounded || CanUseCoyote) ExecuteJump();
            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_cornerPushDir != 0f)
            {
                // 코너 보정 중에는 입력을 무시하고 보정 방향으로만 밀어냅니다.
                _frameVelocity.x = _cornerPushDir * _stats.CornerCorrectionSpeed;
                _cornerPushDir = 0f; // 다음 프레임에 다시 판단
                return;
            }

            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0)
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.velocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}