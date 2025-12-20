using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 보드 관리 클래스
/// 마우스 위치를 보드 좌표로 변환하는 기능 제공
/// </summary>
public class GameBoard : MonoBehaviour
{
    public static GameBoard instance;
    
    [Header("보드 참조")]
    public RectTransform boardRect;
    
    private Canvas canvas;
    private Camera uiCamera;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Canvas 찾기
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        // UI Camera 찾기
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = canvas.worldCamera;
        }
        else if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            uiCamera = Camera.main;
        }

        // boardRect가 설정되지 않았으면 자동으로 찾기
        if (boardRect == null)
        {
            boardRect = GetComponent<RectTransform>();
            if (boardRect == null && GameManager.instance != null && GameManager.instance.playerField != null)
            {
                boardRect = GameManager.instance.playerField.GetComponent<RectTransform>();
            }
        }
    }

    /// <summary>
    /// 싱글톤 인스턴스 가져오기
    /// </summary>
    public static GameBoard Get()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("GameBoard");
            instance = go.AddComponent<GameBoard>();
        }
        return instance;
    }

    /// <summary>
    /// 마우스 위치를 보드 좌표로 레이캐스트하여 변환
    /// </summary>
    public Vector3 RaycastMouseBoard()
    {
        if (boardRect == null)
        {
            Debug.LogWarning("[GameBoard] boardRect가 설정되지 않았습니다.");
            return Vector3.zero;
        }

        Vector2 mousePosition;
        
        // New Input System 지원
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
        else
        {
            mousePosition = Input.mousePosition;
        }

        // RectTransformUtility를 사용하여 마우스 위치를 보드 좌표로 변환
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardRect, 
            mousePosition, 
            uiCamera, 
            out localPoint))
        {
            // 보드의 월드 좌표로 변환
            return boardRect.TransformPoint(localPoint);
        }

        // 변환 실패 시 마우스 위치를 그대로 반환 (스크린 좌표)
        return mousePosition;
    }
}

