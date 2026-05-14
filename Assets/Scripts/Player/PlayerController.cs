using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private GameEvent onFruitCollected, onReachFlag;

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
        _animator.runtimeAnimatorController = Character[this.OwnerClientId];
        _isFacingLeft.OnValueChanged += OnFacingChanged;
    }

    private void OnFacingChanged(bool oldValue, bool newValue)
    {
        _spriteRenderer.flipX = newValue;
    }
    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
        _playerInputActions.Player.Jump.performed += OnPlayerJump;
    }
    private void OnDisable()
    {
        _playerInputActions.Player.Jump.performed -= OnPlayerJump;
        _playerInputActions.Player.Disable();
    }
    private void OnPlayerJump(InputAction.CallbackContext context)
    {
        if (!_isGrounded) return;
        _animator.SetTrigger("Jump");
        _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
    private void Update()
    {
        if (!IsOwner) return; //For the Player to not sync with the movement
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
        SceneManager.LoadScene(TitleScreenSceneName);
    }

    [Rpc(SendTo.Everyone)]
    private void RequestRespawnServerRpc(ulong clientId)
    {
        FindAnyObjectByType<RespawnManager>().RespawnPlayer(clientId);
        GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Everyone)]
    private void DespawnServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Everyone)]
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
}