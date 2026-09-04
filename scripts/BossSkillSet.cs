using UnityEngine;

[CreateAssetMenu(menuName = "Boss/BossSkillSet")]
public class BossSkillSet : ScriptableObject
{
    public BossDifficulty difficulty;
    public BossSkill[] skills;
}
