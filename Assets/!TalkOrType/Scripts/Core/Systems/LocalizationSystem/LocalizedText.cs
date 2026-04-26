using TMPro;
using UnityEngine;
using Zenject;
using RKS.TalkOrType.Core.Managers;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    private TMP_Text _text;

    [Inject] private LocalizationManager _loc;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        UpdateText();
        _loc.OnLanguageChanged += UpdateText;
    }

    void OnDisable()
    {
        _loc.OnLanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        _text.text = _loc.Get(key);
    }
}