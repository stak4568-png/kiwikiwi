using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem; // New Input System 사용 시 필수

public class CombatArrow : MonoBehaviour
{
    public static CombatArrow instance;

    public RectTransform arrowhead;      // 화살표 머리
    public RectTransform dotsContainer;  // 점들을 담은 부모
    private List<RectTransform> dots = new List<RectTransform>();

    private Vector2 startPoint;
    public float curveHeight = 100f;

    void Awake()
    {
        if (instance == null) instance = this;

        // 점들 초기화 및 클릭 방지(Raycast Target OFF)
        foreach (RectTransform child in dotsContainer)
        {
            dots.Add(child);
            if (child.GetComponent<Image>()) child.GetComponent<Image>().raycastTarget = false;
        }
        if (arrowhead.GetComponent<Image>()) arrowhead.GetComponent<Image>().raycastTarget = false;

        gameObject.SetActive(false);
    }

    public void Show(Vector2 startPos)
    {
        gameObject.SetActive(true);
        startPoint = startPos;
        // 화살표가 모든 UI의 최상단에 보이도록 설정
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        // 마우스 현재 위치 가져오기 (New Input System 방식)
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else
        {
            mousePos = Input.mousePosition; // 구형 시스템 백업
        }

        UpdateBezierArrow(mousePos);
    }

    void UpdateBezierArrow(Vector2 endPoint)
    {
        Vector2 midPoint = Vector2.Lerp(startPoint, endPoint, 0.5f);
        Vector2 controlPoint = midPoint + Vector2.up * curveHeight;

        for (int i = 0; i < dots.Count; i++)
        {
            float t = (float)(i + 1) / (dots.Count + 1);
            Vector2 pos = GetBezierPoint(startPoint, controlPoint, endPoint, t);
            dots[i].position = pos;

            float scale = Mathf.Lerp(0.4f, 0.8f, t);
            dots[i].localScale = new Vector3(scale, scale, 1f);
        }

        arrowhead.position = endPoint;

        // 머리 회전 각도 계산
        Vector2 lastDotPos = GetBezierPoint(startPoint, controlPoint, endPoint, 0.95f);
        Vector2 direction = endPoint - lastDotPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowhead.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    Vector2 GetBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }
}