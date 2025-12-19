using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem; // ★ 새로운 입력 시스템을 사용하기 위해 추가

public class CombatArrow : MonoBehaviour
{
    public static CombatArrow instance;

    public RectTransform arrowhead;
    public RectTransform dotsContainer;
    private List<RectTransform> dots = new List<RectTransform>();

    private Vector2 startPoint;
    public float curveHeight = 100f;

    void Awake()
    {
        if (instance == null) instance = this;

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
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        // ★ Old: Input.mousePosition
        // ★ New: Mouse.current.position.ReadValue()
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            UpdateBezierArrow(mousePos);
        }
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

            float scale = Mathf.Lerp(0.5f, 1.0f, t);
            dots[i].localScale = new Vector3(scale, scale, 1f);
        }

        arrowhead.position = endPoint;

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