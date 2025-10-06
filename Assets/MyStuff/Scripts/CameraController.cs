using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float edgeBoundary = 10f; // 屏幕边缘触发移动的像素距离
    public bool enableEdgeScrolling = true;

    [Header("移动边界")]
    public float minX = -2f;
    public float maxX = 10f;
    public float minY = -2f;
    public float maxY = 10f;

    [Header("缩放设置")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    [Header("平滑移动")]
    public bool smoothMovement = true;
    public float smoothTime = 0.1f;

    private Camera cam;
    private Vector3 targetPosition;
    private float targetZoom;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        // 初始化目标位置和缩放
        targetPosition = transform.position;
        targetZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
    }

    void Update()
    {
        HandleEdgeMovement();
        HandleZoom();
        ApplyMovement();
        ApplyZoom();
    }

    /// <summary>
    /// 处理边缘移动
    /// </summary>
    void HandleEdgeMovement()
    {
        // 获取鼠标位置
        Vector3 mousePos = Input.mousePosition;

        // 初始化移动方向
        Vector3 moveDirection = Vector3.zero;

        // 检查鼠标是否在屏幕边缘
        if (enableEdgeScrolling)
        {
            if (mousePos.x <= edgeBoundary)
            {
                moveDirection.x = -1; // 向左移动
            }
            else if (mousePos.x >= Screen.width - edgeBoundary)
            {
                moveDirection.x = 1; // 向右移动
            }

            if (mousePos.y <= edgeBoundary)
            {
                moveDirection.y = -1; // 向下移动
            }
            else if (mousePos.y >= Screen.height - edgeBoundary)
            {
                moveDirection.y = 1; // 向上移动
            }
        }

        // 使用键盘方向键作为备用控制
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            moveDirection.x = horizontal;
            moveDirection.y = vertical;
        }

        // 如果有移动方向，更新目标位置
        if (moveDirection != Vector3.zero)
        {
            Vector3 newPosition = targetPosition + moveDirection.normalized * moveSpeed * Time.deltaTime / Mathf.Max(1, Time.timeScale);

            // 限制移动范围
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

            // 如果是平滑移动，设置目标位置；否则直接设置位置
            if (smoothMovement)
            {
                targetPosition = newPosition;
            }
            else
            {
                transform.position = newPosition;
                targetPosition = newPosition;
            }
        }
    }

    /// <summary>
    /// 处理缩放输入
    /// </summary>
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (cam.orthographic)
            {
                // 正交摄像机的缩放
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
            else
            {
                // 透视摄像机的缩放（通过调整视野）
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// 应用移动
    /// </summary>
    void ApplyMovement()
    {
        if (smoothMovement)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// 应用缩放
    /// </summary>
    void ApplyZoom()
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed / Mathf.Max(1, Time.timeScale));
        }
        else
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetZoom, Time.deltaTime * zoomSpeed / Mathf.Max(1,Time.timeScale));
        }
    }

    /// <summary>
    /// 设置移动边界
    /// </summary>
    public void SetMovementBounds(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;

        // 确保当前摄像机位置在边界内
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        transform.position = clampedPosition;
        targetPosition = clampedPosition;
    }

    /// <summary>
    /// 设置缩放范围
    /// </summary>
    public void SetZoomRange(float minZoom, float maxZoom)
    {
        this.minZoom = minZoom;
        this.maxZoom = maxZoom;

        // 确保当前缩放值在范围内
        if (cam.orthographic)
        {
            targetZoom = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            targetZoom = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// 自动计算基于地图的移动边界
    /// </summary>
    public void CalculateBoundsFromMap(HexMapGenerator mapGenerator)
    {
        if (mapGenerator == null) return;

        // 获取地图尺寸
        int mapWidth = mapGenerator.mapWidth;
        int mapHeight = mapGenerator.mapHeight;

        // 计算地图的世界边界
        Vector3 centerPos = HexTile.HexToWorldPosition(mapWidth / 2, mapHeight / 2);
        Vector3 cornerPos = HexTile.HexToWorldPosition(mapWidth, mapHeight);

        // 设置边界，留有一些余量
        float margin = 2f;
        minX = -margin;
        maxX = cornerPos.x + margin;
        minY = -margin;
        maxY = cornerPos.y + margin;

        // 设置初始摄像机位置为地图中心
        Vector3 startPos = new Vector3(centerPos.x, centerPos.y, transform.position.z);
        transform.position = startPos;
        targetPosition = startPos;

        // 设置合适的缩放范围
        minZoom = 2f;
        maxZoom = Mathf.Max(mapWidth, mapHeight) * 0.8f;

        Debug.Log($"摄像机边界设置: X({minX:F1}, {maxX:F1}), Y({minY:F1}, {maxY:F1}), 缩放({minZoom:F1}, {maxZoom:F1})");
    }

    /// <summary>
    /// 在Scene视图中绘制移动边界
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 绘制移动边界
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minX, minY, transform.position.z);
        Vector3 bottomRight = new Vector3(maxX, minY, transform.position.z);
        Vector3 topLeft = new Vector3(minX, maxY, transform.position.z);
        Vector3 topRight = new Vector3(maxX, maxY, transform.position.z);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // 绘制当前摄像机位置
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}