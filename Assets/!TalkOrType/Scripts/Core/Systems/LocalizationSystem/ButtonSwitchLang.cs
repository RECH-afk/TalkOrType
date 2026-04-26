using UnityEngine;
using Zenject;
using RKS.TalkOrType.Core.Managers;

public class ButtonSwitchLang : MonoBehaviour
{
    [SerializeField] private string languageCode;

    [Inject] private LocalizationManager _loc;

    public void OnClick()
    {
        if (!string.IsNullOrWhiteSpace(languageCode))
            _loc.SetLanguage(languageCode);
    }
}