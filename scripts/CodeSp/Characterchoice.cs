/// <summary>
/// 타이틀에서 선택한 캐릭터의 정보를
/// 게임 씬으로 넘겨주기 위한 정적 저장소
/// </summary>
public static class Characterchoice
{
    public static string Id;
    public static int Lifes;
    public static CharacterPassiveType PassiveType;
    public static float PassiveInterval;

    public static void Apply(CharacterEntry entry)
    {
        if (entry == null) return;

        Id = entry.id;
        Lifes = entry.defaultLives;
        PassiveType = entry.passiveType;
        PassiveInterval = entry.passiveInterval;
    }
}
