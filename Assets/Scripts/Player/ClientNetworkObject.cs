using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkObject : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client authoritative
    }

}
