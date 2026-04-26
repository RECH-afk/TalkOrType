using Steamworks;
using System;
using UnityEngine;

namespace RKS.TalkOrType.Core.Managers
{
    public class DiscordManager : RKSBehaviour
    {
        [Header("Discord Application Settings")]
        public long applicationID;

        [Header("Rich Presence")]
        public string details;
        public string state;
        public string largeImage;
        public string largeText;

        private Discord.Discord discord;
        private long startTime;

        private bool _isInitialized;
        private float _updateTimer;

        private const float UPDATE_INTERVAL = 5f;

        protected override void OnReady()
        {
            TryInitDiscord();
        }

        void TryInitDiscord()
        {
            try
            {
                discord = new Discord.Discord(applicationID, (ulong)Discord.CreateFlags.NoRequireDiscord);

                startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                _isInitialized = true;

                Debug.Log($"[DiscordManager] I'm ready!");

                UpdateStatus();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DiscordManager] I couldn't initialize. Error: {ex}");
                discord = null;
                _isInitialized = false;
            }
        }


        protected override void Update()
        {
            if (!_isInitialized || discord == null)
                return;

            try
            {
                discord.RunCallbacks();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DiscordManager] RunCallbacks error: {ex}");

                SafeDisposeDiscord();
                return;
            }

            _updateTimer += Time.deltaTime;

            if (_updateTimer >= UPDATE_INTERVAL)
            {
                _updateTimer = 0f;
                SafeInvoke(UpdateStatus);
            }
        }

        private void UpdateStatus()
        {
            if (discord == null)
                return;

            try
            {
                var activityManager = discord.GetActivityManager();

                var activity = new Discord.Activity
                {
                    Details = details,
                    State = state,
                    Assets =
                    {
                        LargeImage = largeImage,
                        LargeText = largeText
                    },
                    Timestamps =
                    {
                        Start = startTime
                    }
                };

                activityManager.UpdateActivity(activity, result =>
                {
                    if (result != Discord.Result.Ok)
                        Debug.LogWarning("[DiscordManager] UpdateActivity failed");
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DiscordManager] UpdateStatus error: {ex}");
            }
        }

        protected override void OnDisposed()
        {
            SafeDisposeDiscord();
        }

        void SafeDisposeDiscord()
        {
            if (discord == null)
                return;

            try
            {
                discord.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DiscordManager] Dispose error: {ex}");
            }

            discord = null;
            _isInitialized = false;

            Debug.Log("[DiscordManager] I'm disposed");
        }
    }
}