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

    private Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();
    private List<HexTile> allTiles = new List<HexTile>();

    void Start()
    {
        GenerateHexMap();
        SetupNeighborConnections();
        UnlockStartTile();
    }

    /// <summary>
    /// ���������ε�ͼ
    /// </summary>
    void GenerateHexMap()
    {
        hexMap.Clear();
        allTiles.Clear();

        for (int q = 0; q < mapWidth; q++)
        {
            for (int r = 0; r < mapHeight; r++)
            {
                CreateHexTile(q, r);
            }
        }

        Debug.Log($"��ͼ������ɣ��� {allTiles.Count} ���ؿ�");
    }

    /// <summary>
    /// �������������εؿ�
    /// </summary>
    void CreateHexTile(int q, int r)
    {
        if (hexTilePrefab == null)
        {
            Debug.LogError("HexTileԤ����δ���䣡");
            return;
        }

        Vector3 worldPosition = HexTile.HexToWorldPosition(q, r);
        GameObject tileObj = Instantiate(hexTilePrefab, worldPosition, Quaternion.identity, transform);
        tileObj.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1);

        HexTile hexTile = tileObj.GetComponent<HexTile>();
        if (hexTile != null)
        {
            hexTile.Initialize(q, r);
            hexMap[new Vector2Int(q, r)] = hexTile;
            allTiles.Add(hexTile);
        }
        else
        {
            Debug.LogError($"��λ�� ({q}, {r}) �����ĵؿ�û��HexTile���");
            Destroy(tileObj);
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
        if (hexMap.TryGetValue(startTileCoord, out HexTile startTile))
        {
            startTile.UnlockTile();
            Debug.Log($"��ʼ�ؿ� {startTileCoord} �ѽ���");
        }
        else
        {
            Debug.LogError($"�Ҳ�����ʼ�ؿ�����: {startTileCoord}");
        }
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
                Gizmos.color = tile.isUnlocked ? Color.green : Color.gray;
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