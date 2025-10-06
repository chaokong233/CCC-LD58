using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("�ƶ�����")]
    public float moveSpeed = 5f;
    public float edgeBoundary = 10f; // ��Ļ��Ե�����ƶ������ؾ���
    public bool enableEdgeScrolling = true;

    [Header("�ƶ��߽�")]
    public float minX = -2f;
    public float maxX = 10f;
    public float minY = -2f;
    public float maxY = 10f;

    [Header("��������")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    [Header("ƽ���ƶ�")]
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

        // ��ʼ��Ŀ��λ�ú�����
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
    /// �����Ե�ƶ�
    /// </summary>
    void HandleEdgeMovement()
    {
        // ��ȡ���λ��
        Vector3 mousePos = Input.mousePosition;

        // ��ʼ���ƶ�����
        Vector3 moveDirection = Vector3.zero;

        // �������Ƿ�����Ļ��Ե
        if (enableEdgeScrolling)
        {
            if (mousePos.x <= edgeBoundary)
            {
                moveDirection.x = -1; // �����ƶ�
            }
            else if (mousePos.x >= Screen.width - edgeBoundary)
            {
                moveDirection.x = 1; // �����ƶ�
            }

            if (mousePos.y <= edgeBoundary)
            {
                moveDirection.y = -1; // �����ƶ�
            }
            else if (mousePos.y >= Screen.height - edgeBoundary)
            {
                moveDirection.y = 1; // �����ƶ�
            }
        }

        // ʹ�ü��̷������Ϊ���ÿ���
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            moveDirection.x = horizontal;
            moveDirection.y = vertical;
        }

        // ������ƶ����򣬸���Ŀ��λ��
        if (moveDirection != Vector3.zero)
        {
            Vector3 newPosition = targetPosition + moveDirection.normalized * moveSpeed * Time.deltaTime / Mathf.Max(1, Time.timeScale);

            // �����ƶ���Χ
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

            // �����ƽ���ƶ�������Ŀ��λ�ã�����ֱ������λ��
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
    /// ������������
    /// </summary>
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            if (cam.orthographic)
            {
                // ���������������
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
            else
            {
                // ͸������������ţ�ͨ��������Ұ��
                targetZoom -= scroll * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// Ӧ���ƶ�
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
    /// Ӧ������
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
    /// �����ƶ��߽�
    /// </summary>
    public void SetMovementBounds(float minX, float maxX, float minY, float maxY)
    {
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;

        // ȷ����ǰ�����λ���ڱ߽���
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        transform.position = clampedPosition;
        targetPosition = clampedPosition;
    }

    /// <summary>
    /// �������ŷ�Χ
    /// </summary>
    public void SetZoomRange(float minZoom, float maxZoom)
    {
        this.minZoom = minZoom;
        this.maxZoom = maxZoom;

        // ȷ����ǰ����ֵ�ڷ�Χ��
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
    /// �Զ�������ڵ�ͼ���ƶ��߽�
    /// </summary>
    public void CalculateBoundsFromMap(HexMapGenerator mapGenerator)
    {
        if (mapGenerator == null) return;

        // ��ȡ��ͼ�ߴ�
        int mapWidth = mapGenerator.mapWidth;
        int mapHeight = mapGenerator.mapHeight;

        // �����ͼ������߽�
        Vector3 centerPos = HexTile.HexToWorldPosition(mapWidth / 2, mapHeight / 2);
        Vector3 cornerPos = HexTile.HexToWorldPosition(mapWidth, mapHeight);

        // ���ñ߽磬����һЩ����
        float margin = 2f;
        minX = -margin;
        maxX = cornerPos.x + margin;
        minY = -margin;
        maxY = cornerPos.y + margin;

        // ���ó�ʼ�����λ��Ϊ��ͼ����
        Vector3 startPos = new Vector3(centerPos.x, centerPos.y, transform.position.z);
        transform.position = startPos;
        targetPosition = startPos;

        // ���ú��ʵ����ŷ�Χ
        minZoom = 2f;
        maxZoom = Mathf.Max(mapWidth, mapHeight) * 0.8f;

        Debug.Log($"������߽�����: X({minX:F1}, {maxX:F1}), Y({minY:F1}, {maxY:F1}), ����({minZoom:F1}, {maxZoom:F1})");
    }

    /// <summary>
    /// ��Scene��ͼ�л����ƶ��߽�
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // �����ƶ��߽�
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minX, minY, transform.position.z);
        Vector3 bottomRight = new Vector3(maxX, minY, transform.position.z);
        Vector3 topLeft = new Vector3(minX, maxY, transform.position.z);
        Vector3 topRight = new Vector3(maxX, maxY, transform.position.z);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // ���Ƶ�ǰ�����λ��
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}