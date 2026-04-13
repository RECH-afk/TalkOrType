using DG.Tweening;
using RKS.TalkOrType.Core;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;
using RKS.TalkOrType.Core.Managers;
using RKS.TalkOrType.Core.Network;

namespace RKS.TalkOrType.UI
{
    sealed class MenuController : RKSBehaviour
    {
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform buttonsContainer;
        [SerializeField] private RectTransform playContainer;
        [SerializeField] private RectTransform settingsContainer;
        [SerializeField] private RectTransform creditsContainer;
        [SerializeField] private RectTransform steamContainer;

        [SerializeField] private RectTransform lobbyLeftContainer;
        [SerializeField] private RectTransform lobbyRightContainer;

        [SerializeField] private RectTransform settingsLeftContainer;
        [SerializeField] private RectTransform settingsRightContainer;

        [SerializeField] private GameObject leavePopup;
        [SerializeField] private GameObject kickPopup;
        [SerializeField] private GameObject unavailableLobbyPopup;
        [SerializeField] private GameObject notEnoughPlayersLobbyPopup;

        [SerializeField] private GameObject renamePopup;
        [SerializeField] private TMP_InputField renameInput;
        [SerializeField] private TextMeshProUGUI lobbyNameText;

        [SerializeField] private TextMeshProUGUI stateText;

        [SerializeField] private RawImage steamAvatar;
        [SerializeField] private TextMeshProUGUI steamNicknameText;

        [SerializeField] private TMP_InputField joinInput;

        [SerializeField] private float duration = 0.6f;
        [SerializeField] private Ease ease = Ease.OutExpo;

        private Friend playerData;

        private Vector2 logoStartPos;
        private Vector2 containersStartOffscreen;
        private Vector2 logoStartOffscreen;
        private Vector2 steamStartOffscreen;

        private Vector2 lobbyLeftStart;
        private Vector2 lobbyRightStart;

        private Vector2 lobbyLeftOffscreen;
        private Vector2 lobbyRightOffscreen;

        private Vector2 settingsLeftStart;
        private Vector2 settingsRightStart;

        private Vector2 settingsLeftOffscreen;
        private Vector2 settingsRightOffscreen;

        private RectTransform currentScreen;
        private bool isTransitioning;

        [Inject] private AvatarService _avatarService;
        [Inject] private NetworkService _network;
        [Inject] private LobbyManagerSteam _lobby;
        [Inject] private LobbyController _lobbyController;

        protected override void OnReady()
        {
            playerData = new Friend(SteamClient.SteamId);

            LoadSteamData();

            logoStartPos = logo.anchoredPosition;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            logoStartOffscreen = logoStartPos - Vector2.right * screenWidth;
            containersStartOffscreen = buttonsContainer.anchoredPosition - Vector2.right * screenWidth;
            steamStartOffscreen = steamContainer.anchoredPosition - Vector2.up * screenHeight;

            lobbyLeftStart = lobbyLeftContainer.anchoredPosition;
            lobbyRightStart = lobbyRightContainer.anchoredPosition;

            lobbyLeftOffscreen = lobbyLeftStart - Vector2.right * screenWidth;
            lobbyRightOffscreen = lobbyRightStart + Vector2.right * screenWidth;

            settingsLeftStart = settingsLeftContainer.anchoredPosition;
            settingsRightStart = settingsRightContainer.anchoredPosition;

            settingsLeftOffscreen = settingsLeftStart - Vector2.right * screenWidth;
            settingsRightOffscreen = settingsRightStart + Vector2.right * screenWidth;

            logo.anchoredPosition = logoStartOffscreen;
            playContainer.anchoredPosition = containersStartOffscreen;
            settingsContainer.anchoredPosition = containersStartOffscreen;
            buttonsContainer.anchoredPosition = containersStartOffscreen;
            steamContainer.anchoredPosition = steamStartOffscreen;

            lobbyLeftContainer.anchoredPosition = lobbyLeftOffscreen;
            lobbyRightContainer.anchoredPosition = lobbyRightOffscreen;

            settingsLeftContainer.anchoredPosition = settingsLeftOffscreen;
            settingsRightContainer.anchoredPosition = settingsRightOffscreen;

            leavePopup.SetActive(false);

            ShowStartup();

            SetState("v0.1");
            _lobby.OnLobbyEntered += ShowLobby;
            _lobby.OnLobbyUpdated += OnLobbyUpdated;
            _lobby.OnLobbyLeft += HideLobby;
            _lobby.OnKicked += ShowKickMessage;
        }

        void ShowStartup()
        {
            logo.DOAnchorPos(logoStartPos, duration).SetEase(ease);

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.right * Screen.width, duration)
                .SetEase(ease);

            steamContainer
                .DOAnchorPos(steamStartOffscreen + Vector2.up * Screen.height, duration)
                .SetEase(ease);
        }

        protected override void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }
        }

        async void LoadSteamData()
        {
            steamNicknameText.text = playerData.Name;

            steamAvatar.texture = null;

            var texture = await _avatarService.GetAvatar(playerData);

            if (texture != null)
                steamAvatar.texture = texture;
        }

        void HandleEscape()
        {
            if (isTransitioning) return;

            if (leavePopup.activeSelf)
            {
                CloseLeavePopup();
                return;
            }

            if (notEnoughPlayersLobbyPopup.activeSelf)
            {
                CloseKickPopup();
                return;
            }

            if (unavailableLobbyPopup.activeSelf)
            {
                CloseLobbyUnavailablePopup();
                return;
            }

            if (_lobby.CurrentLobby.HasValue)
            {
                ShowLeavePopup();
                return;
            }

            if (currentScreen != null)
            {
                HideCurrentScreen();
            }
        }

        public void OnClickCreateLobby()
        {
            _lobbyController.CreateLobby();
        }

        public void OnClickJoinLobby()
        {
            if (string.IsNullOrWhiteSpace(joinInput.text)) return;
            _lobbyController.JoinLobby();
        }

        public void OnClickLeaveLobby()
        {
            ShowLeavePopup();
        }

        public void OnClickInvite()
        {
            Debug.Log("Invite clicked");

            _lobbyController.Invite();
        }

        public void OnConfirmLeave()
        {
            leavePopup.SetActive(false);
            _lobbyController.LeaveLobby();
        }

        public void OnCancelLeave()
        {
            CloseLeavePopup();
        }

        void ShowLeavePopup()
        {
            leavePopup.SetActive(true);
            leavePopup.transform.localScale = Vector3.zero;
            leavePopup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        void CloseLeavePopup()
        {
            leavePopup.transform
                .DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => leavePopup.SetActive(false));
        }
        void ShowKickPopup()
        {
            kickPopup.SetActive(true);
            kickPopup.transform.localScale = Vector3.zero;
            kickPopup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

         public void CloseKickPopup()
        {
            kickPopup.transform
                .DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => kickPopup.SetActive(false));
        }

        void ShowNotEnoughPlayersPopup()
        {
            notEnoughPlayersLobbyPopup.SetActive(true);
            notEnoughPlayersLobbyPopup.transform.localScale = Vector3.zero;
            notEnoughPlayersLobbyPopup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        public void CloseNotEnoughPlayersPopup()
        {
            notEnoughPlayersLobbyPopup.transform
                .DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => notEnoughPlayersLobbyPopup.SetActive(false));
        }

        void ShowLobbyUnavailablePopup()
        {
            unavailableLobbyPopup.SetActive(true);
            unavailableLobbyPopup.transform.localScale = Vector3.zero;
            unavailableLobbyPopup.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        public void CloseLobbyUnavailablePopup()
        {
            unavailableLobbyPopup.transform
                .DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => unavailableLobbyPopup.SetActive(false));
        }

        public void ShowPlay() => ShowScreen(playContainer, "Play");
        public void ShowCredits() => ShowScreen(creditsContainer, "Credits");

        void ShowScreen(RectTransform screen, string stateName)
        {
            if (isTransitioning) return;

            isTransitioning = true;

            if (currentScreen != null)
                HideScreen(currentScreen);

            currentScreen = screen;

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.left * Screen.width, duration)
                .SetEase(ease);

            screen
                .DOAnchorPos(screen.anchoredPosition + Vector2.right * Screen.width, duration)
                .SetEase(ease);

            SetState(stateName);

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }

        void HideCurrentScreen()
        {
            if (isTransitioning || currentScreen == null) return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.right * Screen.width, duration)
                .SetEase(ease);

            HideScreen(currentScreen);
            currentScreen = null;

            SetState("Main");

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }

        void HideScreen(RectTransform screen)
        {
            screen
                .DOAnchorPos(screen.anchoredPosition + Vector2.left * Screen.width, duration)
                .SetEase(ease);
        }
        public void OpenRename()
        {
            if (!_lobby.IsHost) return;

            renamePopup.SetActive(true);

            renameInput.text = _lobby.CurrentLobby.Value.GetData("name");
        }
        public void ConfirmRename()
        {
            if (!_lobby.IsHost) return;

            string newName = renameInput.text;

            if (string.IsNullOrWhiteSpace(newName))
                return;

            _lobby.SetLobbyName(newName);
            lobbyNameText.text = newName;

            renamePopup.SetActive(false);
        }
        public void CancelRename()
        {
            renamePopup.SetActive(false);
        }

        public async void StartGame()
        {
            if (_lobby.Players.Count < 2)
            {
                ShowNotEnoughPlayersPopup();
                return;
            }

            if (!_lobby.IsHost) return;

            await System.Threading.Tasks.Task.Delay(200);
            _network.SendToAll(_lobby.CurrentLobby.Value, "START_GAME");
            Transition?.LoadScene("IsGameScene");
        }

        void ShowLobby()
        {
            if (isTransitioning) return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.left * Screen.width, duration)
                .SetEase(ease);

            playContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.left * Screen.width, duration)
                .SetEase(ease);

            steamContainer
                .DOAnchorPos(steamStartOffscreen + Vector2.down * Screen.width, duration)
                .SetEase(ease);

            lobbyLeftContainer
                .DOAnchorPos(lobbyLeftStart, duration)
                .SetEase(ease);

            lobbyRightContainer
                .DOAnchorPos(lobbyRightStart, duration)
                .SetEase(ease);

            SetState("Lobby");

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }

        void HideLobby()
        {
            if (isTransitioning) return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.left * Screen.width, duration)
                .SetEase(ease);

            playContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.right * Screen.width, duration)
                .SetEase(ease);

            steamContainer
                .DOAnchorPos(steamStartOffscreen + Vector2.up * Screen.height, duration)
                .SetEase(ease);

            lobbyLeftContainer
                .DOAnchorPos(lobbyLeftOffscreen, duration)
                .SetEase(ease);

            lobbyRightContainer
                .DOAnchorPos(lobbyRightOffscreen, duration)
                .SetEase(ease);

            SetState("Play");

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }
        public void ShowSettings()
        {
            if (isTransitioning) return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.left * Screen.width, duration)
                .SetEase(ease);

            steamContainer
                .DOAnchorPos(steamStartOffscreen + Vector2.down * Screen.width, duration)
                .SetEase(ease);

            settingsLeftContainer
                .DOAnchorPos(settingsLeftStart, duration)
                .SetEase(ease);

            settingsRightContainer
                .DOAnchorPos(settingsRightStart, duration)
                .SetEase(ease);

            SetState("Settings");

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }
        public void HideSettings()
        {
            if (isTransitioning) return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(containersStartOffscreen + Vector2.right * Screen.width, duration)
                .SetEase(ease);

            steamContainer
                .DOAnchorPos(steamStartOffscreen + Vector2.up * Screen.height, duration)
                .SetEase(ease);

            settingsLeftContainer
                .DOAnchorPos(settingsLeftOffscreen, duration)
                .SetEase(ease);

            settingsRightContainer
                .DOAnchorPos(settingsRightOffscreen, duration)
                .SetEase(ease);

            SetState("Play");

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }

        void ShowKickMessage(bool byAdmin)
        {
            if (byAdmin)
                ShowKickPopup();
            else
                ShowLobbyUnavailablePopup();
        }
        void OnLobbyUpdated()
        {
            if (!_lobby.CurrentLobby.HasValue) return;
            lobbyNameText.text = _lobby.CurrentLobby.Value.GetData("name");
        }

        void SetState(string text)
        {
            stateText.text = text;
        }
    }
}