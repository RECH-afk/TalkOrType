using Steamworks;
using Steamworks.Data;
using System;
using System.Text;
using Zenject;
using UnityEngine;

namespace RKS.TalkOrType.Core.Network
{
    public enum NetMessageType
    {
        FSM,
        Voice
    }

    [Serializable]
    public struct NetMessage
    {
        public NetMessageType Type;
        public string Payload;
    }

    public class NetworkService : IInitializable, ITickable
    {
        public event Action<SteamId, NetMessage> OnMessage;

        public void Initialize()
        {
            Debug.Log("[NET] Init");

            SteamNetworking.OnP2PSessionRequest += (id) =>
            {
                SteamNetworking.AcceptP2PSessionWithUser(id);
            };
        }

        public void Tick()
        {
            while (SteamNetworking.IsP2PPacketAvailable(out uint size))
            {
                byte[] buffer = new byte[size];

                uint msgSize = 0;
                SteamId sender = default;

                if (!SteamNetworking.ReadP2PPacket(buffer, ref msgSize, ref sender))
                    continue;

                string json = Encoding.UTF8.GetString(buffer, 0, (int)msgSize);

                try
                {
                    var msg = JsonUtility.FromJson<NetMessage>(json);
                    OnMessage?.Invoke(sender, msg);
                }
                catch
                {
                    Debug.LogWarning("[NET] Bad packet");
                }
            }
        }

        public void Send(SteamId id, NetMessageType type, string payload = "")
        {
            var msg = new NetMessage
            {
                Type = type,
                Payload = payload
            };

            var json = JsonUtility.ToJson(msg);
            var data = Encoding.UTF8.GetBytes(json);

            SteamNetworking.SendP2PPacket(id, data, data.Length, (int)P2PSend.Reliable);
        }

        public void SendToAll(Lobby lobby, NetMessageType type, string payload = "")
        {
            foreach (var p in lobby.Members)
            {
                if (p.Id == SteamClient.SteamId) continue;
                Send(p.Id, type, payload);
            }
        }

        public void Warmup(Lobby lobby)
        {
            foreach (var p in lobby.Members)
            {
                if (p.Id == SteamClient.SteamId) continue;

                SteamNetworking.AcceptP2PSessionWithUser(p.Id);
            }
        }
    }
}