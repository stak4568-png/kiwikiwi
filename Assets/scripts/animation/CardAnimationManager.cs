using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 카드 애니메이션 관리자
/// 소환, 공격, 파괴 등 카드 행동에 애니메이션 적용
/// </summary>
public class CardAnimationManager : MonoBehaviour
{
    public static CardAnimationManager instance;

    [Header("이징 설정")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve bounceCurve;

    [Header("시간 설정")]
    public float drawDuration = 0.4f;
    public float summonDuration = 0.5f;
    public float attackDuration = 0.3f;
    public float damageDuration = 0.2f;
    public float deathDuration = 0.4f;
    public float releaseDuration = 0.3f;

    [Header("이펙트 프리팹")]
    public GameObject mirrorRipplePrefab;     // 거울 파문
    public GameObject shatterEffectPrefab;    // 파편화
    public GameObject attackImpactPrefab;     // 충격파
    public GameObject heartEffectPrefab;      // 유혹 하트
    public GameObject damageTextPrefab;       // 데미지 텍스트
    public GameObject manaGainEffectPrefab;   // 마나 획득 (릴리스)

    [Header("색상")]
    public Color damageFlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    public Color healFlashColor = new Color(0.3f, 1f, 0.3f, 1f);

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 기본 바운스 커브 생성
        if (bounceCurve == null || bounceCurve.length == 0)
        {
            bounceCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(0.7f, 0.9f),
                new Keyframe(1f, 1f)
            );
        }
    }

    // ===== 드로우 애니메이션 =====

    /// <summary>
    /// 카드 드로우 애니메이션 (덱 → 손패)
    /// </summary>
    public IEnumerator AnimateDraw(Transform card, Vector3 deckPosition, Vector3 handPosition)
    {
        if (card == null) yield break;

        card.position = deckPosition;
        card.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < drawDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / drawDuration;
            float curveT = moveCurve.Evaluate(t);

            card.position = Vector3.Lerp(deckPosition, handPosition, curveT);
            card.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, bounceCurve.Evaluate(t));

            yield return null;
        }

        card.position = handPosition;
        card.localScale = Vector3.one;
    }

    // ===== 소환 애니메이션 =====

    /// <summary>
    /// 카드 소환 애니메이션 (손패 → 필드)
    /// </summary>
    public IEnumerator AnimateSummon(Transform card, Vector3 targetPosition, Action onRipple = null)
    {
        if (card == null) yield break;

        Vector3 startPos = card.position;
        Vector3 startScale = card.localScale;

        // 거울 파문 이펙트
        if (mirrorRipplePrefab != null)
        {
            GameObject ripple = Instantiate(mirrorRipplePrefab, targetPosition, Quaternion.identity);
            Destroy(ripple, 1.5f);
            onRipple?.Invoke();
        }

        // 스케일 줄이기
        float elapsed = 0f;
        float shrinkDuration = summonDuration * 0.3f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;
            card.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        // 위치 이동 (순간)
        card.position = targetPosition;

        // 스케일 복구 (바운스)
        elapsed = 0f;
        float growDuration = summonDuration * 0.7f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / growDuration;
            card.localScale = Vector3.one * bounceCurve.Evaluate(t);
            yield return null;
        }

        card.localScale = Vector3.one;
    }

    // ===== 공격 애니메이션 =====

    /// <summary>
    /// 카드 공격 애니메이션 (대상을 향해 돌진)
    /// </summary>
    public IEnumerator AnimateAttack(Transform attacker, Vector3 targetPosition, Action onImpact = null)
    {
        if (attacker == null) yield break;

        Vector3 startPos = attacker.position;
        Vector3 midPoint = Vector3.Lerp(startPos, targetPosition, 0.7f);

        // 돌진
        float elapsed = 0f;
        float chargeDuration = attackDuration * 0.6f;
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeDuration;
            attacker.position = Vector3.Lerp(startPos, midPoint, moveCurve.Evaluate(t));
            yield return null;
        }

        // 충격
        if (attackImpactPrefab != null)
        {
            GameObject impact = Instantiate(attackImpactPrefab, midPoint, Quaternion.identity);
            Destroy(impact, 1f);
        }
        onImpact?.Invoke();

        // 복귀
        elapsed = 0f;
        float returnDuration = attackDuration * 0.4f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            attacker.position = Vector3.Lerp(midPoint, startPos, moveCurve.Evaluate(t));
            yield return null;
        }

        attacker.position = startPos;
    }

    // ===== 피격 애니메이션 =====

    /// <summary>
    /// 피격 애니메이션 (흔들림 + 색상 변화)
    /// </summary>
    public IEnumerator AnimateDamage(Transform card, UnityEngine.UI.Image cardImage, int damageAmount)
    {
        if (card == null) yield break;

        Vector3 startPos = card.position;
        Color originalColor = cardImage != null ? cardImage.color : Color.white;

        // 데미지 텍스트 표시
        if (damageTextPrefab != null && damageAmount > 0)
        {
            GameObject dmgText = Instantiate(damageTextPrefab, card.position + Vector3.up * 50f, Quaternion.identity, card.parent);
            var tmp = dmgText.GetComponent<TMPro.TMP_Text>();
            if (tmp != null) tmp.text = $"-{damageAmount}";
            Destroy(dmgText, 1f);
        }

        // 흔들림 + 색상 변화
        float elapsed = 0f;
        int shakeCount = 3;
        float shakeAmount = 10f;

        while (elapsed < damageDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageDuration;

            // 흔들림
            float shake = Mathf.Sin(t * shakeCount * Mathf.PI * 2) * shakeAmount * (1 - t);
            card.position = startPos + new Vector3(shake, 0, 0);

            // 색상 플래시
            if (cardImage != null)
            {
                cardImage.color = Color.Lerp(damageFlashColor, originalColor, t);
            }

            yield return null;
        }

        card.position = startPos;
        if (cardImage != null) cardImage.color = originalColor;
    }

    // ===== 파괴 애니메이션 =====

    /// <summary>
    /// 카드 파괴 애니메이션 (거울 파편 흩어짐)
    /// </summary>
    public IEnumerator AnimateDeath(Transform card, Action onComplete = null)
    {
        if (card == null) yield break;

        Vector3 startScale = card.localScale;
        Vector3 startPos = card.position;

        // 파편 이펙트
        if (shatterEffectPrefab != null)
        {
            GameObject shatter = Instantiate(shatterEffectPrefab, startPos, Quaternion.identity);
            Destroy(shatter, 2f);
        }

        // 스케일 감소 + 회전
        float elapsed = 0f;
        while (elapsed < deathDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathDuration;

            card.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            card.Rotate(0, 0, Time.deltaTime * 360f);

            yield return null;
        }

        onComplete?.Invoke();
    }

    // ===== 릴리스 애니메이션 =====

    /// <summary>
    /// 릴리스 애니메이션 (빛나며 사라짐)
    /// </summary>
    public IEnumerator AnimateRelease(Transform card, Action onComplete = null)
    {
        if (card == null) yield break;

        Vector3 startPos = card.position;
        Vector3 startScale = card.localScale;

        // 마나 이펙트
        if (manaGainEffectPrefab != null)
        {
            GameObject manaFx = Instantiate(manaGainEffectPrefab, startPos, Quaternion.identity);
            Destroy(manaFx, 1.5f);
        }

        // 위로 상승하며 사라짐
        float elapsed = 0f;
        while (elapsed < releaseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / releaseDuration;

            card.position = startPos + Vector3.up * (t * 100f);
            card.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        onComplete?.Invoke();
    }

    // ===== 유혹 애니메이션 =====

    /// <summary>
    /// 유혹 공격 애니메이션 (하트 이펙트)
    /// </summary>
    public IEnumerator AnimateSeduce(Vector3 sourcePosition, Vector3 targetPosition, int lustAmount)
    {
        if (heartEffectPrefab != null)
        {
            // 하트 생성
            GameObject heart = Instantiate(heartEffectPrefab, sourcePosition, Quaternion.identity);

            // 대상으로 이동
            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                heart.transform.position = Vector3.Lerp(sourcePosition, targetPosition, moveCurve.Evaluate(t));
                yield return null;
            }

            Destroy(heart);
        }

        yield return null;
    }
}

