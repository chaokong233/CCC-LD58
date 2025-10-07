using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
    public GameObject AbilityPanel_normal;
    public Button AbilityButton_01;
    public Button AbilityButton_02;
    public Button AbilityButton_03;
    public Button AbilityButton_04;
    public List<TextMeshProUGUI> cd_texts = new List<TextMeshProUGUI>();
    public GameObject AbilityPanel_quell;
    public Button AbilityButton_05;
    public Button AbilityButton_06;
    [Header("   Pause")]
    public GameObject pausePanel;
    public Button returnToGameButton;
    public Button returnToMenuButton;
    public Slider speedSlider;
    public TextMeshProUGUI speedText;
    [Header("   GameSuccess")]
    public GameObject gameSuccessPanel;
    public Button continueButton;

    [Header("Music")]
    public AudioSource effect_AudioSource;
    //public AudioSource audioSource;
    public List<AudioResource> ability_audio;
    public AudioResource rebell_audio;
    public AudioResource recover_audio;
    public AudioResource warning_audio;
    public AudioResource click_audio;
    public AudioResource cancel_audio;
    public AudioResource unlock_audio;

    [Header("��������")]
    public float unlockCost = 100f;

    [Header("�������")]
    public Vector2 panelOffset = new Vector2(0, 80f); // ���ƫ����
    public float screenRLMargin = 20f; // ��Ļ�߾�
    public float screenUpMargin = 150f; // ��Ļ�߾�

    public HexTile currentSelectedTile;
    private GameManager gameManager;
    private HexMapGenerator mapGenerator;
    private Camera mainCamera;
    private RectTransform panelRectTransform;

    void Start()
    {
        mainCamera = Camera.main;
        gameManager = FindFirstObjectByType<GameManager>();
        mapGenerator = FindFirstObjectByType<HexMapGenerator>();
        effect_AudioSource = gameObject.AddComponent<AudioSource>();

        // �󶨰�ť�¼�
        unlockButton.onClick.AddListener(OnUnlockButtonClicked);

        AbilityButton_01.onClick.AddListener(OnAbilityButton_01_ButtonClicked);
        AbilityButton_02.onClick.AddListener(OnAbilityButton_02_ButtonClicked);
        AbilityButton_03.onClick.AddListener(OnAbilityButton_03_ButtonClicked);
        AbilityButton_04.onClick.AddListener(OnAbilityButton_04_ButtonClicked);
        AbilityButton_05.onClick.AddListener(OnAbilityButton_05_ButtonClicked);
        AbilityButton_06.onClick.AddListener(OnAbilityButton_06_ButtonClicked);

        returnToGameButton.onClick.AddListener(OnReturnToGameButtonClicked);
        returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClicked);

        continueButton.onClick.AddListener(OnGameSuccessContinueButtonClicked);

        // AbilityButton_01.

        // ��ȡ����RectTransform
        if (hexPanel != null)
            panelRectTransform = hexPanel.GetComponent<RectTransform>();

        // ��ʼ�������
        if (hexPanel != null)
            hexPanel.SetActive(false);

        // ��ʼ�������
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // ��ʼ�������
        if (gameSuccessPanel != null)
            gameSuccessPanel.SetActive(false);
    }

    void Update()
    {
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
            float currentTime = gameManager.currentTime;
            fundsText.text = $"�ʽ�: {gameManager.currentFunds:F0}\nʱ��: {currentTime/60:F0}min{currentTime%60:F1}s";
        }

        // ��ESC�ر����
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (hexPanel.activeSelf)
                HidePanel();
            else if (pausePanel.activeSelf)
            {
                ResumeGame();
            }
            else
                PauseGame();
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
            if(!currentSelectedTile.isObstacle())
                SetUnlockCost(mapGenerator.GetTileCostAtCoord(currentSelectedTile.q, currentSelectedTile.r));

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
        bool isNotRebel = !currentSelectedTile.isRebelContinent;
        bool isCalmedDown = currentSelectedTile.isCalmedDown;
        TileType tileType = currentSelectedTile.tileType;

        string tiletext = "";
        string rebelLable = isNotRebel ? "\n" : "(����)\n";
        string cityLable = isUnlocked ? "" : "�����������ƿصĵ���\n\n";
        string suburbLable = isUnlocked ? "" : "��Ը�ԣ�ĵ���\n\n";
        string ruralLable = isUnlocked ? "" : "��ʱ�������ĵ���\n\n";

        switch (currentSelectedTile.tileType)
        {
            case TileType.City:
                tiletext += "����"+ rebelLable + cityLable;
                break;
            case TileType.Suburb:
                tiletext += "����" + rebelLable + suburbLable;
                break;
            case TileType.Rural:
                tiletext += "ũ��" + rebelLable + ruralLable;
                break;
            case TileType.Mountain:
                tiletext += "ɽ��" + rebelLable + "����һ���ؿ��ϰ���";
                break;
            case TileType.Lake:
                tiletext += "����" + rebelLable + "����һ���ؿ��ϰ���";
                break;
        }

        // ���µؿ���Ϣ�ı�
        if (isUnlocked && isNotRebel)
        {
            tiletext += string.Format("������:{0:P1}\n", currentSelectedTile.currentCollectionRate)
                + string.Format("������:{0:P1}\n\n", currentSelectedTile.resistanceLevel)
                + string.Format("֧�ֶ�:{0:P1}\n", currentSelectedTile.supportLevel)
                + string.Format("�ܸ˶�:{0:P1}\n", currentSelectedTile.LeverageLevel)
                + string.Format("���϶�:{0:P1}\n", currentSelectedTile.unioLevel);
            tileInfoText.text = tiletext;
        }
        else
        {
            if(!isNotRebel)
            {
                tiletext += "�������������\n��Ǯ��ƽ����";
            }
            else if(!isUnlocked)
            {
                tiletext += $"��������:{currentSelectedTile.baseCollectionValue}\n����cd:{currentSelectedTile.currentCollectionCooldown}s";
            }
            tileInfoText.text = tiletext;
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

            bool isNotObstacle = !currentSelectedTile.isObstacle();

            if (hasUnlockedNeighbor && enoughFunds && isNotObstacle)
            {
                unlockButton.interactable = true;
                // ���³ɱ��ı�
                costText.text = $"��������:{unlockCost}";
            }
            else
            {
                unlockButton.interactable = false;
                if(!isNotObstacle)
                    costText.text = $"�����ɽ�����";
                else if (!hasUnlockedNeighbor)
                    costText.text = $"��������:{unlockCost}\n�������ڣ�";
                else if(!enoughFunds)
                    costText.text = $"��������:{unlockCost}\n���ʽ��㣩";
            }         
        }

        // ���¼������
        if (isUnlocked && isNotRebel)
        {
            AbilityPanel_normal.SetActive(true);
            float current_cd = currentSelectedTile.currentDebtCollectionMethodCooldown;
            bool isAbilityAvaible = current_cd <= 0;
            if(isAbilityAvaible)
            {
                ShowCdText(false);
                AbilityButton_01.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Gentle];
                AbilityButton_02.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Legal];
                AbilityButton_03.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Quell];
                AbilityButton_04.interactable = gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)DebtCollectionMethod.Violent];
            }
            else
            {
                ShowCdText(true, current_cd);
                AbilityButton_01.interactable = false;
                AbilityButton_02.interactable = false;
                AbilityButton_03.interactable = false;
                AbilityButton_04.interactable = false;
            }
        }
        else
        {
            AbilityPanel_normal.SetActive(false);
            if(!isNotRebel)
            {
                AbilityPanel_quell.SetActive(true);

                AbilityButton_05.interactable = !isCalmedDown && gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)QuellMethod.CalmDown];
                AbilityButton_06.interactable = isCalmedDown && gameManager.currentFunds >= HexTile.abilityCost[(int)tileType, (int)QuellMethod.Permeation];
            }
            else
            {
                AbilityPanel_quell.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ��Ӱ�ť��ʾ
    /// </summary>
    private void AddToolkit(Button button, GameObject toolkitPanel)
    {
        //button.OnPointerEnter.
        //button.onPointerExit.AddListener(OnPointerExit);
        //button.onPointerEnter.AddListener((date) => { OnPointerEnter(toolkitPanel); });
        //button.onPointerExit.AddListener(OnPointerExit);
    }

    /// <summary>
    /// cd_text
    /// </summary>
    private void ShowCdText(bool show_or_hide, float cd = 0f)
    {
        foreach (var text in cd_texts)
        {
            text.text = show_or_hide ? $"{cd:F1}s" : "";
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
        PlaySound(ability_audio[0]);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_02_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Legal);
        PlaySound(ability_audio[1]);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_03_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Quell);
        PlaySound(ability_audio[2]);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_04_ButtonClicked()
    {
        currentSelectedTile.ExecuteDebtCollection(DebtCollectionMethod.Violent);
        PlaySound(ability_audio[3]);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_05_ButtonClicked()
    {
        currentSelectedTile.ExecuteQuell(QuellMethod.CalmDown);
        PlaySound(ability_audio[4]);
    }

    /// <summary>
    /// ���ܰ�ť����¼�
    /// </summary>
    private void OnAbilityButton_06_ButtonClicked()
    {
        currentSelectedTile.ExecuteQuell(QuellMethod.Permeation);
        PlaySound(ability_audio[5]);  
    }

    public void PlaySound(AudioResource resource)
    {
        effect_AudioSource.resource = resource;
        effect_AudioSource.Play();
    }

    /// <summary>
    /// ���ý����ɱ�
    /// </summary>
    public void SetUnlockCost(float cost)
    {
        unlockCost = cost;
        UpdatePanelInfo();
    }


    /// <summary>
    /// ��ͣ��Ϸ
    /// </summary>
    private void PauseGame()
    {
        OnPlayClickSound();
        pausePanel.SetActive(true);
        Time.timeScale = 0;
    }

    /// <summary>
    /// �ָ���Ϸ
    /// </summary>
    private void ResumeGame()
    {
        OnPlayCancelSound();
        pausePanel.SetActive(false);
        Time.timeScale = speedSlider.value;
    }

    /// <summary>
    /// ���ٻ���
    /// </summary>
    public void OnSpeedSliderValueChange()
    {
        speedText.text = $"{speedSlider.value}x";
    }

    /// <summary>
    /// ��ť����¼�
    /// </summary>
    private void OnReturnToGameButtonClicked()
    {
        ResumeGame();
    }

    /// <summary>
    /// ��ť����¼�
    /// </summary>
    private void OnReturnToMenuButtonClicked()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// ��Ϸʤ��
    /// </summary>
    public void OnGameSuccess()
    {
        OnPlayClickSound();
        gameSuccessPanel.SetActive(true);
        Time.timeScale = 0;
    }

    public void OnTileRebell()
    {
        PlaySound(rebell_audio);
    }

    public void OnTileRecover()
    {
        PlaySound(recover_audio);
    }

    public void OnTileWanring()
    {
        PlaySound(warning_audio);
    }

    public void OnPlayClickSound()
    {
        PlaySound(click_audio);
    }

    public void OnPlayCancelSound()
    {
        PlaySound(cancel_audio);
    }

    public void OnUnlockTile()
    {
        PlaySound(unlock_audio);
    }

    /// <summary>
    /// ��ť����¼�
    /// </summary>
    private void OnGameSuccessContinueButtonClicked()
    {
        gameSuccessPanel.SetActive(false);
        Time.timeScale = speedSlider.value;
    }

}