using Steamworks;
using System;
using System.Text;
using Zenject;
using UnityEngine;

namespace RKS.TalkOrType.Core.Network
{
    public class NetworkService : IInitializable, ITickable
    {
        public event Action<SteamId, string> OnMessage;

        public void Initialize()
        {
            Debug.Log("Network ready");

            SteamNetworking.OnP2PSessionRequest += (SteamId id) =>
            {
                Debug.Log($"Accept P2P: {id}");
                SteamNetworking.AcceptP2PSessionWithUser(id);
            };
        }

        public void Tick()
        {
            uint size;

            while (SteamNetworking.IsP2PPacketAvailable(out size))
            {
                byte[] buffer = new byte[size];

                uint msgSize = 0;
                SteamId sender = default;

                if (SteamNetworking.ReadP2PPacket(buffer, ref msgSize, ref sender))
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, (int)msgSize);
                    OnMessage?.Invoke(sender, msg);
                }
            }
        }

        public void Send(SteamId id, string msg)
        {
            var data = Encoding.UTF8.GetBytes(msg);

            SteamNetworking.SendP2PPacket(
                id,
                data,
                data.Length,
                (int)P2PSend.Reliable
            );
        }

        public void SendToAll(Steamworks.Data.Lobby lobby, string msg)
        {
            foreach (var p in lobby.Members)
            {
                if (p.Id == SteamClient.SteamId) continue;
                SteamNetworking.AcceptP2PSessionWithUser(p.Id);

                Send(p.Id, msg);
            }
        }
    }
}