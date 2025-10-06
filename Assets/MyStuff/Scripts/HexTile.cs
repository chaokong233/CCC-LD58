using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum DebtCollectionMethod
{
    None=0,
    Gentle=1,     // �ºʹ�ծ
    Legal=2,      // ���ɴ�ծ
    Quell=3,      // ƽϢ
    Violent=4     // ������ծ
}

public enum QuellMethod
{
    CalmDown = 5,     // 
    Permeation = 6,      // 
}

public enum TileType
{
    Default = 0,
    City = 1,       // ����
    Suburb = 2,     // ����  
    Rural = 3,      // ũ��
    Lake = 4,       // �������ϰ���
    Mountain = 5,   // ɽ�أ��ϰ���
    MaxNum = Mountain
}

public class HexTile : MonoBehaviour
{
    [Header("�ؿ������Ϣ")]
    public int q; // ��������������Q
    public int r; // ��������������R
    public string tileName;
    public bool isUnlocked = false;
    public bool isRebelContinent = false;
    public bool isCalmedDown = false;

    [Header("Gameplay����")]
    public float debtCost = 100.0f; // ���ν���۸�

    public float collectionCooldown = 8.0f; // ����CD
    private float currentCollectionCooldown = 8.0f; // ��ǰCD

    public float baseCollectionValue = 15.0f; // ��������ֵ
    public float currentCollectionRate = 1.0f; // ��ǰ������ 0-1
    public float baseCollectionRate = 0.8f; // ���������� 0-1
    public float targetCollectionRate = 1f; // Ŀ�������� 0-1

    public float resistanceLevel = 0.3f; // ������ 0-1
    public float supportLevel = 0.1f; // ֧�ֶ� 0-1
    public float supportLevel_temp = 0.1f; // ���������Ӱ��ǰ��֧�ֶ�
    public float unioLevel = 0.0f; // ����� 0-1(�ý����ٷֱȴ���)
    public float LeverageLevel = 0.0f; // �ܸ�ֵ 0-1

    public float resistanceLevelGrowth = -1f / 180f; // ��������Ȼ����
    public float baseResistanceLevelGrowth = -1f / 180f; // ��ʼ��������Ȼ����
    public float regionOriginFuns = 1000.0f; // ����ԭʼ�ʽ�
    public float totalGain = 0f; // �������ʽ�

    public float DebtCollectionMethodCooldown = 30.0f; // ��ծ��ʽCD
    public float currentDebtCollectionMethodCooldown = 0.0f; // ��ǰCD

    private GameManager gameManager_;
    private HexMapGenerator mapGenerator_;
    private FloatingTextController floatingTextController_;

    [Header("���ڵؿ�")]
    public List<HexTile> neighbors = new List<HexTile>();

    [Header("���ӻ����")]
    public SpriteRenderer spriteRenderer;
    public Color unlockedColor = Color.white;
    public Color cityUnlockedColor = Color.white;
    public Color suburbUnlockedColor = Color.white;
    public Color ruralUnlockedColor = Color.white;
    public Color lakeUnlockedColor = Color.white;
    public Color mountainUnlockedColor = Color.white;

    public Color resistanceColor = Color.red;
    public Color lockedColor = Color.gray;

    [Header("TileType")]
    public TileType tileType = TileType.City;

    [Header(    "0.default 1.City 2.Suburb 3.Rural 4.Lake 5.Mountain")]
    public Sprite[] TileTypeSprites = {};

    // �����η������� (Q, R ����)������qΪż����tile
    private static readonly Vector2Int[] hexDirections_01 =
    {
        new Vector2Int(0, 1),   // ��
        new Vector2Int(1, 1),   // ����
        new Vector2Int(1, 0),   // ����
        new Vector2Int(0, -1),  // ��
        new Vector2Int(-1, 1),  // ����
        new Vector2Int(-1, 0)   // ����
    };

    // �����η������� (Q, R ����)������qΪ������tile
    private static readonly Vector2Int[] hexDirections =
    {
        new Vector2Int(0, 1),    // ��
        new Vector2Int(1, 0),    // ����
        new Vector2Int(1, -1),   // ����
        new Vector2Int(0, -1),   // ��
        new Vector2Int(-1, 0),   // ����
        new Vector2Int(-1, -1)   // ����
    };

    // ��������, ��һ��������tileType�ţ��ڶ��������Ǽ��ܺ�
    public static readonly float[,] abilityCost = new float[4,7]
    {
        {0,0,0,0,0,0,0},
        {0,     5f,25f,20f,0f,      150f,300f},
        {0,     5f,25f,20f,0f,      150f,300f},
        {0,     5f,25f,20f,0f,      150f,300f}
    };

    private static readonly float collectionRestitutionFactor = 1f / (3f * 8.0f);
    
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (gameManager_ == null)
            gameManager_ = FindFirstObjectByType<GameManager>();
        if (mapGenerator_ == null)
            mapGenerator_ = FindFirstObjectByType<HexMapGenerator>();
        if (floatingTextController_ == null)
            floatingTextController_ = FindFirstObjectByType<FloatingTextController>();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if(isUnlocked)
        {
            if(!isRebelContinent)
            {
                if (currentDebtCollectionMethodCooldown > 0)
                    currentDebtCollectionMethodCooldown -= Time.deltaTime;
                UpdateAttribute();
                CalculateProduct();
            }
            else
            {
                if(!isCalmedDown)
                    UpdateRebelContinent();
                currentDebtCollectionMethodCooldown = 0;
            }

            UpdateVisuals();
        }
    }

    /// <summary>
    /// ��ʼ���ؿ�
    /// </summary>
    public void Initialize(int qCoord, int rCoord, string name = "")
    {
        q = qCoord;
        r = rCoord;
        tileName = string.IsNullOrEmpty(name) ? $"Tile_{q}_{r}" : name;

        // ����λ��
        Vector3 worldPos = HexToWorldPosition(q, r);
        transform.position = worldPos;
    }

    /// <summary>
    /// ��ʼ���ؿ�����
    /// </summary>
    public void InitializeType(TileType type)
    {
        tileType = type;

        // Update Sprite
        int len = TileTypeSprites.Length;
        int idx = (int)type;
        if (idx <= len - 1)
            spriteRenderer.sprite = TileTypeSprites[idx];
        else if (len > 0)
            spriteRenderer.sprite = TileTypeSprites[0];

        // Update Color
        switch (type)
        {
            case TileType.City:
                unlockedColor = cityUnlockedColor;
                break;
            case TileType.Rural:
                unlockedColor = ruralUnlockedColor;
                break;
            case TileType.Suburb:
                unlockedColor = suburbUnlockedColor;
                break;
            case TileType.Mountain:
                unlockedColor = mountainUnlockedColor;
                break;
            case TileType.Lake:
                unlockedColor = lakeUnlockedColor;
                break;
        }
    }

    public bool isObstacle()
    {
        return tileType == TileType.Lake || tileType == TileType.Mountain;
    }

    /// <summary>
    /// �����ؿ�
    /// </summary>
    public void UnlockTile()
    {
        if (!isUnlocked)
        {
            isUnlocked = true;
            UpdateVisuals();
            Debug.Log($"�ؿ� {tileName} �ѽ���");
        }
    }

    /// <summary>
    /// ִ�д�ծ
    /// </summary>
    public void ExecuteDebtCollection(DebtCollectionMethod method)
    {
        if (!isUnlocked || currentDebtCollectionMethodCooldown > 0) return;

        currentDebtCollectionMethodCooldown = DebtCollectionMethodCooldown; // ��ȴ

        gameManager_.Spending(abilityCost[(int)tileType,(int)method]); // Cost

        switch (method)
        {
            //case DebtCollectionMethod.Violent:
            //    baseCollection = 0.7f;
            //    resistanceChange = 0.4f;
            //    break;
            //case DebtCollectionMethod.Gentle:
            //    baseCollection = 0.3f;
            //    resistanceChange = -0.1f;
            //    break;
            //case DebtCollectionMethod.Legal:
            //    baseCollection = 0.5f;
            //    resistanceChange = 0.1f;
            //    break;
            case DebtCollectionMethod.Gentle:
                
                break;
            case DebtCollectionMethod.Legal:

                break;
            case DebtCollectionMethod.Quell:

                break;
            case DebtCollectionMethod.Violent:

                resistanceLevel += 1f;
                break;
        }
    }

    /// <summary>
    /// ִ��ƽ��
    /// </summary>
    public void ExecuteQuell(QuellMethod method)
    {
        if (!isRebelContinent) return;

        gameManager_.Spending(abilityCost[(int)tileType, (int)method]); // Cost

        switch (method)
        {
            case QuellMethod.CalmDown:
                isCalmedDown = true;
                break;
            case QuellMethod.Permeation:
                isRebelContinent = false;
                OnRecoverRebel();
                break;
        }
    }

    private void UpdateAttribute()
    {
        // ����ȣ����ý����ٷֱȴ��棩
        unioLevel = 0.8f * (float)(mapGenerator_.unlockedTileTypeCounter[(int)tileType]) / (float)(mapGenerator_.tileTypeCounter[(int)tileType]);

        // �ܸ�
        LeverageLevel = totalGain / (regionOriginFuns + totalGain);

        // �����ȣ���Թֵ����Ȼ����
        resistanceLevelGrowth = baseResistanceLevelGrowth + LeverageLevel * 1f/8f;

        // �����ȣ���Թֵ��
        resistanceLevel = Math.Max(resistanceLevel + resistanceLevelGrowth * Time.deltaTime, 0);
        // ��������ʱ����Ϊ��������
        if (resistanceLevel >= 1)
            OnBeRebelContinent();       

        // ֧�ֶ�
        supportLevel = Math.Clamp(unioLevel + supportLevel_temp, 0, 1);

        // Ŀ��������
        targetCollectionRate = Math.Clamp(baseCollectionRate + supportLevel - resistanceLevel, 0, 1);

        // ��ǰ������
        float alpha = collectionRestitutionFactor * Time.deltaTime;
        currentCollectionRate = currentCollectionRate * (1f - alpha) + targetCollectionRate * alpha;

    }

    /// <summary>
    /// �������
    /// </summary>
    private void CalculateProduct()
    {
        // ������ȴʱ��
        currentCollectionCooldown -= Time.deltaTime;
        // ��������
        if (currentCollectionCooldown <= 0)
        {
            float needIncome = baseCollectionValue * currentCollectionRate;
            gameManager_.Income(needIncome);

            var selfPos = HexToWorldPosition(q, r);
            totalGain += needIncome;
            floatingTextController_.ShowMoneyText(selfPos, needIncome);

            // Reset CD
            currentCollectionCooldown += collectionCooldown;
        }
        
    }

    /// <summary>
    /// ���µؿ����
    /// </summary>
    public void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (!isUnlocked)
        {
            spriteRenderer.color = Color.Lerp(lockedColor, unlockedColor, 0.2f);
        }
        else
        {
            if (isRebelContinent)
            {
                Color baseColor = unlockedColor;
                Color targetColor = Color.Lerp(baseColor, resistanceColor, 0.9f);
                spriteRenderer.color = targetColor;
            }
            else { 
                // ������Թ�Ȼ����ɫ
                Color baseColor = unlockedColor;
                Color targetColor = Color.Lerp(baseColor, resistanceColor, Math.Max(resistanceLevel-0.2f,0f)*0.8f);
                spriteRenderer.color = targetColor;
            }
        }
    }

    /// <summary>
    /// ��Ϊ��������
    /// </summary>
    private void OnBeRebelContinent()
    {
        isCalmedDown = false;
        isRebelContinent = true;
        floatingTextController_.ShowText(transform.position, "Rebel!", new Color(1f, 0.1f, 0.1f, 1f));
    }

    /// <summary>
    /// �ָ�����
    /// </summary>
    private void OnRecoverRebel()
    {
        // �������Բ���
        currentCollectionCooldown = 1.0f; // ��ǰCD

        currentCollectionRate = 0.8f; // ��ǰ������ 0-1

        resistanceLevel = 0.1f; // ������ 0-1
        supportLevel = 0.1f; // ֧�ֶ� 0-1
        supportLevel_temp = 0.5f; // ���������Ӱ��ǰ��֧�ֶ�

        totalGain = 0f; // �������ʽ�
    }

    /// <summary>
    /// ��������
    /// </summary>
    private void UpdateRebelContinent()
    {
        foreach (var tile in neighbors)
        {
            tile.resistanceLevel += 0.01f * Time.deltaTime;
        }
    }

    /// <summary>
    /// ��ȡָ���������������
    /// </summary>
    public static Vector2Int GetNeighborCoordinate(Vector2Int coord, int direction)
    {
        if (direction < 0 || direction >= hexDirections.Length)
        {
            Debug.LogError($"��Ч�ķ���: {direction}");
            return coord;
        }

        return coord + ((coord.x % 2 == 0) ? hexDirections[direction] : hexDirections_01[direction]);
    }

    /// <summary>
    /// ������������ת��Ϊ��������
    /// </summary>
    public static Vector3 HexToWorldPosition(int q, int r)
    {
        float x = q * 1.5f; // ˮƽ���
        float y = r * Mathf.Sqrt(3f) + (q % 2) * (Mathf.Sqrt(3f) / 2f); // ��ֱ��࣬��������

        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// ��ȡ������������
    /// </summary>
    public static List<Vector2Int> GetAllNeighborCoordinates(int q, int r)
    {
        List<Vector2Int> neighborCoords = new List<Vector2Int>();

        for (int i = 0; i < hexDirections.Length; i++)
        {
            Vector2Int neighborCoord = GetNeighborCoordinate(new Vector2Int(q, r), i);
            neighborCoords.Add(neighborCoord);
        }

        return neighborCoords;
    }

}