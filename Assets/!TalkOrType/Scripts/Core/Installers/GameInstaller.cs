using UnityEngine;
using Zenject;

public class UIInstaller : MonoInstaller
{
    [Header("Initial Theme")]
    public Color Accent = Color.cyan;
    public Color Background = new(0.1f, 0.1f, 0.1f);
    public Color Text = Color.white;

    public override void InstallBindings()
    {
        Container.Bind<UIManager>()
            .AsSingle()
            .WithArguments(Accent, Background, Text);
    }
}