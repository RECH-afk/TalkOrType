using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace RKS.TalkOrType.Core.Network
{
    public class LobbyManager : IInitializable
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
            SteamMatchmaking.OnLobbyCreated += OnCreated;
            SteamMatchmaking.OnLobbyEntered += OnEntered;
            SteamMatchmaking.OnLobbyMemberJoined += (_, __) => Refresh();
            SteamMatchmaking.OnLobbyMemberLeave += (_, __) => Refresh();

            _network.OnMessage += OnMessage;
        }

        public async void CreateLobby()
        {
            CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(8);

            if (!CurrentLobby.HasValue)
            {
                Debug.LogError("Create failed");
                return;
            }

            SetupLobby();
            Refresh();
        }

        public async void JoinLobby(ulong id)
        {
            CurrentLobby = await SteamMatchmaking.JoinLobbyAsync(id);

            if (!CurrentLobby.HasValue)
            {
                Debug.LogError("Join failed");
                return;
            }

            CheckKick();
            Refresh();
        }

        public void LeaveLobby()
        {
            if (!CurrentLobby.HasValue) return;

            if (IsHost)
                _network.SendToAll(CurrentLobby.Value, "HOST_LEFT");

            CurrentLobby.Value.Leave();
            CurrentLobby = null;

            _players.Clear();
            OnLobbyLeft?.Invoke();
        }

        private void SetupLobby()
        {
            CurrentLobby.Value.SetPublic();
            CurrentLobby.Value.SetJoinable(true);
            CurrentLobby.Value.SetData("name", "My Lobby");
        }

        public void SetLobbyName(string name)
        {
            if (IsHost)
                CurrentLobby.Value.SetData("name", name);
        }

        public string GetLobbyName() => CurrentLobby?.GetData("name");
        public string GetLobbyCode() => CurrentLobby?.Id.ToString();

        private void Refresh()
        {
            if (!CurrentLobby.HasValue) return;

            _players.Clear();

            foreach (var p in CurrentLobby.Value.Members)
                _players.Add(p);

            OnLobbyUpdated?.Invoke();
        }

        public void Kick(SteamId id)
        {
            if (!IsHost) return;

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

        public void Invite()
        {
            if (!CurrentLobby.HasValue) return;

            SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
        }

        private void OnMessage(SteamId sender, string msg)
        {
            if (msg == "KICK" || msg == "HOST_LEFT")
                LeaveLobby();
        }

        private void OnCreated(Result result, Lobby lobby)
        {
            CurrentLobby = lobby;
            SetupLobby();
            Refresh();
        }

        private void OnEntered(Lobby lobby)
        {
            CurrentLobby = lobby;
            CheckKick();
            Refresh();
        }
    }
}