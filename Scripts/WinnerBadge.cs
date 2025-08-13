using UnityEngine;
using System.Collections;
using TMPro;

public class WinnerBadge : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private SpriteRenderer circle; // BadgeCircle
    [SerializeField] private TextMeshProUGUI numberText;   // BadgeText

    [Header("Sprites de color")]
    [SerializeField] private Sprite redCircle;
    [SerializeField] private Sprite blackCircle;
    [SerializeField] private Sprite greenCircle;

    [Header("Aparición")]
    [SerializeField] private float targetDiameterWorld = 2.2f; // ajusta hasta que llegue al borde verde
    [SerializeField] private float popTime = 0.25f;
    [SerializeField] private float showTime = 2.0f;
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0,0,1,1);

    Vector3 baseScale;

    void Awake()
    {
        if (circle) circle.enabled = false;
        if (numberText) numberText.gameObject.SetActive(false);
        baseScale = transform.localScale;
    }

    public void Show(int number, bool isRed, bool isGreen)
    {
        // Sprite por color
        if (circle)
        {
            circle.sprite = isGreen ? greenCircle : (isRed ? redCircle : blackCircle);
            // Escala para que el diámetro del sprite ocupe targetDiameterWorld
            if (circle.sprite != null)
            {
                var b = circle.sprite.bounds;
                float curDiameter = Mathf.Max(b.size.x, b.size.y);
                float s = (curDiameter > 0f) ? (targetDiameterWorld / curDiameter) : 1f;
                circle.transform.localScale = Vector3.one * s;
            }
        }

        if (numberText)
        {
            numberText.text = number.ToString();
            numberText.fontSize = 60; // tamaño base para TextMesh; ajusta si hace falta
        }

        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        // Pop-in
        if (circle) { circle.enabled = true; }
        if (numberText) { numberText.gameObject.SetActive(true); }

        float t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / popTime);
            float s = Mathf.Lerp(0.6f, 1.08f, popCurve.Evaluate(u));
            if (circle) circle.transform.localScale = circle.transform.localScale.normalized * circle.transform.localScale.magnitude * 1f; // mantener
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;

        yield return new WaitForSeconds(showTime);

        // Ocultar
        if (circle) circle.enabled = false;
        if (numberText) numberText.gameObject.SetActive(false);
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (circle) circle.enabled = false;
        if (numberText) numberText.gameObject.SetActive(false);
    }
}
