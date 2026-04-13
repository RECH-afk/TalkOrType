using RKS.TalkOrType.Core.Managers;
using UnityEngine;
using Zenject;
using RKS.TalkOrType.Core.Network;

namespace RKS.TalkOrType.Core.Installers
{
    sealed class ProjectInstaller : MonoInstaller
    {
        [Header("Managers")]
        [SerializeField] private LocalizationManager localizationManagerPrefab;
        [SerializeField] private AudioManager audioManagerPrefab;
        [SerializeField] private DiscordManager discordControllerPrefab;
        [SerializeField] private SteamManager steamManagerPrefab;
        [SerializeField] private SaveManager saveManagerPrefab;
        [SerializeField] private TransitionManager transitionServicePrefab;

        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Installing global managers...");

            Container.Bind<LocalizationManager>().FromComponentInNewPrefab(localizationManagerPrefab).AsSingle().NonLazy();
            Container.Bind<AudioManager>().FromComponentInNewPrefab(audioManagerPrefab).AsSingle().NonLazy();
            Container.Bind<DiscordManager>().FromComponentInNewPrefab(discordControllerPrefab).AsSingle().NonLazy();

            Container.Bind<AvatarService>().AsSingle();

            Container.Bind<SteamManager>().FromComponentInNewPrefab(steamManagerPrefab).AsSingle().NonLazy();
            Container.Bind<SaveManager>().FromComponentInNewPrefab(saveManagerPrefab).AsSingle().NonLazy();
            Container.Bind<TransitionManager>().FromComponentInNewPrefab(transitionServicePrefab).AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<NetworkService>().AsSingle();
            Container.BindInterfacesAndSelfTo<LobbyManagerSteam>().AsSingle();
        }
    }
}