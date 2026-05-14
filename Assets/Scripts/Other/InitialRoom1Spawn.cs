using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class InitialRoom1Spawn : NetworkBehaviour
{
    public Transform p1Point;
    public Transform p2Point;

    public override void OnNetworkSpawn()
    {
        StartCoroutine(CatchAndMovePlayers());
    }

    private IEnumerator CatchAndMovePlayers()
    {
        for (int i = 0; i < 50; i++)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject p in players)
            {
                var networkObj = p.GetComponent<NetworkObject>();

                if (networkObj != null && networkObj.IsOwner)
                {
                    if (networkObj.OwnerClientId == 0)
                    {
                        p.transform.position = p1Point.position;
                    }
                    else 
                    {
                        p.transform.position = p2Point.position;
                    }

                    if (p.TryGetComponent(out Rigidbody2D rb))
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                }
            }

            if (players.Length >= 2) break;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
