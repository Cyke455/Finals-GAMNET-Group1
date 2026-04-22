using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TimeCounter : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeCounterLabel;
    private bool _isNotCounting = true;

    private NetworkVariable<float> _timeElapsed = new NetworkVariable<float>(
    0f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
            NetworkManager.OnServerStarted += OnServerStarted;

        _timeElapsed.OnValueChanged += OnTimeChanged;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer)
            NetworkManager.OnServerStarted -= OnServerStarted;

        _timeElapsed.OnValueChanged -= OnTimeChanged;
    }
    private void OnServerStarted()
    {
        _isNotCounting = false; //counting begins
        _playersFinished.Value = 0;
    }
    private void OnTimeChanged(float oldValue, float newValue)
    {
        var minutes = newValue / 60;
        var seconds = newValue % 60;
        _timeCounterLabel.text = $"{minutes:00}:{seconds:00}";
    }
    private void Update()
    {
        if (!IsServer || _isNotCounting) return;
        _timeElapsed.Value += Time.deltaTime;
    }

    private NetworkVariable<int> _playersFinished = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public void OnReachFlag()
    {
        if (!IsServer) return;
        _playersFinished.Value++;

        int totalPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (_playersFinished.Value >= totalPlayers)
            StopTimerRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void StopTimerRpc()
    {
        _isNotCounting = true;
        _timeCounterLabel.color = Color.green;
    }
}
