using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    #region ANIMATOR PARAMETERS
    const string IDLE = "Idle";
    const string RUN = "Run";
    const string JUMP = "Jump";
    const string FALL = "Fall";
    const string PUSH = "Push";
    #endregion

    [Header("Colliders")]
    [SerializeField] private float _skinWidth;
    private BoxCollider2D _boxCollider;
    [SerializeField] private int _horizontalRaycastCount;
    [SerializeField] private int _verticalRaycastCount;
    private Vector2 _colliderSize;
    private float _colliderHeight;
    private float _colliderWidth;
    private float _horizontalRaycastSpace;
    private float _verticalRaycastSpace;
    [SerializeField] LayerMask _normalMask;

    [Header("Inputs")]
    [SerializeField] private KeyCode _moveRightKeyCode;
    [SerializeField] private KeyCode _moveLeftKeyCode;
    [SerializeField] private KeyCode _jumpKeyCode;

    [Header("Moverment")]
    [SerializeField] protected float xSpeed;
    [SerializeField] protected float _jumpSpeed;
    [SerializeField] protected float _jumpingTimer;
    private float _jumpingTimerTrack;
    [SerializeField] protected float _gravity;
    [SerializeField] private float _smoothOnGround;
    [SerializeField] private float _smoothOnAir;
    private float _xInput;
    private float _yInput;
    [SerializeField] private Vector2 _velocity = new Vector2();
    private bool pushing = false;

    private float _refXVelocity;
    private float _refYVelocity;

    private RaycastOrigins _raycastOrigins;

    struct RaycastOrigins
    {
        public Vector2 _topLeft, _topRight, _bottomLeft, _bottomRight;
    }

    private CollisionState _collisionState;
    struct CollisionState
    {
        public bool _below, above;
        public GameObject _verticalCollision;
        public GameObject _horizontalCollision;
    }

    [Header("Others")]
    [SerializeField] private bool _isFacingRight = true;
    private Animator _anim;



    private void Start()
    {
        _anim = GetComponent<Animator>();
        _boxCollider = GetComponent<BoxCollider2D>();

        calculateRaycastSpace();
    }

    private void setRaycastOrigins()
    {
        Bounds _boxBounds = _boxCollider.bounds;
        _boxBounds.Expand(_skinWidth * -1);

        _raycastOrigins._topLeft = new Vector2(_boxBounds.min.x, _boxBounds.max.y);
        _raycastOrigins._topRight = new Vector2(_boxBounds.max.x, _boxBounds.max.y);
        _raycastOrigins._bottomLeft = new Vector2(_boxBounds.min.x, _boxBounds.min.y);
        _raycastOrigins._bottomRight = new Vector2(_boxBounds.max.x, _boxBounds.min.y);
    }

    private void calculateRaycastSpace()
    {
        Bounds _boxBounds = _boxCollider.bounds;
        _boxBounds.Expand(_skinWidth * -1);

        _horizontalRaycastCount = Mathf.Clamp(_horizontalRaycastCount, 2, int.MaxValue);
        _verticalRaycastCount = Mathf.Clamp(_verticalRaycastCount, 2, int.MaxValue);

        _horizontalRaycastSpace = _boxBounds.size.y / (_horizontalRaycastCount - 1);
        _verticalRaycastSpace = _boxBounds.size.x / (_verticalRaycastCount - 1);
    }

    private void Update()
    {
        resetMovementAndInputs();

        updateMovementInputs();

        calculateHorizontalVelocity();

        calculateVerticalVelocity();


        move(_velocity * Time.deltaTime);
        setAnimation();
        flipX();

        if (_collisionState._below || _collisionState.above)
        {
            _velocity.y = 0;
        }
    }

    private void move(Vector2 velocity)
    {
        setRaycastOrigins();
        _collisionState._below = false;
        _collisionState.above = false;

        if (velocity.x != 0)
        {
            detectHorizontalCollision(ref velocity);
        }
        if (velocity.y != 0)
        {
            detectVerticalCollision(ref velocity);
        }

        transform.Translate(velocity);
    }

    private void calculateHorizontalVelocity()
    {
        _velocity.x = Mathf.SmoothDamp(_velocity.x, _xInput * xSpeed, ref _refXVelocity, ((_collisionState._below) ? _smoothOnGround : _smoothOnAir) * Time.deltaTime);
    }

    private void calculateVerticalVelocity()
    {

        if (_collisionState._below)
        {
            if (Input.GetKeyDown(_jumpKeyCode))
            {
                _velocity.y = _jumpSpeed;
                _jumpingTimerTrack = _jumpingTimer;
            }
        }

        if (Input.GetKey(_jumpKeyCode) && _jumpingTimerTrack > 0)
        {
            _velocity.y = _jumpSpeed;
            _jumpingTimerTrack -= Time.deltaTime;
        }

        if (Input.GetKeyUp(_jumpKeyCode))
        {
            _jumpingTimerTrack = 0;
        }

        _velocity.y += _gravity * Time.deltaTime;
    }

    private void updateMovementInputs()
    {
        if (Input.GetKey(_moveRightKeyCode))
        {
            _xInput = 1;
        }
        else if (Input.GetKey(_moveLeftKeyCode))
        {
            _xInput = -1;
        }

    }

    private void detectHorizontalCollision(ref Vector2 velocity)
    {
        pushing = false;
        _collisionState._horizontalCollision = null;

        float directionX = Mathf.Sign(velocity.x);
        float raycastLenght = Mathf.Abs(velocity.x) + _skinWidth;

        for (int i = 0; i < _horizontalRaycastCount; i++)
        {
            Vector2 raycastOrigin = (directionX == -1) ? _raycastOrigins._bottomLeft : _raycastOrigins._bottomRight;
            raycastOrigin += (Vector2.up * _horizontalRaycastSpace * i);

            RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.right * directionX, raycastLenght, _normalMask);
            Debug.DrawRay(raycastOrigin, Vector2.right * directionX * raycastLenght, Color.green);

            if (hit)
            {
                velocity.x = (hit.distance - _skinWidth) * directionX;
                raycastLenght = hit.distance;
                _collisionState._horizontalCollision = hit.collider.gameObject;
            }
        }

        if (_collisionState._horizontalCollision)
        {
            pushing = true;
        }
    }

    private void detectVerticalCollision(ref Vector2 velocity)
    {
        _collisionState._verticalCollision = null;

        float directionY = Mathf.Sign(velocity.y);
        float raycastLenght = Mathf.Abs(velocity.y) + _skinWidth;

        for (int i = 0; i < _verticalRaycastCount; i++)
        {
            Vector2 raycastOrigin = (directionY == -1) ? _raycastOrigins._bottomLeft : _raycastOrigins._topLeft;
            raycastOrigin += Vector2.right * (_verticalRaycastSpace * i);

            RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.up * directionY, raycastLenght, _normalMask);
            Debug.DrawRay(raycastOrigin, Vector2.up * directionY * raycastLenght, Color.green);

            if (hit)
            {
                velocity.y = (hit.distance - _skinWidth) * directionY;
                raycastLenght = hit.distance;

                _collisionState._verticalCollision = hit.collider.gameObject;

                _collisionState._below = (directionY < 0);
                _collisionState.above = (directionY > 0);
            }
        }
    }

    private void resetMovementAndInputs()
    {
        _xInput = 0;
        _yInput = 0;
    }

    private void setAnimation()
    {
        if (_collisionState._below)
        {
            if (_velocity.x != 0)
            {
                if (pushing)
                {
                    _anim.Play(Animator.StringToHash(PUSH));
                }
                else
                {
                    _anim.Play(Animator.StringToHash(RUN));
                }
            }
            else
            {
                _anim.Play(Animator.StringToHash(IDLE));
            }
        }
        else
        {
            if (_velocity.y > 0)
            {
                _anim.Play(Animator.StringToHash(JUMP));

            }
            else if (_velocity.y < 0)
            {
                _anim.Play(Animator.StringToHash(FALL));
            }
        }
    }

    private void flipX()
    {
        if (_velocity.x > 0 && !_isFacingRight)
        {
            _isFacingRight = !_isFacingRight;
            Vector2 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;
        }
        else if (_velocity.x < 0 && _isFacingRight)
        {
            _isFacingRight = !_isFacingRight;
            Vector2 newScale = transform.localScale;
            newScale.x *= -1;
            transform.localScale = newScale;
        }

    }
}
