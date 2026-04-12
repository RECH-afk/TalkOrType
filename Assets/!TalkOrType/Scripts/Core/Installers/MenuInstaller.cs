using UnityEngine;
using Zenject;
using RKS.TalkOrType.UI;

public class MenuInstaller : MonoInstaller
{
    [SerializeField] private MenuController menuController;

    public override void InstallBindings()
    {
        Container.Bind<MenuController>().FromInstance(menuController).AsSingle();
        Container.BindInterfacesAndSelfTo<LobbyController>().FromComponentInHierarchy().AsSingle();
    }
}