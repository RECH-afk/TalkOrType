using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using System;
using Zenject;
using TMPro;
using RKS.TalkOrType.Core;
using RKS.TalkOrType.Core.Managers;

namespace RKS.TalkOrType.UI
{
    public class LobbyPlayerContainer : RKSBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private RawImage avatarImage;

        [SerializeField] private GameObject crown;
        [SerializeField] private Button kickButton;

        [SerializeField] private Texture defaultAvatar;

        [Inject] private AvatarService _avatarService;

        private SteamId _id;

        public void Setup(Friend player, bool isHost, bool canKick, Action<SteamId> onKick)
        {
            _id = player.Id;

            nameText.text = player.Name;

            crown.SetActive(isHost);

            kickButton.gameObject.SetActive(canKick);
            kickButton.onClick.RemoveAllListeners();
            kickButton.onClick.AddListener(() => onKick(_id));

            LoadAvatar(player);
        }

        private async void LoadAvatar(Friend player)
        {
            avatarImage.texture = defaultAvatar;

            var tex = await _avatarService.GetAvatar(player);

            if (tex != null)
                avatarImage.texture = tex;
        }
    }
}