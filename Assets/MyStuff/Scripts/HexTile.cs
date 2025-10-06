using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebtCollectionMethod
{
    None = 0,
    Gentle = 1,     // �ºʹ�ծ
    Legal = 2,      // ���ɴ�ծ
    Quell = 3,      // ƽϢ
    Violent = 4     // ������ծ
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
    public float currentCollectionCooldown = 8.0f; // ��ǰCD

    public float baseCollectionValue = 8.0f; // ��������ֵ
    public float currentCollectionRate = 0.8f; // ��ǰ������ 0-1
    public float baseCollectionRate = 0.3f; // ���������� 0-1
    public float targetCollectionRate = 0.3f; // Ŀ�������� 0-1

    public float resistanceLevel = 0.1f; // ������ 0-1
    public float supportLevel = 0.0f; // ֧�ֶ� 0-1
    public float supportLevel_temp = 0.0f; // ���������Ӱ��ǰ��֧�ֶ�
    public float unioLevel = 0.0f; // ����� 0-1(�ý����ٷֱȴ���)
    public float unioLevelFractor = 0.5f; // ����ȳ��� 0-1
    public float LeverageLevel = 0.0f; // �ܸ�ֵ 0-1

    public float resistanceLevelGrowth = -0.35f / 90f; // ��������Ȼ����
    public float baseResistanceLevelGrowth = -0.35f / 90f; // ��ʼ��������Ȼ����
    public float regionOriginFuns = 420.0f; // ����ԭʼ�ʽ�
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

    [Header("0.default 1.City 2.Suburb 3.Rural 4.Lake 5.Mountain")]
    public Sprite[] TileTypeSprites = { };

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
    public static readonly float[,] abilityCost = new float[4, 7]
    {
        {0,0,0,0,0,0,0},
        {0,     120f,150f,300f,50f,   2000f,1200f},
        {0,     30f,40f,70f,10f,      600f,400f},
        {0,     15f,20f,35f,0f,       300f,200f}
    };

    private static readonly float collectionRestitutionFactor = 1f / 60f;
    private static readonly float rebelContinentLerpFactor = 0.05f;

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
        if (isUnlocked)
        {
            if (!isRebelContinent)
            {
                if (currentDebtCollectionMethodCooldown > 0)
                    currentDebtCollectionMethodCooldown -= Time.deltaTime;
                UpdateAttribute();
                CalculateProduct();
            }
            else
            {
                if (!isCalmedDown)
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

        // Initialize TileType Attribute
        switch (type)
        {
            case TileType.Rural:
                debtCost = 100.0f; // ���ν���۸�
                baseCollectionValue = 8.0f; // ��������ֵ
                baseCollectionRate = 0.3f; // ���������� 0-1
                resistanceLevel = 0.1f; // ������ 0-1
                supportLevel_temp = 0.0f; // ���������Ӱ��ǰ��֧�ֶ�
                unioLevelFractor = 0.55f; // �����Ӱ������
                baseResistanceLevelGrowth = -0.3f / 90f; // ��ʼ��������Ȼ����
                regionOriginFuns = 380.0f; // ����ԭʼ�ʽ�
                break;
            case TileType.Suburb:
                debtCost = 200.0f; // ���ν���۸�
                baseCollectionValue = 20.0f; // ��������ֵ
                baseCollectionRate = 0.2f; // ���������� 0-1
                resistanceLevel = 0.1f; // ������ 0-1
                supportLevel_temp = 0.0f; // ���������Ӱ��ǰ��֧�ֶ�
                unioLevelFractor = 0.3f; // �����Ӱ������
                baseResistanceLevelGrowth = -0.0025f; // ��ʼ��������Ȼ����
                regionOriginFuns = 8000.0f; // ����ԭʼ�ʽ�
                break;
            case TileType.City:
                debtCost = 1000.0f; // ���ν���۸�
                baseCollectionValue = 80f; // ��������ֵ
                baseCollectionRate = 0.1f; // ���������� 0-1
                resistanceLevel = 0.1f; // ������ 0-1
                supportLevel_temp = 0.0f; // ���������Ӱ��ǰ��֧�ֶ�
                unioLevelFractor = 0.1f; // �����Ӱ������
                baseResistanceLevelGrowth = -0.001f; // ��ʼ��������Ȼ����
                regionOriginFuns = 5000.0f; // ����ԭʼ�ʽ�
                break;
            case 0:
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

        gameManager_.Spending(abilityCost[(int)tileType, (int)method]); // Cost

        switch (method)
        {
            case DebtCollectionMethod.Gentle:
                currentCollectionRate = Math.Max(0.80f, currentCollectionRate);
                supportLevel_temp += 0.10f;
                resistanceLevel += 0.05f;
                break;
            case DebtCollectionMethod.Legal:
                currentCollectionRate = Math.Max(1.00f, currentCollectionRate);
                supportLevel_temp += 0.03f;
                resistanceLevel += 0.15f;
                break;
            case DebtCollectionMethod.Quell:
                currentCollectionRate = Math.Max(0.60f, currentCollectionRate);
                supportLevel += 0.08f;
                resistanceLevel += -0.60f;
                break;
            case DebtCollectionMethod.Violent:
                currentCollectionRate = Math.Max(1.20f, currentCollectionRate);
                supportLevel_temp += -0.05f;
                resistanceLevel += 0.30f;
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
        unioLevel = (float)(mapGenerator_.unlockedTileTypeCounter[(int)tileType]) / (float)(mapGenerator_.tileTypeCounter[(int)tileType]);

        // �ܸ�, ʱ�����
        LeverageLevel = totalGain / (regionOriginFuns + totalGain * Mathf.Max(1f, 2f - gameManager_.currentTime * 0.2f / 60f));

        float LeverageLevelMutiplier = 0.0487f;
        float supportLevelLevelMutiplier = -0.033f;
        // �����ȣ���Թֵ����Ȼ����
        resistanceLevelGrowth = baseResistanceLevelGrowth + LeverageLevel * LeverageLevelMutiplier + supportLevel * supportLevelLevelMutiplier;

        // �����ȣ���Թֵ��
        // remap: 60~100 -> 1~0.8, ��Թֵ�ϸ�ʱ����������
        float resistanceLevelMutiplier = resistanceLevelGrowth < 0.6 ? 1.0f : Mathf.Lerp(1f, 0.7f, (resistanceLevel - 0.6f) / 0.4f);
        resistanceLevel = Math.Max(resistanceLevel + resistanceLevelGrowth * resistanceLevelMutiplier * Time.deltaTime, 0);
        // ��������ʱ����Ϊ��������
        if (resistanceLevel >= 1)
        {
            OnBeRebelContinent();
            resistanceLevel = 1;
        }

        // ֧�ֶ�
        supportLevel = Math.Clamp(unioLevel * unioLevelFractor + supportLevel_temp, 0, 1);

        // Ŀ��������
        targetCollectionRate = Math.Clamp(baseCollectionRate + supportLevel - resistanceLevel, 0, 1);

        // ��ǰ������
        float alpha = collectionRestitutionFactor * Time.deltaTime;
        currentCollectionRate = Mathf.Lerp(currentCollectionRate, targetCollectionRate, alpha);
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
            else
            {
                // ������Թ�Ȼ����ɫ
                Color baseColor = unlockedColor;
                Color targetColor = Color.Lerp(baseColor, resistanceColor, Math.Max(resistanceLevel - 0.2f, 0f) * 0.8f);
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
    /// ��������Ӱ����Χ
    /// </summary>
    private void UpdateRebelContinent()
    {
        float alpha = rebelContinentLerpFactor * Time.deltaTime;
        foreach (var tile in neighbors)
        {
            tile.resistanceLevel = Mathf.Lerp(tile.resistanceLevel, 1f, alpha);
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