using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Netcode;

public class LavaMoveUp : NetworkBehaviour
{
    public float moveSpeed = 2;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsHost) return;

        transform.position = transform.position + (Vector3.up * moveSpeed) * Time.deltaTime;
    }
}
