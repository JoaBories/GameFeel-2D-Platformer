using UnityEngine;
using UnityEngine.InputSystem;

public class Movements : MonoBehaviour
{
    private Movements instance;

    private Controls _inputActions;
    private InputAction _moveAction;
    private InputAction _jumpAction;


    [Header("Move Values")]
    [SerializeField] private float speed;
    [SerializeField] private float acceleration;
    [SerializeField] private float decceleration;
    [SerializeField] private float frictionAmount;

    [Header("Jump Value")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float gravity;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float jumpInputBufferTime;

    [Header("Check Assign")]
    [SerializeField] private Vector2 checkSize;
    [SerializeField] private Vector2 checkOffset;
    [SerializeField] private LayerMask groundLayer;

    private float moveDir;

    private Rigidbody2D _rb;

    private float lastGroundTime;
    private float lastJumpTime;
    private float lastJumpInputTime;

    private bool isJumping;

    private void Awake()
    {
        instance = this;
        _inputActions = new();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _moveAction = _inputActions.Movements.Move;
        _moveAction.Enable();

        _jumpAction = _inputActions.Movements.Jump;
        _jumpAction.Enable();
        _jumpAction.performed += Jump;
    }

    private void OnDisable()
    {
        _moveAction.Disable();

        _jumpAction.Disable();
        _jumpAction.performed -= Jump;
    }

    private void Update()
    {
        moveDir = (_moveAction.ReadValue<Vector2>().x == 0)? 0 : Mathf.Sign(_moveAction.ReadValue<Vector2>().x);
    }

    private void FixedUpdate()
    {
        if (Physics2D.OverlapBox(transform.position + (Vector3)checkOffset, checkSize, groundLayer) && lastJumpTime < -1f)
        {
            if (isJumping) Debug.Log("yo");
            lastGroundTime = 0;
            isJumping = false;
            if (lastJumpInputTime > 0)
            {
                Jump(new());
            }
        } else
        {
            lastGroundTime -= Time.fixedDeltaTime;
        }

        lastJumpTime -= Time.fixedDeltaTime;
        lastJumpInputTime -= Time.fixedDeltaTime;

        float targetSpeed = moveDir * speed;
        float speedDif = targetSpeed - _rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration;
        float movement = Mathf.Abs(speedDif) * accelRate * Mathf.Sign(speedDif);
        _rb.AddForce(movement * Vector2.right);

        if (moveDir == 0 && lastGroundTime >= 0)
        {
            float amount = Mathf.Min(Mathf.Abs(_rb.velocity.x), Mathf.Abs(frictionAmount)) * Mathf.Sign(_rb.velocity.x);
            _rb.AddForce(-amount * Vector2.right, ForceMode2D.Impulse);
        }
    }


    private void Jump(InputAction.CallbackContext context)
    {
        if (lastGroundTime >= 0 && !isJumping)
        {
            isJumping = true;
            _rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
            lastJumpTime = 0;
            lastJumpInputTime = 0;
        } 
        else
        {
            lastJumpInputTime = jumpInputBufferTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)checkOffset, checkSize);
    }
}

