using UnityEngine;
using Unity.Netcode;

public class LavaMoveUp : NetworkBehaviour
{
    public float moveSpeed = 0.5f;
    public bool startsRising;
    private NetworkVariable<bool> isRising = new NetworkVariable<bool>(false);

    private Vector3 _startPosition;

    public override void OnNetworkSpawn()
    {
        _startPosition = transform.position;

        if (IsServer && startsRising)
        {
            isRising.Value = true;
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (isRising.Value)
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }
    }

    public void StartRising()
    {
        if (IsServer) isRising.Value = true;
    }

    public void ResetLava()
    {
        if (IsServer)
        {
            transform.position = _startPosition;
        }
    }
}
