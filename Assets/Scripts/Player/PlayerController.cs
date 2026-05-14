using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private GameEvent onFruitCollected;
    [SerializeField] private GameEvent onReachFlag;

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private float _moveDirection;
    private bool _isGrounded;
    private PlayerInputActions _playerInputActions;

    public string TitleScreenSceneName;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _playerInputActions = new PlayerInputActions();
    }

    private NetworkVariable<bool> _isFacingLeft = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    [SerializeField] RuntimeAnimatorController[] Character;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        int safeIndex = (int)(this.OwnerClientId % (ulong)Character.Length);
        _animator.runtimeAnimatorController = Character[safeIndex];
        
        _isFacingLeft.OnValueChanged += OnFacingChanged;

        if (SceneManager.GetActiveScene().name == TitleScreenSceneName)
        {
            _spriteRenderer.enabled = false;
            _rb.simulated = false; 
        }
        else
        {
            if (_animator != null) _animator.SetTrigger("Appearing");
        }
    }

    private void OnFacingChanged(bool oldValue, bool newValue)
    {
        _spriteRenderer.flipX = newValue;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; 

        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Jump.performed += OnPlayerJump;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; 

        _playerInputActions.Player.Jump.performed -= OnPlayerJump;
        _playerInputActions.Player.Disable();
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != TitleScreenSceneName)
        {
            _spriteRenderer.enabled = true;
            _rb.simulated = true;
            
            if (_animator != null) _animator.SetTrigger("Appearing");
        }
    }

    private void OnPlayerJump(InputAction.CallbackContext context)
    {
        if (SceneManager.GetActiveScene().name == TitleScreenSceneName) return;

        if (!_isGrounded) return;
        _animator.SetTrigger("Jump");
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void Update()
    {
        if (!IsOwner) return; 

        if (SceneManager.GetActiveScene().name == TitleScreenSceneName) return;

        _moveDirection = _playerInputActions.Player.Move.ReadValue<float>();
        
        if (_moveDirection < 0) _isFacingLeft.Value = true;
        else if (_moveDirection > 0) _isFacingLeft.Value = false;

        _animator.SetBool("IsMoving", _moveDirection != 0);
        _animator.SetBool("IsGrounded", _isGrounded);
    }

    private void FixedUpdate()
    {
        _rb.linearVelocityX = _moveDirection * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }
    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.collider.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void DoRPC()
    {
        print("apple");
        onFruitCollected.RaiseEvent();
        FindAnyObjectByType<RandomSpawner>().StopSpawning();
    }

    [Rpc(SendTo.Everyone)]
    public void FinishedRPC()
    {
        print("Done");
        _hitSpike = false;
        _animator.SetTrigger("Disappear");
        onReachFlag.RaiseEvent();
    }

    private bool _hitSpike = false;

    [Rpc(SendTo.Everyone)]
    public void HitSpikeRPC()
    {
        _hitSpike = true;
        _animator.SetTrigger("Disappear");
    }

    [Rpc(SendTo.Server)]
    private void RequestRespawnServerRpc(ulong clientId)
    {
        FindAnyObjectByType<RespawnManager>().RespawnPlayer(clientId);
        GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Server)]
    private void DespawnServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Server)]
    private void NotifyRespawnServerRpc(ulong clientId)
    {
        FindAnyObjectByType<RespawnManager>().RespawnPlayer(clientId);
        GetComponent<NetworkObject>().Despawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Spikes"))
        {
            HitSpikeRPC();
        }

        if (other.CompareTag("Fruit"))
        {
            other.GetComponent<FruitController>().Collect();
            DoRPC();
        }

        if (other.CompareTag("Finish"))
        {
            FlagController flag = other.GetComponent<FlagController>();
            if (flag.IsActivated)
            {
                FinishedRPC();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.collider.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }

    public void OnDisappear()
    {
        if (_hitSpike)
        {
            NotifyRespawnServerRpc(OwnerClientId);
        }
        else
        {
            if (IsServer)
                GetComponent<NetworkObject>().Despawn();
            else
                DespawnServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isFacingLeft.OnValueChanged -= OnFacingChanged;
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        if (SceneManager.GetActiveScene().name == TitleScreenSceneName) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, mainCam.transform.position.z);
            float smoothSpeed = 8f; 
            mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
    }
}