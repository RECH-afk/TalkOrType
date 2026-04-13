using Steamworks;
using Steamworks.Data;
using System;
using RKS.TalkOrType.Core;
using System.Collections.Generic;
using Zenject;
using UnityEngine;
using RKS.TalkOrType.Core.Network;
using RKS.TalkOrType.Core.Managers;

namespace RKS.TalkOrType.Core.Managers
{
    public class LobbyManagerSteam : IInitializable
    {
        [Inject] private NetworkService _network;
        [Inject] private TransitionManager transition;

        public Lobby? CurrentLobby { get; private set; }

        public bool IsHost => CurrentLobby.HasValue &&
                              CurrentLobby.Value.Owner.Id == SteamClient.SteamId;

        private List<Friend> _players = new();
        public IReadOnlyList<Friend> Players => _players;

        public event Action OnLobbyUpdated;
        public event Action OnLobbyLeft;
        public event Action OnLobbyEntered;
        public event Action<bool> OnKicked;

        public void Initialize()
        {
            SteamMatchmaking.OnLobbyEntered += OnEntered;
            SteamMatchmaking.OnLobbyMemberJoined += (lobby, member) =>
            {
                if (!CurrentLobby.HasValue) return;
                if (lobby.Id != CurrentLobby.Value.Id) return;

                Refresh();
            };
            SteamMatchmaking.OnLobbyMemberLeave += (lobby, member) =>
            {
                if (!CurrentLobby.HasValue) return;
                if (lobby.Id != CurrentLobby.Value.Id) return;

                Refresh();
            };
            SteamFriends.OnGameLobbyJoinRequested += OnInvite;
            SteamMatchmaking.OnLobbyDataChanged += (lobby) =>
            {
                if (CurrentLobby.HasValue && lobby.Id == CurrentLobby.Value.Id)
                    Refresh();
            };

            _network.OnMessage += OnMessage;
        }

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
            string lobbyName = $"Lobby {SteamClient.Name}";

            CurrentLobby.Value.SetPublic();
            CurrentLobby.Value.SetJoinable(true);

            CurrentLobby.Value.SetData("code", code);
            CurrentLobby.Value.SetData("name", lobbyName);

            Debug.Log($"Lobby created: {lobbyName}");

            Refresh();
        }

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

            var kicked = lobby.Value.GetData($"kicked_{SteamClient.SteamId}");

            if (kicked == "1")
            {
                return;
            }

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

        public void SetLobbyName(string name)
        {
            if (!IsHost || !CurrentLobby.HasValue) return;

            CurrentLobby.Value.SetData("name", name);
        }

        public string GetLobbyCode()
        {
            return CurrentLobby?.GetData("code");
        }

        private void Refresh()
        {
            var kicked = CurrentLobby.Value.GetData($"kicked_{SteamClient.SteamId}");

            if (kicked == "1")
            {
                OnKicked?.Invoke(true);
                LeaveLobby();
                return;
            }
            if (CurrentLobby.HasValue)
            {
                var owner = CurrentLobby.Value.Owner;

                if (owner.Id == 0)
                {
                    LeaveLobby();
                    return;
                }
            }
            if (!CurrentLobby.HasValue) return;

            var newList = new List<Friend>();

            foreach (var p in CurrentLobby.Value.Members)
                newList.Add(p);

            _players = newList;

            OnLobbyUpdated?.Invoke();
        }

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
            {
                OnKicked?.Invoke(true);
                LeaveLobby();
            }
        }

        private void OnMessage(SteamId sender, string msg)
        {
            if (msg == "KICK")
            {
                OnKicked?.Invoke(true);
                LeaveLobby();
            }

            if (msg == "HOST_LEFT")
            {
                OnKicked?.Invoke(false);
                LeaveLobby();
            }

            if (msg == "START_GAME")
            {
                transition?.LoadScene("IsGameScene");
            }

            Debug.Log($"MSG: {msg} from {sender}");
        }

        private void OnEntered(Lobby lobby)
        {
            CurrentLobby = lobby;

            CheckKick();

            if (!CurrentLobby.HasValue)
                return;

            OnLobbyEntered?.Invoke();

            Refresh();
        }

        private string GenerateCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ123456789";
            string code = "";

            for (int i = 0; i < 6; i++)
                code += chars[UnityEngine.Random.Range(0, chars.Length)];

            return code;
        }
    }
}