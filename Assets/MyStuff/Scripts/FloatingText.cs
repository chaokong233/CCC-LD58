using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("�������")]
    public TextMeshProUGUI textComponent;
    public CanvasGroup canvasGroup;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float duration;
    private float elapsedTime;
    private AnimationCurve floatCurve;
    private AnimationCurve fadeCurve;

    void Awake()
    {
        // �Զ���ȡ�������
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Initialize(Vector3 worldPosition, string text, Color color, float duration,
                          float floatDistance, AnimationCurve floatCurve, AnimationCurve fadeCurve)
    {
        // ����λ��
        transform.position = worldPosition;
        startPosition = worldPosition;
        targetPosition = worldPosition + Vector3.up * floatDistance;

        // �����ı�����ɫ
        textComponent.text = text;
        textComponent.color = color;

        // ���ö�������
        this.duration = duration;
        this.floatCurve = floatCurve;
        this.fadeCurve = fadeCurve;
        elapsedTime = 0f;

        // ����CanvasGroup
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// ��������״̬
    /// </summary>
    /// <returns>�Ƿ��ڻ�Ծ״̬</returns>
    public bool UpdateText(float deltaTime)
    {
        elapsedTime += deltaTime;

        if (elapsedTime >= duration)
        {
            return false; // ��������
        }

        float progress = elapsedTime / duration;

        // ����λ��
        float floatProgress = floatCurve.Evaluate(progress);
        transform.position = Vector3.Lerp(startPosition, targetPosition, floatProgress);

        // ����͸����
        if (canvasGroup != null)
        {
            canvasGroup.alpha = fadeCurve.Evaluate(progress);
        }

        return true;
    }
}