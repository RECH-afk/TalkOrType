namespace RKS.TalkOrType.Core
{
    [System.Serializable]
    public class GameData
    {
        public const int VERSION = 1;

        [System.Serializable]
        public class Data
        {
            public int version = VERSION;

            public bool isFirstRun = true;
            public bool isPlayerAgreedPlay = false;
            public string language = "en_US";
            public int frameRateIndex = 1;
            public int windowModeIndex = 0;
            public float volumeValue = 1.0f;
            public bool isVisualMoverEnabled = true;
        }
    }
}