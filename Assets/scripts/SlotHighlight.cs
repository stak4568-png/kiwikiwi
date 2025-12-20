using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬롯 하이라이트 컴포넌트
/// 드래그 중인 카드에 따라 슬롯을 하이라이트합니다.
/// </summary>
public class SlotHighlight : MonoBehaviour
{
    [Header("하이라이트 설정")]
    public Image highlightImage;
    public Color highlightColor = new Color(1f, 1f, 0.5f, 0.5f);
    public float fadeSpeed = 5f;

    private float currentAlpha = 0f;
    private float targetAlpha = 0f;

    void Awake()
    {
        // Image 컴포넌트가 없으면 자동 생성
        if (highlightImage == null)
        {
            highlightImage = GetComponent<Image>();
            if (highlightImage == null)
            {
                GameObject highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(transform);
                highlightObj.transform.localPosition = Vector3.zero;
                highlightObj.transform.localScale = Vector3.one;
                
                RectTransform rect = highlightObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                
                highlightImage = highlightObj.AddComponent<Image>();
                highlightImage.color = highlightColor;
                highlightImage.raycastTarget = false; // 레이캐스트 방해 안 함
            }
        }

        // 초기 상태: 투명
        if (highlightImage != null)
        {
            highlightImage.color = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
        }
    }

    void Update()
    {
        // 페이드 효과
        if (currentAlpha != targetAlpha)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            
            if (highlightImage != null)
            {
                Color color = highlightColor;
                color.a = currentAlpha;
                highlightImage.color = color;
            }
        }
    }

    /// <summary>
    /// 하이라이트 알파값 설정 (0~1)
    /// </summary>
    public void SetAlpha(float alpha)
    {
        targetAlpha = Mathf.Clamp01(alpha);
    }

    /// <summary>
    /// 하이라이트 즉시 표시
    /// </summary>
    public void Show()
    {
        targetAlpha = 1f;
        currentAlpha = 1f;
        if (highlightImage != null)
        {
            highlightImage.color = highlightColor;
        }
    }

    /// <summary>
    /// 하이라이트 즉시 숨김
    /// </summary>
    public void Hide()
    {
        targetAlpha = 0f;
        currentAlpha = 0f;
        if (highlightImage != null)
        {
            Color color = highlightColor;
            color.a = 0f;
            highlightImage.color = color;
        }
    }
}

