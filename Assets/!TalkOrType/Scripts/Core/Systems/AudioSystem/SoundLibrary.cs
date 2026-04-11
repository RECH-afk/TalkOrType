using UnityEngine;

[CreateAssetMenu(menuName = "Talk Or Type/Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Tooltip("All sounds included in this library.")]
    public SoundData[] sounds;
}
