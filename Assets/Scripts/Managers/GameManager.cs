using System;
using Managers;
using Managers.Serializer;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private SceneManager _sm;
    private static GameManager _instance;
    private readonly ISerializer _serializer = BinarySerializer.Instance;
    [SerializeField] private bool isMultiplayer;
    private bool _isHost;

    private readonly NetworkVariable<bool> _gameStarted = new();

    #region Events

    private void Awake()
    {
        if (_instance is not null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        _sm = FindFirstObjectByType<SceneManager>();
    }

    private async void Start()
    {
        try
        {
            if (isMultiplayer)
            {
                _isHost = _serializer.Deserialize($"{ISerializer.DebugDir}/NeedHost", true);
                _serializer.Serialize(!_isHost, $"{ISerializer.DebugDir}", "NeedHost");
                await UnityServices.InitializeAsync();
                _sm.logger.Log($"Starting as {(_isHost ? "Host" : "Client")}");

                // Ensuring no credential re-use between runs.
                if (AuthenticationService.Instance.IsSignedIn)
                    AuthenticationService.Instance.SignOut();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (_isHost)
                    NetworkManager.Singleton.StartHost();
                else
                    NetworkManager.Singleton.StartClient();
            }
        }
        catch (Exception e)
        {
            _sm.logger.Log("An error occurred while starting the game in GameManager::Start. See below.");
            _sm.logger.LogError(e.Message);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) // Host runs this
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        _gameStarted.OnValueChanged += OnGameStartedChanged;
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            _serializer.Serialize(true, $"{ISerializer.DebugDir}", "NeedHost");
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        if (_gameStarted != null)
            _gameStarted.OnValueChanged -= OnGameStartedChanged;
    }

    #endregion

    // Host only
    private void OnClientConnected(ulong clientId)
    {
        _sm.logger.Log($"Player {clientId} connected.", Color.red);
        TryStartGame();
    }

    // Host only
    private void OnClientDisconnected(ulong clientId)
    {
        _sm.logger.Log($"Player {clientId} disconnected.", Color.red);
        if (_gameStarted.Value)
            EndGame();
    }

    // Host only
    private void TryStartGame()
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            _sm.logger.Log("Both players connected. Starting game.", Color.red);
            _gameStarted.Value = true;
        }
        else
        {
            _sm.logger.Log($"Waiting for the {(IsServer ? "Client" : "Host")}...", Color.blue);
        }
    }

    // Host only
    private void EndGame()
    {
        _sm.logger.Log("Ending game...", Color.red);
        _gameStarted.Value = false;

        // You could also trigger a UI message, return to menu, etc.
    }

    // Host & Client
    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            _sm.logger.Log("Game Started!", Color.red);
            // Activate gameplay
        }
        else
        {
            _sm.logger.Log("Game Ended!", Color.red);
            ;
            // Show end screen
        }
    }
}