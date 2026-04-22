using UnityEngine;
using Unity.Netcode.Components;
public class ClientNetworkAnmator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client authoritative
    }

}
