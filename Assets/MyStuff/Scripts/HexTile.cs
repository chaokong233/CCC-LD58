using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebtCollectionMethod
{
    None,
    Gentle,     // �ºʹ�ծ
    Legal,      // ���ɴ�ծ
    Violent     // ������ծ
}

public class HexTile : MonoBehaviour
{
    [Header("�ؿ������Ϣ")]
    public int q; // ��������������Q
    public int r; // ��������������R
    public string tileName;
    public bool isUnlocked = false;

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
    public float unioLevel = 0.0f; // ����� 0-1

    private DebtCollectionMethod currentCollectionMethod = DebtCollectionMethod.None;
    public float DebtCollectionMethodCooldown = 30.0f; // ��ծ��ʽCD
    public float currentDebtCollectionMethodCooldown = 0.0f; // ��ǰCD

    private GameManager gameManager_;
    private FloatingTextController floatingTextController_;

    [Header("���ڵؿ�")]
    public List<HexTile> neighbors = new List<HexTile>();

    [Header("���ӻ����")]
    public SpriteRenderer spriteRenderer;
    public Color unlockedColor = Color.white;
    public Color resistanceColor = Color.red;
    public Color lockedColor = Color.gray;

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

    private static readonly float collectionRestitutionFactor = 1f / (3f * 8.0f);
    private static readonly float resistanceReduceFactor = 1f / 180f;
    

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (gameManager_ == null)
            gameManager_ = FindFirstObjectByType<GameManager>();
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
            if (currentDebtCollectionMethodCooldown > 0)
                currentDebtCollectionMethodCooldown -= Time.deltaTime;

            UpdateAttribute();
            UpdateVisuals();
            CalculateProduct();
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

        currentCollectionMethod = method;
        currentDebtCollectionMethodCooldown = DebtCollectionMethodCooldown; // ��ȴ

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
                Debug.Log("1");
                break;
            case DebtCollectionMethod.Legal:
                Debug.Log("2");
                break;
            case DebtCollectionMethod.Violent:
                Debug.Log("3");
                break;
        }
    }

    /// <summary>
    /// ��������ȼӳ�
    /// </summary>
    //private float CalculateConnectionBonus()
    //{
    //    int connectedUnlockedNeighbors = 0;
    //    foreach (var neighbor in neighbors)
    //    {
    //        if (neighbor != null && neighbor.isUnlocked)
    //            connectedUnlockedNeighbors++;
    //    }

    //    // ÿ��һ�����ڽ����ؿ飬����5%��������
    //    return connectedUnlockedNeighbors * 0.05f;
    //}

    /// <summary>
    /// ���¸�������
    /// </summary>
    private void UpdateAttribute()
    {
        // �����ȣ���Թֵ��
        resistanceLevel = Math.Max(resistanceLevel - resistanceReduceFactor * Time.deltaTime, 0);

        // ֧�ֶ�
        supportLevel = unioLevel + supportLevel_temp;

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
            spriteRenderer.color = lockedColor;
        }
        else
        {
            // ������Թ�Ȼ����ɫ
            Color baseColor = unlockedColor;
            Color targetColor = Color.Lerp(baseColor, resistanceColor, Math.Max(resistanceLevel-0.2f,0f)*0.8f);
            spriteRenderer.color = targetColor;
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