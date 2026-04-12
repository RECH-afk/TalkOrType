using UnityEngine;
using Zenject;
using TMPro;
using Steamworks;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private TextMeshProUGUI lobbyNameText;

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

        codeText.text = _lobby.GetLobbyCode();
        lobbyNameText.text = _lobby.CurrentLobby.Value.GetData("name");

        foreach (Transform t in content)
            Destroy(t.gameObject);

        foreach (var p in _lobby.Players)
        {
            var go = _container.InstantiatePrefab(playerPrefab, content);
            var ui = go.GetComponent<PlayerContainer>();

            bool isHost = _lobby.CurrentLobby.Value.Owner.Id == p.Id;
            bool canKick = _lobby.IsHost && p.Id != SteamClient.SteamId;

            ui.Setup(p, isHost, canKick, _lobby.Kick);
        }
    }

    private void Clear()
    {
        foreach (Transform t in content)
            Destroy(t.gameObject);
    }

    public void CopyCode()
    {
        GUIUtility.systemCopyBuffer = codeText.text;
        Debug.Log("Код скопирован");
    }
}