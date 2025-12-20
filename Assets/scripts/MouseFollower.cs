using UnityEngine;

/// <summary>
/// 마우스를 따라다니는 UI 요소
/// GameBoard를 사용하여 보드 좌표로 마우스 위치를 추적
/// </summary>
public class MouseFollower : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("활성화 여부")]
    public bool visible = false;
    
    [Tooltip("부드러운 이동 사용 여부")]
    public bool useSmoothing = false;
    
    [Tooltip("부드러운 이동 속도 (0~1, 높을수록 빠름)")]
    [Range(0f, 1f)]
    public float smoothingSpeed = 0.2f;
    
    [Tooltip("오프셋 (마우스 위치에서의 상대 위치)")]
    public Vector3 offset = Vector3.zero;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (visible)
        {
            Vector3 dest = GameBoard.Get().RaycastMouseBoard();
            dest += offset;
            
            if (useSmoothing)
            {
                transform.position = Vector3.Lerp(transform.position, dest, smoothingSpeed);
            }
            else
            {
                transform.position = dest;
            }
        }
    }

    /// <summary>
    /// 표시/숨김 제어
    /// </summary>
    public void SetVisible(bool value)
    {
        visible = value;
        if (gameObject != null)
        {
            gameObject.SetActive(value);
        }
    }

    /// <summary>
    /// 즉시 마우스 위치로 이동
    /// </summary>
    public void SnapToMouse()
    {
        if (GameBoard.Get() != null)
        {
            Vector3 dest = GameBoard.Get().RaycastMouseBoard();
            transform.position = dest + offset;
        }
    }
}

