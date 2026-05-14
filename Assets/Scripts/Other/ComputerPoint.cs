using UnityEngine;
using Unity.Netcode;

public class ComputerPoint : NetworkBehaviour
{
    public NetworkVariable<bool> isActivated = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
        
    private Animator animator;
    
    public LevelManager roomManager; 

    void Start()
    {
        animator = GetComponent<Animator>();
        isActivated.OnValueChanged += OnStateChanged;
    }

    void OnStateChanged(bool oldVal, bool newVal)
    {
        if (animator != null) animator.SetBool("IsActivated", newVal);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        
        if (other.CompareTag("Player"))
        {
            isActivated.Value = true;
            if (roomManager != null) 
            {
                roomManager.CheckComputers();
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;
        
        if (other.CompareTag("Player"))
        {
            isActivated.Value = false;
        }
    }
    public void ResetComputer()
    {
        if (IsServer)
        {
            isActivated.Value = false;
        }
    }
}