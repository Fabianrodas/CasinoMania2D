using UnityEngine;

public class WaveLetters : MonoBehaviour
{
    public RectTransform[] letters;
    public float amplitude = 10f;
    public float frequency = 2f;
    public float offset = 0.2f;

    private Vector2[] originalPositions;

    void Start()
    {
        // Guarda las posiciones originales de cada letra
        originalPositions = new Vector2[letters.Length];
        for (int i = 0; i < letters.Length; i++)
        {
            originalPositions[i] = letters[i].anchoredPosition;
        }
    }

    void Update()
    {
        for (int i = 0; i < letters.Length; i++)
        {
            float y = Mathf.Sin(Time.time * frequency + i * offset) * amplitude;
            letters[i].anchoredPosition = originalPositions[i] + new Vector2(0, y);
        }
    }
}
