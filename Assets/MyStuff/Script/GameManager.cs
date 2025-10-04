using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("��Ϸ����")]
    public float initialFunds = 500f;
    public float currentFunds;

    [Header("����")]
    public HexMapGenerator mapGenerator;
    public HexUIController uiController;

    private List<HexTile> unlockedTiles = new List<HexTile>();

    void Start()
    {
        currentFunds = initialFunds;
        Debug.Log($"��Ϸ��ʼ����ʼ�ʽ�: {currentFunds}");
    }

    void Update()
    {

    }

    /// <summary>
    /// �����ؿ�
    /// </summary>
    public bool UnlockTile(int q, int r, float cost)
    {
        if (currentFunds >= cost)
        {
            if (mapGenerator.UnlockTileAtCoord(q, r, cost))
            {
                currentFunds -= cost;
                HexTile tile = mapGenerator.GetTileAtCoord(q, r);
                if (tile != null)
                {
                    unlockedTiles.Add(tile);
                }

                Debug.Log($"�����ؿ� ({q}, {r})������: {cost}��ʣ���ʽ�: {currentFunds}");
                return true;
            }
        }
        else
        {
            Debug.LogWarning($"�ʽ��㣡��Ҫ: {cost}����ǰ: {currentFunds}");
        }

        return false;
    }

    /// <summary>
    /// ����ʽ�ӿ�
    /// </summary>
    public void Income(float value)
    {
        currentFunds += value;
    }

    /// <summary>
    /// ֧���ʽ�ӿ�
    /// </summary>
    public void Spending(float value)
    {
        currentFunds -= value;
        if (currentFunds < 0)
            Debug.LogError($"�ʽ��㣡��ǰ�ʽ�Ϊ{currentFunds}��");
    }

    /// <summary>
    /// ����ʽ𣨲����ã�
    /// </summary>
    public void AddFunds(float amount)
    {
        currentFunds += amount;
        Debug.Log($"����ʽ�: {amount}����ǰ�ʽ�: {currentFunds}");
    }
}
