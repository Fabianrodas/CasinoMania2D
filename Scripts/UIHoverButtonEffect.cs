using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHoverButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleAmount = 1.1f;
    public float scaleSpeed = 5f;
    public Color highlightColor = new Color(1f, 1f, 0.5f); // amarillo suave

    private Vector3 originalScale;
    private Color originalColor;
    private Image buttonImage;
    private bool isHovered = false;

    void Start()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
    }

    void Update()
    {
        // Escalar suavemente
        Vector3 targetScale = isHovered ? originalScale * scaleAmount : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        // Color de fondo
        if (buttonImage != null)
        {
            Color targetColor = isHovered ? highlightColor : originalColor;
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * scaleSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}
