using UnityEngine;
using Zenject;
using TMPro;
using Steamworks;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private GameObject renamePopup;
    [SerializeField] private TMP_InputField renameInput;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject playerPrefab;

    [Inject] private LobbyManagerSteam _lobby;
    [Inject] private DiContainer _container;

    private void Start()
    {
        _lobby.OnLobbyUpdated += Refresh;
        _lobby.OnLobbyLeft += Clear;

        renamePopup.SetActive(false);
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

    public void OpenRename()
    {
        if (!_lobby.IsHost) return;
        renamePopup.SetActive(true);
        renameInput.text = "";
    }

    public void ConfirmRename()
    {
        if (!_lobby.IsHost) return;
        _lobby.SetLobbyName(renameInput.text);
        renamePopup.SetActive(false);
    }

    public void CancelRename()
    {
        renamePopup.SetActive(false);
    }

    private void Refresh()
    {
        codeText.text = _lobby.GetLobbyCode();

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
}