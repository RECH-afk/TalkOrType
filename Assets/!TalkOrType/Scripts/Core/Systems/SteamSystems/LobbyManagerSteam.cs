using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using Zenject;
using UnityEngine;
using RKS.TalkOrType.Core.Network;

namespace RKS.TalkOrType.Core.Managers
{
    public enum LobbyState
    {
        Lobby,
        Starting,
        Game
    }

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
        public event Action OnUnavailable;

        private bool _sceneLoaded = false;
        private bool _isGameStarting = false;

        public void Initialize()
        {
            SteamMatchmaking.OnLobbyEntered += OnEntered;
            SteamMatchmaking.OnLobbyMemberJoined += OnMemberChanged;
            SteamMatchmaking.OnLobbyMemberLeave += OnMemberChanged;
            SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;

            SteamFriends.OnGameLobbyJoinRequested += OnInvite;

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

            CurrentLobby.Value.SetPublic();
            CurrentLobby.Value.SetJoinable(true);

            CurrentLobby.Value.SetData("code", GenerateCode());
            CurrentLobby.Value.SetData("name", $"Lobby {SteamClient.Name}");
            CurrentLobby.Value.SetData("state", "lobby");

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

            await SteamMatchmaking.JoinLobbyAsync(list[0].Id);
        }

        public void Invite()
        {
            if (!CurrentLobby.HasValue) return;

            SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
        }

        private async void OnInvite(Lobby lobby, SteamId friend)
        {
            await SteamMatchmaking.JoinLobbyAsync(lobby.Id);
        }

        private void OnEntered(Lobby lobby)
        {
            var state = lobby.GetData("state");

            if (state == "game")
            {
                var allowed = lobby.GetData($"allowed_{SteamClient.SteamId}");

                if (allowed != "1")
                {
                    Debug.Log("Not allowed to join (game already started)");

                    lobby.Leave();
                    OnUnavailable.Invoke();
                    return;
                }
            }

            CurrentLobby = lobby;

            Refresh();
            OnLobbyEntered?.Invoke();

            HandleState(lobby);
        }

        private void OnMemberChanged(Lobby lobby, Friend member)
        {
            if (!CurrentLobby.HasValue) return;
            if (lobby.Id != CurrentLobby.Value.Id) return;

            Refresh();
        }

        private void OnLobbyDataChanged(Lobby lobby)
        {
            if (GetState() == LobbyState.Game)
            {
                var allowed = lobby.GetData($"allowed_{SteamClient.SteamId}");

                if (allowed != "1")
                {
                    Debug.Log("Late kick (not allowed)");

                    LeaveLobby();
                    OnKicked?.Invoke(false);
                    return;
                }
            }

            if (!CurrentLobby.HasValue) return;
            if (lobby.Id != CurrentLobby.Value.Id) return;

            HandleState(lobby);
            Refresh();
        }

        public LobbyState GetState()
        {
            if (!CurrentLobby.HasValue) return LobbyState.Lobby;

            var state = CurrentLobby.Value.GetData("state");

            return state switch
            {
                "starting" => LobbyState.Starting,
                "game" => LobbyState.Game,
                _ => LobbyState.Lobby
            };
        }

        private void HandleState(Lobby lobby)
        {
            var state = lobby.GetData("state");

            if (state == "starting" && !_isGameStarting)
            {
                _isGameStarting = true;

                Debug.Log("Game starting...");

                transition?.LoadScene("IsGameScene");
            }

            if (state == "game" && !_sceneLoaded)
            {
                _sceneLoaded = true;
                transition?.LoadScene("IsGameScene");
            }
        }

        public async void StartGame()
        {
            if (!IsHost || !CurrentLobby.HasValue) return;
            if (_players.Count < 2) return;

            var lobby = CurrentLobby.Value;

            foreach (var p in _players)
            {
                lobby.SetData($"allowed_{p.Id}", "1");
            }

            lobby.SetData("state", "starting");

            await System.Threading.Tasks.Task.Delay(1000);

            lobby.SetData("state", "game");

            lobby.SetJoinable(false);
        }

        public void LeaveLobby()
        {
            if (!CurrentLobby.HasValue) return;

            if (IsHost)
                CurrentLobby.Value.SetData("state", "closed");

            CurrentLobby.Value.Leave();
            ClearLobby();
        }

        private void ClearLobby()
        {
            CurrentLobby = null;
            _players.Clear();
            _isGameStarting = false;

            OnLobbyLeft?.Invoke();
        }

        public void Kick(SteamId id)
        {
            if (!IsHost || !CurrentLobby.HasValue) return;

            CurrentLobby.Value.SetData($"kick_{id}", "1");
        }

        private void OnMessage(SteamId sender, NetMessage msg)
        {
        }

        private void Refresh()
        {
            if (!CurrentLobby.HasValue) return;

            if (CurrentLobby.Value.GetData($"kick_{SteamClient.SteamId}") == "1")
            {
                OnKicked?.Invoke(true);
                LeaveLobby();
                return;
            }

            var newList = new List<Friend>();

            foreach (var p in CurrentLobby.Value.Members)
                newList.Add(p);

            _players = newList;

            OnLobbyUpdated?.Invoke();
        }

        public void SetLobbyName(string name)
        {
            if (!IsHost || !CurrentLobby.HasValue) return;

            CurrentLobby.Value.SetData("name", name);
        }

        public string GetLobbyCode() => CurrentLobby?.GetData("code");

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