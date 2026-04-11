using Steamworks;
using UnityEngine;

namespace RKS.TalkOrType.Core.Managers
{
    public class SteamManager : RKSBehaviour
    {
        private const uint APP_ID = 480;

        protected override void OnInjected()
        {
            try
            {
                SteamClient.Init(APP_ID);
                Debug.Log($"[SteamManager] Successful initialization! Player: {SteamClient.Name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SteamManager] Initialization error: {e.Message}");
            }
        }

        protected override void OnDisposed()
        {
            SteamClient.Shutdown();
        }

        protected override void Update()
        {
            SteamClient.RunCallbacks();
        }
    }
}
