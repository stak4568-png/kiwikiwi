using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 필드 비주얼 관리자
/// 컨셉: "기억의 거울" - 카드는 거울 속에서 기억의 파편으로 나타남
/// </summary>
public class FieldVisualManager : MonoBehaviour
{
    public static FieldVisualManager instance;

    [Header("배경")]
    public Image fieldBackground;
    public Sprite mirrorBackgroundSprite;
    public Color backgroundTint = new Color(0.1f, 0.1f, 0.2f, 1f);

    [Header("파문 효과")]
    public GameObject ambientRipplePrefab;
    public float rippleInterval = 3f;
    public float rippleRandomRange = 200f;

    [Header("파편 효과")]
    public GameObject shardParticlePrefab;
    public int shardCount = 5;

    [Header("분위기 연출")]
    public ParticleSystem floatingParticles;
    public float particleIntensity = 1f;

    [Header("균열 효과 (경계선)")]
    public Image crackOverlay;
    public float crackPulseSpeed = 0.5f;
    public float crackPulseAmount = 0.1f;

    [Header("슬롯 하이라이트")]
    public Color emptySlotGlow = new Color(0.5f, 0.7f, 1f, 0.3f);
    public Color occupiedSlotGlow = new Color(0.8f, 0.5f, 1f, 0.5f);

    [Header("성능 최적화")]
    [Tooltip("동시에 존재할 수 있는 최대 파문 개수")]
    public int maxActiveRipples = 5;
    [Tooltip("균열 펄스 업데이트 간격 (초)")]
    public float crackPulseUpdateInterval = 0.05f;

    // [제거됨] CardSlotIndicator - FieldSlotManager로 대체됨

    private Coroutine _rippleCoroutine;
    private Coroutine _crackPulseCoroutine;
    private int _activeRippleCount = 0;  // 현재 활성 파문 개수 추적

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeVisuals();
        StartAmbientEffects();
    }

    void InitializeVisuals()
    {
        // 배경 설정
        if (fieldBackground != null)
        {
            if (mirrorBackgroundSprite != null)
                fieldBackground.sprite = mirrorBackgroundSprite;
            fieldBackground.color = backgroundTint;
        }

        // 떠다니는 파티클 시작
        if (floatingParticles != null)
        {
            var emission = floatingParticles.emission;
            emission.rateOverTime = 10f * particleIntensity;
            floatingParticles.Play();
        }
    }

    void StartAmbientEffects()
    {
        // 주기적 파문
        if (ambientRipplePrefab != null)
        {
            _rippleCoroutine = StartCoroutine(AmbientRippleLoop());
        }

        // 균열 펄스
        if (crackOverlay != null)
        {
            _crackPulseCoroutine = StartCoroutine(CrackPulseLoop());
        }
    }

    /// <summary>
    /// 주기적으로 배경에 파문 생성
    /// </summary>
    IEnumerator AmbientRippleLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(rippleInterval + Random.Range(-1f, 1f));

            // 랜덤 위치에 파문
            Vector3 randomPos = new Vector3(
                Random.Range(-rippleRandomRange, rippleRandomRange),
                Random.Range(-rippleRandomRange, rippleRandomRange),
                0
            );

            SpawnRipple(transform.position + randomPos, RippleType.Ambient);
        }
    }

    /// <summary>
    /// 균열 오버레이 펄스 효과 (최적화: 매 프레임 대신 간격 업데이트)
    /// </summary>
    IEnumerator CrackPulseLoop()
    {
        Color baseColor = crackOverlay.color;
        float baseAlpha = baseColor.a;
        WaitForSeconds wait = new WaitForSeconds(crackPulseUpdateInterval);

        while (true)
        {
            float pulse = Mathf.Sin(Time.time * crackPulseSpeed * Mathf.PI * 2) * crackPulseAmount;
            Color newColor = baseColor;
            newColor.a = baseAlpha + pulse;
            crackOverlay.color = newColor;
            yield return wait;  // 최적화: 매 프레임 대신 간격마다 업데이트
        }
    }

    // ===== 이벤트 연동 =====

    /// <summary>
    /// 카드 소환 시 호출
    /// </summary>
    public void OnCardSummoned(Vector3 position, bool isPlayerCard = true)
    {
        SpawnRipple(position, RippleType.Summon);
        
        // FieldSlotManager는 자동으로 관리하므로 별도 업데이트 불필요
    }

    /// <summary>
    /// 카드 파괴 시 호출
    /// </summary>
    public void OnCardDestroyed(Vector3 position, bool isPlayerCard)
    {
        SpawnShatterEffect(position);
        
        // FieldSlotManager는 자동으로 관리하므로 별도 업데이트 불필요
    }

    /// <summary>
    /// 유혹 공격 시 호출
    /// </summary>
    public void OnSeduceAttack(Vector3 sourcePos, Vector3 targetPos)
    {
        // 하트 파티클 또는 특수 효과
        SpawnRipple(targetPos, RippleType.Seduce);
    }

    /// <summary>
    /// 턴 변경 시 호출
    /// </summary>
    public void OnTurnChanged(bool isEnemyTurn)
    {
        // 배경색 미세 변화
        if (fieldBackground != null)
        {
            Color targetColor = isEnemyTurn
                ? new Color(0.2f, 0.1f, 0.15f, 1f)  // 적 턴: 붉은 톤
                : backgroundTint;                    // 플레이어 턴: 기본

            StartCoroutine(FadeBackgroundColor(targetColor, 0.5f));
        }
    }

    // ===== 이펙트 생성 =====

    public enum RippleType { Ambient, Summon, Seduce, Damage }

    void SpawnRipple(Vector3 position, RippleType type)
    {
        if (ambientRipplePrefab == null) return;

        // 성능 최적화: Ambient 타입은 개수 제한 적용
        if (type == RippleType.Ambient && _activeRippleCount >= maxActiveRipples)
        {
            return;  // 최대 개수 초과 시 생성하지 않음
        }

        GameObject ripple = Instantiate(ambientRipplePrefab, position, Quaternion.identity, transform);

        // 타입별 크기/색상 조절
        float scale = type switch
        {
            RippleType.Summon => 1.5f,
            RippleType.Seduce => 1.2f,
            RippleType.Damage => 0.8f,
            _ => 1f
        };

        ripple.transform.localScale = Vector3.one * scale;

        // 파문 개수 추적 및 자동 제거
        if (type == RippleType.Ambient)
        {
            _activeRippleCount++;
            StartCoroutine(DestroyRippleAfterDelay(ripple, 2f));
        }
        else
        {
            Destroy(ripple, 2f);
        }
    }

    /// <summary>
    /// 파문 제거 및 카운터 감소 (성능 최적화)
    /// </summary>
    IEnumerator DestroyRippleAfterDelay(GameObject ripple, float delay)
    {
        yield return new WaitForSeconds(delay);
        _activeRippleCount--;
        if (ripple != null) Destroy(ripple);
    }

    void SpawnShatterEffect(Vector3 position)
    {
        if (shardParticlePrefab == null) return;

        for (int i = 0; i < shardCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-30f, 30f),
                Random.Range(-30f, 30f),
                0
            );

            GameObject shard = Instantiate(shardParticlePrefab, position + offset, Quaternion.identity, transform);

            // 파편 날아가는 애니메이션
            StartCoroutine(AnimateShard(shard.transform, position + offset));
        }
    }

    IEnumerator AnimateShard(Transform shard, Vector3 startPos)
    {
        if (shard == null) yield break;

        Vector3 direction = (startPos - transform.position).normalized + new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0
        );

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            shard.position = startPos + direction * (t * 100f);
            shard.localScale = Vector3.one * (1 - t);
            shard.Rotate(0, 0, Time.deltaTime * 360f);

            yield return null;
        }

        Destroy(shard.gameObject);
    }

    IEnumerator FadeBackgroundColor(Color targetColor, float duration)
    {
        if (fieldBackground == null) yield break;

        Color startColor = fieldBackground.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fieldBackground.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }

        fieldBackground.color = targetColor;
    }

    void OnDestroy()
    {
        if (_rippleCoroutine != null) StopCoroutine(_rippleCoroutine);
        if (_crackPulseCoroutine != null) StopCoroutine(_crackPulseCoroutine);
    }
}

