using UnityEngine;

[DisallowMultipleComponent]
public class PlayerLives : MonoBehaviour
{
    [Header("Lives")]
    [SerializeField] private int lives = 0;

    [Header("UI")]
    [SerializeField] private SkillCooldownUI skillCooldownUI;

    public int Lives => lives;

    private void Start()
    {
        RefreshUI();
    }

    public void SetLives(int value)
    {
        lives = Mathf.Max(0, value);
        RefreshUI();
    }

    public bool ConsumeLife(float reviveInvincibleTime)
    {
        if (lives <= 0)
            return false;

        lives--;
        RefreshUI();

        if (skillCooldownUI != null)
            skillCooldownUI.StartCooldown(reviveInvincibleTime);

        return true;
    }

    public void BindUI(SkillCooldownUI ui)
    {
        skillCooldownUI = ui;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (skillCooldownUI != null)
            skillCooldownUI.SetStackCount(lives);
    }
}