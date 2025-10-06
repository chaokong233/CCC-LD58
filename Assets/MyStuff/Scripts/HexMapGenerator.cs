using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    [Header("��ͼ����")]
    public int mapWidth = 10;
    public int mapHeight = 10;
    public GameObject hexTilePrefab;

    [Header("��ʼ����")]
    public Vector2Int startTileCoord = new Vector2Int(5, 5);

    [Header("Tile ��С")]
    public Vector2 tileSize = new Vector2(5, 5);

    [Header("���ɲ���")]
    [Range(1, 10)]
    public int cityClusterCount = 3;
    [Range(1, 5)]
    public int cityClusterSize = 3;
    [Range(1, 10)]
    public int obstacleClusterCount = 4;
    [Range(1, 8)]
    public int obstacleClusterSize = 4;
    [Range(1, 3)]
    public int suburbRingCount = 2;
    [Range(1, 4)]
    public int minCityCount = 1;

    [Header("��ͼ����")]
    public List<int> tileTypeCounter = new List<int>(((int)TileType.MaxNum)+1);
    public List<int> unlockedTileTypeCounter = new List<int>(((int)TileType.MaxNum)+1);

    private Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();
    [HideInInspector]
    public Dictionary<Vector2Int, HexTile> cityTiles = new Dictionary<Vector2Int, HexTile>();
    private List<HexTile> allTiles = new List<HexTile>();
    private System.Random random;

    void Start()
    {
        for (int i = 0; i < ((int)TileType.MaxNum) + 1; i++)
        {
            tileTypeCounter.Add(0);
            unlockedTileTypeCounter.Add(0);
        }
        GenerateHexMap();
        SetupNeighborConnections();
        UnlockStartTile();
    }

    /// <summary>
    /// ���������ε�ͼ
    /// </summary>
    void GenerateHexMap()
    {
        do
        {
            random = new System.Random(System.DateTime.Now.Millisecond);

            hexMap.Clear();
            cityTiles.Clear();
            foreach (var item in allTiles)
            {
                Destroy(item.gameObject);
            }
            allTiles.Clear();

            for (int q = 0; q < mapWidth; q++)
            {
                for (int r = 0; r < mapHeight; r++)
                {
                    CreateHexTile(q, r, TileType.Rural);
                }
            }

            //�ڶ��׶Σ������ϰ����򣨺�����ɽ�أ�
            GenerateObstacleClusters();
            // �����׶Σ����ɳ��м�Ⱥ
            GenerateCityClusters();
            // ���Ľ׶Σ�ȷ����ͨ��
            EnsureConnectivity();

            // Counter
            for (int i = 0; i < tileTypeCounter.Count; i++)
            {
                tileTypeCounter[i] = 0;
            }

            foreach (var tile in hexMap)
            {
                if (tile.Value.tileType == TileType.City)
                {
                    cityTiles.Add(tile.Key, tile.Value);
                }
                tileTypeCounter[(int)tile.Value.tileType]++;
            }

        } while (tileTypeCounter[(int)TileType.City] < minCityCount);

        Debug.Log($"��ͼ������ɣ��� {allTiles.Count} ���ؿ�");
    }

    /// <summary>
    /// ����ָ�����͵ĵؿ�
    /// </summary>
    void CreateHexTile(int q, int r, TileType type, string customName = "")
    {
        if (hexTilePrefab == null)
        {
            Debug.LogError("HexTileԤ����δ���䣡");
            return;
        }

        // ������������еؿ飬���Ƴ�
        Vector2Int coord = new Vector2Int(q, r);
        if (hexMap.ContainsKey(coord))
        {
            // ������ϰ��ؿ飬���ܸ���
            if (hexMap[coord].isObstacle())
                return;

            Destroy(hexMap[coord].gameObject);
            hexMap.Remove(coord);
            allTiles.RemoveAll(t => t.q == q && t.r == r);
        }

        GameObject tileObj = Instantiate(hexTilePrefab, new Vector3(), Quaternion.identity, transform);
        tileObj.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1);

        HexTile hexTile = tileObj.GetComponent<HexTile>();
        if (hexTile != null)
        {
            hexTile.Initialize(q, r, customName);
            hexTile.InitializeType(type);
            hexMap[coord] = hexTile;
            allTiles.Add(hexTile);
        }
        else
        {
            Debug.LogError($"��λ�� ({q}, {r}) �����ĵؿ�û��HexTile���");
            Destroy(tileObj);
        }
    }

    bool IsValidCoordinate(Vector2Int coord)
    {
        return coord.x >= 0 && coord.y >= 0 && coord.x < mapWidth && coord.y < mapHeight;
    }

    /// <summary>
    /// �����ϰ����򣨺�����ɽ�أ�
    /// </summary>
    void GenerateObstacleClusters()
    {
        int clustersGenerated = 0;

        while (clustersGenerated < obstacleClusterCount)
        {
            // ���ѡ���ϰ�����
            TileType obstacleType = random.Next(2) == 0 ? TileType.Lake : TileType.Mountain;

            // ���ѡ����ʼ�㣨ȷ�����ڱ�Ե������������ϰ���
            int startQ = random.Next(2, mapWidth - 2);
            int startR = random.Next(2, mapHeight - 2);

            Vector2Int startCoord = new Vector2Int(startQ, startR);

            // �����ʼ���Ƿ��Ѿ����ϰ�
            if (hexMap.ContainsKey(startCoord) && hexMap[startCoord].isObstacle())
                continue;

            // �����ϰ���Ⱥ
            if (GenerateCluster(startCoord, obstacleType, obstacleClusterSize))
            {
                clustersGenerated++;
                //Debug.Log($"����{GetTileTypeData(obstacleType).displayName}��Ⱥ #{clustersGenerated} �� ({startQ}, {startR})");
            }
        }
    }

    /// <summary>
    /// ���ɳ��м�Ⱥ
    /// </summary>
    void GenerateCityClusters()
    {
        int clustersGenerated = 0;

        while (clustersGenerated < cityClusterCount)
        {
            // ���ѡ����ʼ�㣨ȷ�����ڱ�Ե�Ҳ����ϰ���
            int startQ = random.Next(2, mapWidth - 2);
            int startR = random.Next(2, mapHeight - 2);

            Vector2Int startCoord = new Vector2Int(startQ, startR);

            // �����ʼ���Ƿ��ʺ�
            if (!IsValidForCity(startCoord))
                continue;

            // ���ɳ��м�Ⱥ
            if (GenerateCityCluster(startCoord))
            {
                clustersGenerated++;
                Debug.Log($"���ɳ��м�Ⱥ #{clustersGenerated} �� ({startQ}, {startR})");
            }
        }
    }

    /// <summary>
    /// ���ɵ������м�Ⱥ�������������ĺͽ�������
    /// </summary>
    bool GenerateCityCluster(Vector2Int centerCoord)
    {
        // ���ɳ�������
        List<Vector2Int> cityTiles = FloodFill(centerCoord, cityClusterSize,
            coord => !hexMap[coord].isObstacle() && hexMap[coord].tileType != TileType.City);

        if (cityTiles.Count == 0) return false;

        // ��ѡ�еĵؿ�����Ϊ����
        foreach (Vector2Int coord in cityTiles)
        {
            CreateHexTile(coord.x, coord.y, TileType.City, $"����_{coord.x}_{coord.y}");
        }

        // ���ɽ�����
        for (int ring = 1; ring <= suburbRingCount; ring++)
        {
            List<Vector2Int> ringTiles = GetRingTiles(cityTiles, ring);

            foreach (Vector2Int coord in ringTiles)
            {
                if (hexMap.ContainsKey(coord) && !hexMap[coord].isObstacle() &&
                    hexMap[coord].tileType != TileType.City && hexMap[coord].tileType != TileType.Suburb)
                {
                    CreateHexTile(coord.x, coord.y, TileType.Suburb, $"����_{coord.x}_{coord.y}");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// ���ɼ�Ⱥ��ͨ�÷�����
    /// </summary>
    bool GenerateCluster(Vector2Int centerCoord, TileType type, int targetSize)
    {
        List<Vector2Int> clusterTiles = FloodFill(centerCoord, targetSize,
            coord => !hexMap[coord].isObstacle() && hexMap[coord].tileType != type);

        if (clusterTiles.Count < targetSize / 2) // �����Ⱥ̫С������
            return false;

        // ��ѡ�еĵؿ�����Ϊָ������
        foreach (Vector2Int coord in clusterTiles)
        {
            CreateHexTile(coord.x, coord.y, type);
        }

        return true;
    }

    /// <summary>
    /// ��ˮ����㷨������������
    /// </summary>
    List<Vector2Int> FloodFill(Vector2Int startCoord, int maxSize, System.Func<Vector2Int, bool> isValid)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startCoord);
        visited.Add(startCoord);

        while (queue.Count > 0 && result.Count < maxSize)
        {
            Vector2Int current = queue.Dequeue();

            if (isValid(current))
            {
                result.Add(current);

                // ��ȡ��������
                List<Vector2Int> neighbors = HexTile.GetAllNeighborCoordinates(current.x, current.y);

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor) && hexMap.ContainsKey(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// ��ȡָ������Ļ�״�ؿ�
    /// </summary>
    List<Vector2Int> GetRingTiles(List<Vector2Int> centerTiles, int distance)
    {
        HashSet<Vector2Int> ringTiles = new HashSet<Vector2Int>();

        foreach (Vector2Int center in centerTiles)
        {
            // ��ȡ����Ϊdistance�����еؿ�
            List<Vector2Int> tilesAtDistance = GetTilesAtDistance(center, distance);
            foreach (Vector2Int tile in tilesAtDistance)
            {
                if (!centerTiles.Contains(tile) && hexMap.ContainsKey(tile))
                {
                    ringTiles.Add(tile);
                }
            }
        }

        return new List<Vector2Int>(ringTiles);
    }

    /// <summary>
    /// ��ȡָ����������еؿ�
    /// </summary>
    List<Vector2Int> GetTilesAtDistance(Vector2Int center, int distance)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // �򵥵������ξ������
        for (int q = -distance; q <= distance; q++)
        {
            for (int r = -distance; r <= distance; r++)
            {
                if (Mathf.Abs(q + r) <= distance)
                {
                    Vector2Int coord = new Vector2Int(center.x + q, center.y + r);
                    if (IsValidCoordinate(coord))
                    {
                        result.Add(coord);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// ��������Ƿ��ʺϽ�������
    /// </summary>
    bool IsValidForCity(Vector2Int coord)
    {
        if (!hexMap.ContainsKey(coord) || hexMap[coord].isObstacle())
            return false;

        // �����Χ�Ƿ����㹻�Ŀռ�
        int validNeighbors = 0;
        List<Vector2Int> neighbors = HexTile.GetAllNeighborCoordinates(coord.x, coord.y);

        foreach (Vector2Int neighbor in neighbors)
        {
            if (hexMap.ContainsKey(neighbor) && !hexMap[neighbor].isObstacle())
            {
                validNeighbors++;
            }
        }

        return validNeighbors >= 4; // ����4����Ч���ڵؿ�
    }

    /// <summary>
    /// ȷ�����п�ͨ�еؿ����ͨ��
    /// </summary>
    void EnsureConnectivity()
    {
        // �ҵ����п�ͨ�еؿ�
        List<Vector2Int> passableTiles = new List<Vector2Int>();
        foreach (var pair in hexMap)
        {
            if (!pair.Value.isObstacle())
            {
                passableTiles.Add(pair.Key);
            }
        }

        if (passableTiles.Count == 0) return;

        // ʹ��BFS�����ͨ��
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // ����ʼ�㿪ʼ
        queue.Enqueue(startTileCoord);
        visited.Add(startTileCoord);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            List<Vector2Int> neighbors = HexTile.GetAllNeighborCoordinates(current.x, current.y);
            foreach (Vector2Int neighbor in neighbors)
            {
                if (hexMap.ContainsKey(neighbor) && !hexMap[neighbor].isObstacle() &&
                    !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // ��δ��ͨ�ĵؿ�ת��Ϊũ��
        int convertedCount = 0;
        foreach (Vector2Int tile in passableTiles)
        {
            if (!visited.Contains(tile))
            {
                CreateHexTile(tile.x, tile.y, TileType.Rural);
                convertedCount++;
            }
        }

        if (convertedCount > 0)
        {
            Debug.Log($"ȷ����ͨ��: ת���� {convertedCount} ��δ��ͨ�ؿ�Ϊũ��");
        }
    }

    /// <summary>
    /// �������еؿ�����ڹ�ϵ
    /// </summary>
    void SetupNeighborConnections()
    {
        foreach (var tile in allTiles)
        {
            tile.neighbors.Clear();

            // ��ȡ������������
            List<Vector2Int> neighborCoords = HexTile.GetAllNeighborCoordinates(tile.q, tile.r);

            foreach (Vector2Int coord in neighborCoords)
            {
                if (hexMap.TryGetValue(coord, out HexTile neighborTile))
                {
                    tile.neighbors.Add(neighborTile);
                }
            }
        }

        Debug.Log("���ڹ�ϵ�������");
    }

    /// <summary>
    /// ������ʼ�ؿ�
    /// </summary>
    void UnlockStartTile()
    {
        HexTile startTile = FindSuitableStartTile();

        if (startTile != null)
        {
            startTileCoord = new Vector2Int(startTile.q, startTile.r);
            unlockedTileTypeCounter[(int)startTile.tileType]++;

            startTile.UnlockTile();
            Debug.Log($"���ܽ�����ʼ�ؿ�: {startTile.tileName} ({startTile.q}, {startTile.r})");
        }
        else
        {
            Debug.LogError("�޷��ҵ����ʵ�ũ����ʼ�ؿ飡");
        }
    }

    /// <summary>
    /// Ѱ�Һ��ʵ���ʼ�ؿ�
    /// </summary>
    HexTile FindSuitableStartTile()
    {
        List<HexTile> ruralTiles = new List<HexTile>();

        // �ռ�����ũ��ؿ�
        foreach (var tile in allTiles)
        {
            if (tile.tileType == TileType.Rural && !tile.isObstacle())
            {
                ruralTiles.Add(tile);
            }
        }

        if (ruralTiles.Count == 0)
        {
            Debug.LogWarning("û���ҵ��κ�ũ��ؿ飬����Ѱ��������ͨ�еؿ�");
            // ���û��ũ��ؿ飬Ѱ���κο�ͨ�еؿ�
            foreach (var tile in allTiles)
            {
                if (!tile.isObstacle() && tile.tileType != TileType.Lake && tile.tileType != TileType.Mountain)
                {
                    ruralTiles.Add(tile);
                }
            }
        }

        if (ruralTiles.Count == 0)
        {
            Debug.LogError("û���ҵ��κο�ͨ�е���ʼ�ؿ飡");
            return null;
        }

        // ���ָ������ʼ�����Ҹ�������ũ�壬����ʹ��
        if (hexMap.ContainsKey(startTileCoord))
        {
            HexTile specifiedTile = hexMap[startTileCoord];
            if (specifiedTile.tileType == TileType.Rural && !specifiedTile.isObstacle())
            {
                Debug.Log($"ʹ��ָ����ũ����ʼ�ؿ�: ({startTileCoord.x}, {startTileCoord.y})");
                return specifiedTile;
            }
        }

        // ���ѡȡ
        HexTile randomTile = ruralTiles[random.Next(ruralTiles.Count)];
        Debug.Log($"���ѡ��ũ��ؿ�: ({randomTile.q}, {randomTile.r})");
        return randomTile;
    }


    /// <summary>
    /// ����ָ������ĵؿ�
    /// </summary>
    public bool UnlockTileAtCoord(int q, int r, float cost = 0f)
    {
        Vector2Int coord = new Vector2Int(q, r);
        if (hexMap.TryGetValue(coord, out HexTile tile))
        {
            // ����Ƿ��Ѿ������ڵĽ����ؿ�
            bool hasUnlockedNeighbor = false;
            foreach (var neighbor in tile.neighbors)
            {
                if (neighbor.isUnlocked)
                {
                    hasUnlockedNeighbor = true;
                    break;
                }
            }

            if (hasUnlockedNeighbor || (q == startTileCoord.x && r == startTileCoord.y))
            {
                tile.UnlockTile();
                unlockedTileTypeCounter[(int)tile.tileType]++;

                return true;
            }
            else
            {
                Debug.LogWarning($"�޷������ؿ� ({q}, {r})��û�����ڵĽ����ؿ�");
                return false;
            }
        }

        Debug.LogWarning($"�Ҳ������� ({q}, {r}) �ĵؿ�");
        return false;
    }

    /// <summary>
    /// ��ȡָ������ĵؿ�
    /// </summary>
    public HexTile GetTileAtCoord(int q, int r)
    {
        Vector2Int coord = new Vector2Int(q, r);
        hexMap.TryGetValue(coord, out HexTile tile);
        return tile;
    }

    /// <summary>
    /// ��ȡ���еؿ�
    /// </summary>
    public List<HexTile> GetAllTiles()
    {
        return new List<HexTile>(allTiles);
    }

    /// <summary>
    /// �������ɵ�ͼ���༭�����ã�
    /// </summary>
    [ContextMenu("�������ɵ�ͼ")]
    public void RegenerateMap()
    {
        // ������еؿ�
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        GenerateHexMap();
        SetupNeighborConnections();
        UnlockStartTile();
    }

    /// <summary>
    /// ��Scene��ͼ�л��Ƶ�����Ϣ
    /// </summary>
    private void OnDrawGizmos()
    {
        if (allTiles == null) return;

        foreach (var tile in allTiles)
        {
            if (tile != null)
            {
                // ��������������
                Gizmos.color = Color.green;
                foreach (var neighbor in tile.neighbors)
                {
                    if (neighbor != null)
                    {
                        Gizmos.DrawLine(tile.transform.position, neighbor.transform.position);
                    }
                }

                // ���������ǩ
#if UNITY_EDITOR
                // UnityEditor.Handles.Label(tile.transform.position + Vector3.up * 0.3f, $"({tile.q},{tile.r})");
#endif
            }
        }
    }
}