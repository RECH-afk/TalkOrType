using UnityEngine;
using Zenject;
using TMPro;
using Steamworks;
using RKS.TalkOrType.Core.Managers;

namespace RKS.TalkOrType.UI
{
    public class LobbyController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private TextMeshProUGUI codeText;
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI playersCountText;
        [SerializeField] private GameObject startButton;

        [Header("Players List")]
        [SerializeField] private Transform content;
        [SerializeField] private GameObject playerPrefab;

        [Inject] private LobbyManagerSteam _lobby;
        [Inject] private DiContainer _container;

        private void Start()
        {
            _lobby.OnLobbyUpdated += Refresh;
            _lobby.OnLobbyLeft += Clear;
        }

        public void CreateLobby()
        {
            _lobby.CreateLobby();
        }

        public void JoinLobby()
        {
            if (string.IsNullOrWhiteSpace(codeInput.text)) return;
            _lobby.JoinByCode(codeInput.text);
        }

        public void LeaveLobby()
        {
            _lobby.LeaveLobby();
        }

        public void Invite()
        {
            _lobby.Invite();
        }

        private void Refresh()
        {
            if (!_lobby.CurrentLobby.HasValue) return;

            var lobby = _lobby.CurrentLobby.Value;

            codeText.text = _lobby.GetLobbyCode();
            lobbyNameText.text = lobby.GetData("name");

            int currentPlayers = _lobby.Players.Count;
            int maxPlayers = lobby.MaxMembers;

            if (maxPlayers <= 0)
                maxPlayers = 12;

            playersCountText.text = $"{currentPlayers}/{maxPlayers}";

            foreach (Transform t in content)
                Destroy(t.gameObject);

            foreach (var p in _lobby.Players)
            {
                var go = _container.InstantiatePrefab(playerPrefab, content);
                var ui = go.GetComponent<LobbyPlayerContainer>();

                bool isHost = lobby.Owner.Id == p.Id;
                bool canKick = _lobby.IsHost && p.Id != SteamClient.SteamId;

                ui.Setup(p, isHost, canKick, _lobby.Kick);
            }

            startButton.SetActive(_lobby.IsHost);
        }

        private void Clear()
        {
            foreach (Transform t in content)
                Destroy(t.gameObject);

            playersCountText.text = "0/0";
        }

        public void CopyCode()
        {
            GUIUtility.systemCopyBuffer = codeText.text;
        }
    }
}