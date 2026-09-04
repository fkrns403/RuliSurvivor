using UnityEngine;

public abstract class BossSkill : ScriptableObject
{
    public float cooldown = 3f;
    public abstract void Execute(BossContext ctx);
}
