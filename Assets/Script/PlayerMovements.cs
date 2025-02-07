using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PlayerMovements : MonoBehaviour
{
    public static PlayerMovements instance;

    private Controls _inputActions;

    private InputAction _moveAction;
    public static float moveDir;

    private InputAction _jumpAction;


    [Header("Movements Values")]
    [SerializeField] private float acceleration;
    [SerializeField] private float speed;
    [SerializeField] private float decceleration;
    [SerializeField] private float frictionAmount;
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float gravityScale;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float jumpCutMultiplier;
    [SerializeField] private float jumpInputBufferTime;

    [Header("Checks")]
    [SerializeField] private Transform checkGroundPoint;
    [SerializeField] private Vector2 checkGroundSize;
    [SerializeField] private Vector2 checkGroundOffset;
    [SerializeField] private LayerMask groundLayers;

    [Header("FeedBacks")]
    [SerializeField] private Vector3 maxStretch;
    [SerializeField] private float velForMaxStretch;
    [SerializeField] private GameObject playerDisplay;
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private GameObject eyes;
    [SerializeField] private float eyeSpeed;

    private Rigidbody2D _rb;

    private float lastGroundTime;
    private float lastJumpTime;
    private float jumpInputBuffer;

    private bool isJumping;
    private bool isFalling;

    private Vector3 baseScale;

    private GameObject visualEffectObject;
    private GameObject visualEffectObjectBis;
    private float vfxTime;
    private float vfxTimeBis;

    private float goalEyesX;

    private void Awake()
    {
        instance = this;
        _rb = GetComponent<Rigidbody2D>();

        _inputActions = new Controls();
        baseScale = playerDisplay.transform.localScale;

        goalEyesX = 0.25f;
    }

    private void OnEnable()
    {
        _moveAction = _inputActions.Gameplay.Move;
        _moveAction.Enable();

        _jumpAction = _inputActions.Gameplay.Jump;
        _jumpAction.Enable();
        _jumpAction.performed += Jump;
        _jumpAction.canceled += JumpCancel;
    }

    private void OnDisable()
    {
        _moveAction.Disable();
    }

    private void Update()
    {
        moveDir = _moveAction.ReadValue<Vector2>().x;

        if (isJumping)
        {
            float velY = Mathf.Abs(_rb.velocity.y);
            if (velY <= velForMaxStretch)
            {
                if (velY >= 0.1f)
                {
                    velY *= 1 / velForMaxStretch;
                    Vector3 scaleDiff = baseScale - maxStretch;
                    float scaleY = baseScale.y - scaleDiff.y * velY;
                    float scaleX = baseScale.x - scaleDiff.x * velY;
                    playerDisplay.transform.localScale = new Vector3(scaleX, scaleY, baseScale.z);
                }
                else
                {
                    playerDisplay.transform.localScale = baseScale;
                }
            }
            else
            {
                playerDisplay.transform.localScale = maxStretch;
            }
        } else
        {
            playerDisplay.transform.localScale = baseScale;
        }

        if (Mathf.Abs(eyes.transform.localPosition.x - goalEyesX) >= 0.05f)
        {
            float a = goalEyesX - eyes.transform.localPosition.x;
            eyes.transform.localPosition = new Vector3(eyes.transform.localPosition.x + eyeSpeed * Mathf.Sign(a) * Time.deltaTime, 0.2f, 0);
        }
        else
        {
            eyes.transform.localPosition = new Vector3(goalEyesX, 0.2f, 0);
        }
    }

    private void FixedUpdate()
    {
        lastGroundTime -= Time.fixedDeltaTime;
        lastJumpTime -= Time.fixedDeltaTime;
        jumpInputBuffer -= Time.fixedDeltaTime;
        vfxTime -= Time.fixedDeltaTime;
        vfxTimeBis -= Time.fixedDeltaTime;

        if (CheckGround())
        {
            lastGroundTime = 0;
            if (isJumping && lastJumpTime < -0.1f)
            {
                isJumping = false;
                PlayVFX();

                if (jumpInputBuffer >= 0)
                {
                    Jump(new InputAction.CallbackContext());
                    jumpInputBuffer = 0;
                }
            } 
            else if (isFalling)
            {
                isFalling = false;
                PlayVFX();
            }
        }
        else
        {
            if (!isJumping)
            {
                isFalling = true;
            }
        }

        if (_rb.velocity.x > 1f)
        {
            goalEyesX = 0.25f;
        } 
        else if (_rb.velocity.x < -1f)
        {
            goalEyesX = -0.25f;
        }

        if (visualEffectObject != null)
        {
            if (vfxTime <= -0.15f)
            {
                visualEffectObject.GetComponent<VisualEffect>().SetFloat("Lifetime", 0);
            }

            if (vfxTime <= -0.65f)
            {
                Destroy(visualEffectObject);
                visualEffectObject = null;
            }
        }

        if (visualEffectObjectBis != null)
        {
            if (vfxTimeBis <= -0.15f)
            {
                visualEffectObjectBis.GetComponent<VisualEffect>().SetFloat("Lifetime", 0);
            }

            if (vfxTimeBis <= -0.65f)
            {
                Destroy(visualEffectObjectBis);
                visualEffectObjectBis = null;
            }
        }

        float targetSpeed = (moveDir == 0) ? 0 : Mathf.Sign(moveDir) * speed;
        float speedDiff = targetSpeed - _rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : decceleration; 
        float movement = Mathf.Abs(speedDiff) * accelRate * Mathf.Sign(speedDiff);
        _rb.AddForce(movement * Vector2.right);

        if (lastGroundTime >= 0 && moveDir == 0)
        {
            float amount = Mathf.Min(Mathf.Abs(_rb.velocity.x), Mathf.Abs(frictionAmount)) * Mathf.Sign(_rb.velocity.x);
            _rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }

        if (_rb.velocity.y < 0.5f) _rb.gravityScale = gravityScale * fallGravityMultiplier;
        else _rb.gravityScale = gravityScale;
    }

    public bool CheckGround()
    {
        return Physics2D.OverlapBox(checkGroundPoint.position + (Vector3)checkGroundOffset , checkGroundSize, 0, groundLayers);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (lastGroundTime >= -coyoteTime && !isJumping)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, 0);
            _rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
            lastJumpTime = 0;
            isJumping = true;
            PlayVFX();

        } else
        {
            jumpInputBuffer = jumpInputBufferTime;
        }
    }

    private void JumpCancel(InputAction.CallbackContext context)
    {
        if (isJumping && _rb.velocity.y > 0)
        {
            _rb.AddForce((1 - jumpCutMultiplier) * _rb.velocity.y * Vector2.down, ForceMode2D.Impulse);
        }
    }

    private void PlayVFX()
    {
        if (vfxTime <= -0.65f && visualEffectObject == null)
        {
            vfxTime = 0;
            visualEffectObject = Instantiate(vfxPrefab, transform.position - new Vector3(0, 0.5f, 0), Quaternion.identity);
            visualEffectObject.GetComponent<VisualEffect>().SetFloat("Lifetime", 0.5f);
        }
        else if (vfxTimeBis <= -0.65f && visualEffectObjectBis == null)
        {
            vfxTimeBis = 0;
            visualEffectObjectBis = Instantiate(vfxPrefab, transform.position - new Vector3(0, 0.5f, 0), Quaternion.identity);
            visualEffectObjectBis.GetComponent<VisualEffect>().SetFloat("Lifetime", 0.5f);
        }
    }
}
