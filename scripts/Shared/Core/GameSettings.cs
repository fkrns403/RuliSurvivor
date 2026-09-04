using UnityEngine;

[System.Serializable]
public class GameSettings
{
    [Header("Audio (0~1)")]
    public float master = 1f;
    public float bgm = 1f;
    public float sfx = 1f;

    [Header("Video")]
    public bool fullscreen = true;

    public int windowResolutionIndex = 2;
    public int qualityIndex = 0;

    public int targetFpsIndex = 1;
    public bool vSync = false;

    [Header("Game")]
    public bool screenShake = true;
    public float shakeStrength = 1f;
    public bool damageNumbers = true;

    public static GameSettings Default() => new GameSettings();
}
