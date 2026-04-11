using DG.Tweening;
using RKS.TalkOrType.Core;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RKS.TalkOrType.UI
{
    sealed class MenuController : RKSBehaviour
    {
        [Header("Main")]
        [SerializeField] private RectTransform logo;
        [SerializeField] private RectTransform buttonsContainer;
        [SerializeField] private RectTransform playContainer;
        [SerializeField] private RectTransform settingsContainer;
        [SerializeField] private RectTransform creditsContainer;
        [SerializeField] private RectTransform steamContainer;

        [Header("State Text")]
        [SerializeField] private TextMeshProUGUI stateText;

        [Header("Steam")]
        [SerializeField] private RawImage steamAvatar;
        [SerializeField] private TextMeshProUGUI steamNicknameText;
        private Friend playerData;

        [Header("Animation")]
        [SerializeField] private float moveDistance = 300f;
        [SerializeField] private float duration = 0.6f;
        [SerializeField] private Ease ease = Ease.OutExpo;

        private Vector2 logoStartPos;
        private Vector3 logoStartScale;
        private Vector2 buttonsStartOffscreen;
        private Vector2 logoStartOffscreen;
        private Vector2 steamStartOffscreen;

        private RectTransform currentScreen;
        private bool isTransitioning;

        protected override void OnReady()
        {
            playerData = new Friend(SteamClient.SteamId);
            LoadSteamData();

            logoStartPos = logo.anchoredPosition;
            logoStartScale = logo.localScale;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            logoStartOffscreen = logoStartPos - Vector2.right * screenWidth;
            buttonsStartOffscreen = buttonsContainer.anchoredPosition - Vector2.right * screenWidth;
            steamStartOffscreen = steamContainer.anchoredPosition - Vector2.up * screenHeight;

            logo.anchoredPosition = logoStartOffscreen;
            buttonsContainer.anchoredPosition = buttonsStartOffscreen;
            steamContainer.anchoredPosition = steamStartOffscreen;

            ShowStartup();

            SetState("v1.0");
        }

        void ShowStartup()
        {
            logo.DOAnchorPos(logoStartPos, duration).SetEase(ease);
            buttonsContainer.DOAnchorPos(buttonsStartOffscreen + Vector2.right * Screen.width, duration).SetEase(ease);
            steamContainer.DOAnchorPos(steamStartOffscreen + Vector2.up * Screen.height, duration).SetEase(ease);
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

            var img = (await playerData.GetLargeAvatarAsync());
            if (!img.HasValue) return;

            var i = img.Value;
            var tex = new Texture2D((int)i.Width, (int)i.Height, TextureFormat.RGBA32, false);

            var flipped = new byte[i.Data.Length];
            int row = (int)i.Width * 4;

            for (int y = 0; y < i.Height; y++)
                System.Array.Copy(i.Data, y * row, flipped, ((int)i.Height - y - 1) * row, row);

            tex.LoadRawTextureData(flipped);
            tex.Apply();

            steamAvatar.texture = tex;
        }

        void HandleEscape()
        {
            if (isTransitioning) return;

            if (currentScreen != null)
            {
                HideCurrentScreen();
                return;
            }
        }

        public void ShowPlay() => ShowScreen(playContainer, "Play");
        public void ShowSettings() => ShowScreen(settingsContainer, "Settings");
        public void ShowCredits() => ShowScreen(creditsContainer, "Credits");

        void SetState(string text)
        {
            stateText.text = text;
        }

        void ShowScreen(RectTransform screen, string stateName)
        {
            if (isTransitioning)
                return;

            isTransitioning = true;

            if (currentScreen != null)
                HideScreen(currentScreen);

            currentScreen = screen;

            buttonsContainer
                .DOAnchorPos(buttonsStartOffscreen + Vector2.right * Screen.width - Vector2.up * moveDistance, duration)
                .SetEase(ease);

            screen
                .DOAnchorPos(screen.anchoredPosition + Vector2.up * moveDistance, duration)
                .SetEase(ease);

            SetState(stateName);

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }

        void HideCurrentScreen()
        {
            if (isTransitioning || currentScreen == null)
                return;

            isTransitioning = true;

            buttonsContainer
                .DOAnchorPos(buttonsStartOffscreen + Vector2.right * Screen.width, duration)
                .SetEase(ease);

            HideScreen(currentScreen);
            currentScreen = null;

            SetState("Main");

            DOVirtual.DelayedCall(duration, () => isTransitioning = false);
        }

        void HideScreen(RectTransform screen)
        {
            screen
                .DOAnchorPos(screen.anchoredPosition - Vector2.up * moveDistance, duration)
                .SetEase(ease);
        }
    }
}