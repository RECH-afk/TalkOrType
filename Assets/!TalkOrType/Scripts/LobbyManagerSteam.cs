using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using Zenject;
using UnityEngine;

public class LobbyManagerSteam : IInitializable
{
    [Inject] private NetworkService _network;

    public Lobby? CurrentLobby { get; private set; }

    public bool IsHost => CurrentLobby.HasValue &&
                          CurrentLobby.Value.Owner.Id == SteamClient.SteamId;

    private List<Friend> _players = new();
    public IReadOnlyList<Friend> Players => _players;

    public event Action OnLobbyUpdated;
    public event Action OnLobbyLeft;

    public void Initialize()
    {
        SteamMatchmaking.OnLobbyEntered += OnEntered;
        SteamMatchmaking.OnLobbyMemberJoined += (_, __) => Refresh();
        SteamMatchmaking.OnLobbyMemberLeave += (_, __) => Refresh();

        SteamFriends.OnGameLobbyJoinRequested += OnInvite;

        _network.OnMessage += OnMessage;
    }

    // =========================
    // CREATE
    // =========================

    public async void CreateLobby()
    {
        var lobby = await SteamMatchmaking.CreateLobbyAsync(8);

        if (!lobby.HasValue)
        {
            Debug.LogError("Create failed");
            return;
        }

        CurrentLobby = lobby;

        string code = GenerateCode();
        string lobbyName = "ЛОББИ " + SteamClient.Name;

        CurrentLobby.Value.SetPublic();
        CurrentLobby.Value.SetJoinable(true);

        CurrentLobby.Value.SetData("code", code);
        CurrentLobby.Value.SetData("name", lobbyName);

        Debug.Log($"Lobby created: {lobbyName}");

        Refresh();
    }

    // =========================
    // JOIN
    // =========================

    public async void JoinByCode(string code)
    {
        var list = await SteamMatchmaking.LobbyList
            .WithKeyValue("code", code.ToUpper())
            .WithSlotsAvailable(1)
            .RequestAsync();

        if (list == null || list.Length == 0)
        {
            Debug.LogError("Lobby not found");
            return;
        }

        JoinLobbyInternal(list[0].Id);
    }

    private async void JoinLobbyInternal(SteamId lobbyId)
    {
        var lobby = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

        if (!lobby.HasValue)
        {
            Debug.LogError("Join failed");
            return;
        }

        CurrentLobby = lobby;

        CheckKick();
        Refresh();
    }

    private void OnInvite(Lobby lobby, SteamId friend)
    {
        Debug.Log($"Invite from {friend}");
        JoinLobbyInternal(lobby.Id);
    }

    public void Invite()
    {
        if (!CurrentLobby.HasValue) return;

        SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
    }

    // =========================
    // LEAVE
    // =========================

    public void LeaveLobby()
    {
        if (!CurrentLobby.HasValue) return;

        if (IsHost)
            _network.SendToAll(CurrentLobby.Value, "HOST_LEFT");

        CurrentLobby.Value.Leave();
        ClearLobby();
    }

    private void ClearLobby()
    {
        CurrentLobby = null;
        _players.Clear();
        OnLobbyLeft?.Invoke();
    }

    // =========================
    // DATA
    // =========================

    public void SetLobbyName(string name)
    {
        if (!IsHost || !CurrentLobby.HasValue) return;

        CurrentLobby.Value.SetData("name", name);
    }

    public string GetLobbyCode()
    {
        return CurrentLobby?.GetData("code");
    }

    // =========================
    // PLAYERS
    // =========================

    private void Refresh()
    {
        if (!CurrentLobby.HasValue) return;

        var newList = new List<Friend>();

        foreach (var p in CurrentLobby.Value.Members)
            newList.Add(p);

        _players = newList;

        OnLobbyUpdated?.Invoke();
    }

    // =========================
    // KICK
    // =========================

    public void Kick(SteamId id)
    {
        if (!IsHost || !CurrentLobby.HasValue) return;

        CurrentLobby.Value.SetData($"kicked_{id}", "1");
        _network.Send(id, "KICK");
    }

    private void CheckKick()
    {
        if (!CurrentLobby.HasValue) return;

        var kicked = CurrentLobby.Value.GetData($"kicked_{SteamClient.SteamId}");

        if (kicked == "1")
            LeaveLobby();
    }

    // =========================
    // NETWORK
    // =========================

    private void OnMessage(SteamId sender, string msg)
    {
        if (msg == "KICK" || msg == "HOST_LEFT")
            LeaveLobby();
    }

    private void OnEntered(Lobby lobby)
    {
        CurrentLobby = lobby;
        CheckKick();
        Refresh();
    }

    // =========================
    // UTILS
    // =========================

    private string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";
        string code = "";

        for (int i = 0; i < 6; i++)
            code += chars[UnityEngine.Random.Range(0, chars.Length)];

        return code;
    }
}