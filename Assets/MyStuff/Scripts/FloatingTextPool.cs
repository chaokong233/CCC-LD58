using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FloatingTextController : MonoBehaviour
{
    [Header("��������")]
    public GameObject floatingTextPrefab;
    public Transform textParent;
    public float textDuration = 1.5f;
    public float floatDistance = 0.1f;
    public Color positiveColor = Color.green;
    public Color negativeColor = Color.red;

    [Header("��������")]
    public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Camera mainCamera;
    private Queue<FloatingText> textPool = new Queue<FloatingText>();
    private List<FloatingText> activeTexts = new List<FloatingText>();

    void Start()
    {
        mainCamera = Camera.main;

        // ���û��ָ��������ʹ�õ�ǰ����
        if (textParent == null)
            textParent = FindFirstObjectByType<Canvas>().GetComponent<Transform>();

        // Ԥ����һЩ���ֶ���
        PrewarmPool(10);
    }

    void Update()
    {
        // �������л�Ծ������
        for (int i = activeTexts.Count - 1; i >= 0; i--)
        {
            if (!activeTexts[i].UpdateText(Time.deltaTime))
            {
                // �������������ն���
                ReturnToPool(activeTexts[i]);
                activeTexts.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Ԥ���������
    /// </summary>
    void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateNewTextObject();
        }
    }

    /// <summary>
    /// �����µ����ֶ���
    /// </summary>
    void CreateNewTextObject()
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("����Ԥ����δ���䣡");
            return;
        }

        GameObject textObj = Instantiate(floatingTextPrefab, textParent);
        FloatingText floatingText = textObj.GetComponent<FloatingText>();

        if (floatingText != null)
        {
            textObj.SetActive(false);
            textPool.Enqueue(floatingText);
        }
    }

    /// <summary>
    /// �Ӷ���ػ�ȡ���ֶ���
    /// </summary>
    FloatingText GetFromPool()
    {
        if (textPool.Count == 0)
        {
            CreateNewTextObject();
        }

        FloatingText text = textPool.Dequeue();
        text.gameObject.SetActive(true);
        return text;
    }

    /// <summary>
    /// �������ֶ��󵽶����
    /// </summary>
    void ReturnToPool(FloatingText text)
    {
        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
    }

    /// <summary>
    /// ������λ����ʾ��Ǯ����
    /// </summary>
    public void ShowMoneyText(Vector3 worldPosition, float amount, string prefix = "")
    {
        FloatingText text = GetFromPool();

        // �����ı�����
        string textContent = prefix + (amount >= 0 ? "+" : "-") + "$" + amount.ToString("F0");

        // ������ɫ
        Color textColor = amount >= 0 ? positiveColor : negativeColor;

        // ��ʼ������
        text.Initialize(worldPosition+new Vector3(0,0.2f,0), textContent, textColor, textDuration, floatDistance, floatCurve, fadeCurve);

        activeTexts.Add(text);
    }

    /// <summary>
    /// ����Ļλ����ʾ��Ǯ����
    /// </summary>
    public void ShowMoneyTextAtScreen(Vector2 screenPosition, float amount, string prefix = "")
    {
        // ����Ļ����ת��Ϊ��������
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPos.z = 0;

        ShowMoneyText(worldPos, amount, prefix);
    }

    /// <summary>
    /// ������л�Ծ������
    /// </summary>
    public void ClearAllTexts()
    {
        foreach (var text in activeTexts)
        {
            ReturnToPool(text);
        }
        activeTexts.Clear();
    }
}