using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HexUIController : MonoBehaviour
{
    [Header("UI����")]
    [Header("   Panel")]
    public GameObject hexPanel;
    public Button unlockButton;
    public TextMeshProUGUI tileInfoText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI fundsText;
    [Header("   Ability")]
    public GameObject AbilityPanel;
    public Button AbilityButton_01;
    public Button AbilityButton_02;
    public Button AbilityButton_03;

    [Header("��������")]
    public float unlockCost = 100f;

    [Header("�������")]
    public Vector2 panelOffset = new Vector2(0, 80f); // ���ƫ����
    public float screenRLMargin = 20f; // ��Ļ�߾�
    public float screenUpMargin = 150f; // ��Ļ�߾�

    private HexTile currentSelectedTile;
    private GameManager gameManager;
    private Camera mainCamera;
    private RectTransform panelRectTransform;

    private float currentTime_ = 0;

    void Start()
    {
        currentTime_ = 0;
        mainCamera = Camera.main;
        gameManager = FindFirstObjectByType<GameManager>();

        // �󶨰�ť�¼�
        unlockButton.onClick.AddListener(OnUnlockButtonClicked);
        AbilityButton_01.onClick.AddListener(OnAbilityButton_01_ButtonClicked);
        AbilityButton_02.onClick.AddListener(OnAbilityButton_02_ButtonClicked);
        AbilityButton_03.onClick.AddListener(OnAbilityButton_03_ButtonClicked);
       
        // ��ȡ����RectTransform
        if (hexPanel != null)
            panelRectTransform = hexPanel.GetComponent<RectTransform>();

        // ��ʼ�������
        if (hexPanel != null)
            hexPanel.SetActive(false);
    }

    void Update()
    {
        currentTime_ += Time.deltaTime;

        // �������� // ����Ƿ�����UI��
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleTileClick();    
        }

        // ������ţ�����Panel Info
        if (hexPanel.activeSelf)
            UpdatePanelInfo();

        // �����ʽ���ʾ
        if (gameManager != null && fundsText != null)
        {
            fundsText.text = $"Fund: {gameManager.currentFunds:F0}\nTime: {currentTime_/60:F0}min{currentTime_%60:F1}s";
        }

        // ��ESC�ر����
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HidePanel();
        }
    }

    /// <summary>
    /// ����ؿ���
    /// </summary>
    void HandleTileClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null)
        {
            HexTile tile = hit.collider.GetComponent<HexTile>();
            if (tile != null)
            {
                ShowUnlockPanel(tile);
                return;
            }
        }

        // ����հ״��������
        HidePanel();
    }

    /// <summary>
    /// ��ʾ�������
    /// </summary>
    void ShowUnlockPanel(HexTile tile)
    {
        currentSelectedTile = tile;

        if (hexPanel != null)
        {
            hexPanel.SetActive(true);
            SetUnlockCost(currentSelectedTile.debtCost);

            // �������λ�ã�ȷ������Ļ�ڣ�
            UpdatePanelPosition(tile.transform.position);

            // ����UI��Ϣ
            UpdatePanelInfo();
        }
    }

    /// <summary>
    /// �������λ�ã�ȷ������Ļ��Χ��
    /// </summary>
    void UpdatePanelPosition(Vector3 worldPosition)
    {
        if (panelRectTransform == null) return;

        // ����������ת��Ϊ��Ļ����
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

        // Ӧ��ƫ����
        screenPos += (Vector3)panelOffset;

        // ��ȡ���ߴ�
        float panelWidth = panelRectTransform.rect.width;
        float panelHeight = panelRectTransform.rect.height;

        // �������߽�
        float minX = panelWidth / 2 + screenRLMargin;
        float maxX = Screen.width - panelWidth / 2 - screenRLMargin;
        float minY = panelHeight / 2;
        float maxY = Screen.height - panelHeight / 2 - screenUpMargin;

        // �����������Ļ��Χ��
        screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
        screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

        // �������λ��
        hexPanel.transform.position = screenPos;

        // ��ѡ��������λ�ñ��������������һ����ͷָ��ؿ�
        // ����������һ����ͷָ��ؿ���Ӿ�ָʾ��
    }

    /// <summary>
    /// �������
    /// </summary>
    public void HidePanel()
    {
        if (hexPanel != null)
            hexPanel.SetActive(false);

        currentSelectedTile = null;
    }

    /// <summary>
    /// ���������Ϣ
    /// </summary>
    void UpdatePanelInfo()
    {
        if (gameManager == null || currentSelectedTile == null) return;

        bool isUnlocked = currentSelectedTile.isUnlocked;

        // ���µؿ���Ϣ�ı�
        if (isUnlocked)
        {
            tileInfoText.text = $"�ؿ� {currentSelectedTile.tileName}\n"
                + string.Format("��ǰ������:{0:P1}\n", currentSelectedTile.currentCollectionRate)
                + string.Format("��Թֵ:{0:F2}\n", currentSelectedTile.resistanceLevel)
                + string.Format("֧�ֶ�:{0:F2}\n", currentSelectedTile.supportLevel);
        }
        else
        {
            tileInfoText.text = $"�ؿ� {currentSelectedTile.tileName}\n";
        }

        // �ж��Ƿ�ɽ�������������ʾ
        if (isUnlocked)
        {
            unlockButton.interactable = false;
            costText.text = "�ѽ���";
        }
        else
        {
            bool hasUnlockedNeighbor = false;
            foreach (var neighbor in currentSelectedTile.neighbors)
            {
                if (neighbor.isUnlocked)
                {
                    hasUnlockedNeighbor = true;
                    break;
                }
            }

            bool enoughFunds = gameManager.currentFunds >= unlockCost;

            if(hasUnlockedNeighbor && enoughFunds)
            {
                unlockButton.interactable = true;
                // ���³ɱ��ı�
                costText.text = $"�����ɱ�:{unlockCost}";
            }
            else
            {
                unlockButton.interactable = false;
                if (!hasUnlockedNeighbor)
                    costText.text = $"�����ɱ�:{unlockCost}(������)";
                else if(!enoughFunds)
                    costText.text = $"�����ɱ�:{unlockCost}(�ʽ���)";
            }         
        }

        // ���¼������
        if (isUnlocked)
        {
            AbilityPanel.SetActive(true);
            bool isAbilityAvaible = currentSelectedTile.currentDebtCollectionMethodCooldown <= 0;
            if(isAbilityAvaible)
            {
                AbilityButton_01.interactable = true;
                AbilityButton_02.interactable = true;
                AbilityButton_03.interactable = true;
            }
            else
            {
                AbilityButton_01.interactable = false;
                AbilityButton_02.interactable = false;
                AbilityButton_03.interactable = false;
            }
        }
        else
        {
            AbilityPanel.SetActive(false);
        }
    }

    /// <summary>
    /// ������ť����¼�
    /// </summary>
    private void OnUnlockButtonClicked()
    {
        Debug.Log("ClickButton");
        if (currentSelectedTile != null && gameManager != null)
        {
            // ���Խ����ؿ�
            if (gameManager.UnlockTile(currentSelectedTile.q, currentSelectedTile.r, unlockCost))
            {
                HidePanel();
            }
            else
            {
                Debug.LogWarning("����ʧ�ܣ��ʽ����ؿ��ѽ���");
                // ������������ӽ���ʧ�ܵķ��������簴ť��������ɫ��˸�ȣ�
            }
        } 
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_01_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Gentle);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_02_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Legal);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_03_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Violent);
    }

    /// <summary>
    /// ���ý����ɱ�
    /// </summary>
    public void SetUnlockCost(float cost)
    {
        unlockCost = cost;
        UpdatePanelInfo();
    }
}