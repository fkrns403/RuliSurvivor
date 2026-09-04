using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HitFeedback2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rigid;

    [Header("Normal Flash")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private int flashCount = 2;

    [Header("Lightning Flash")]
    [SerializeField]
    private Color lightningColor =
        new Color(0.4f, 1f, 1f, 1f);

    [SerializeField]
    private float lightningFlashDuration = 0.03f;

    [SerializeField]
    private int lightningFlashCount = 3;

    [Header("Knockback")]
    [SerializeField]
    private float knockbackPower = 1f;

    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (rigid == null)
            rigid = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void PlayHit()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void PlayHit(Vector3 attackerPosition)
    {
        PlayHit();
        ApplyKnockback(attackerPosition);
    }

    public void PlayLightningHit()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(LightningFlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = hitColor;

            yield return new WaitForSeconds(
                flashDuration);

            spriteRenderer.color = originalColor;

            yield return new WaitForSeconds(
                flashDuration);
        }

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator LightningFlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        for (int i = 0; i < lightningFlashCount; i++)
        {
            spriteRenderer.color = lightningColor;

            yield return new WaitForSeconds(
                lightningFlashDuration);

            spriteRenderer.color = Color.white;

            yield return new WaitForSeconds(
                lightningFlashDuration);
        }

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private void ApplyKnockback(Vector3 attackerPosition)
    {
        if (rigid == null)
            return;

        if (knockbackPower <= 0f)
            return;

        Vector2 dir =
            transform.position - attackerPosition;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        rigid.AddForce(
            dir.normalized * knockbackPower,
            ForceMode2D.Impulse);
    }
}