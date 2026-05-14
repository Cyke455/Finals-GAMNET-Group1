using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    public ComputerPoint[] roomComputers;
    public Transform[] nextRoomSpawns;
    public LavaMoveUp nextRoomLava;
    public bool isFinalRoom;
    public GameEvent onWinEvent;

    public void CheckComputers()
    {
        if (!IsServer) return;
        foreach (var comp in roomComputers)
        {
            if (!comp.isActivated.Value) return;
        }
        
        if (isFinalRoom) 
            TriggerWinRpc();
        else 
            TeleportPlayersRpc();
    }

    [Rpc(SendTo.Everyone)]
    void TeleportPlayersRpc()
    {
        var players = GameObject.FindGameObjectsWithTag("Player")
            .OrderBy(p => p.GetComponent<NetworkObject>().OwnerClientId).ToArray();

        for (int i = 0; i < players.Length; i++)
        {
            if (i < nextRoomSpawns.Length)
            {
                var netObj = players[i].GetComponent<NetworkObject>();
                
                if (netObj.IsOwner)
                {
                    players[i].transform.position = nextRoomSpawns[i].position;
                    
                    if (players[i].TryGetComponent(out Rigidbody2D rb))
                        rb.linearVelocity = Vector2.zero; 
                }

                Animator anim = players[i].GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Appearing");
                }
            }
        }

        RespawnManager rm = FindAnyObjectByType<RespawnManager>();
        if (rm != null)
        {
            rm.UpdateSpawnPoints(nextRoomSpawns);
        }

        if (IsServer)
        {
            if (nextRoomLava != null) nextRoomLava.StartRising();
            foreach (var comp in roomComputers) comp.isActivated.Value = false;
        }
    }

    [Rpc(SendTo.Everyone)]
    void TriggerWinRpc()
    {
        if (onWinEvent != null) onWinEvent.RaiseEvent();
    }
}