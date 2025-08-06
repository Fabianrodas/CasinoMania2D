using UnityEngine;

public class SlotReel : MonoBehaviour
{
    [Header("References")]
    public Transform content;
    public GameObject[] symbolPrefabs;

    [Header("Reel Settings")]
    public int visibleSymbols = 3;
    public int bufferSymbols = 3; // símbolos extra arriba para el loop
    public float speed = 10f;
    public float stopDelay = 2f;
    public float symbolSpacing = 0.1f;

    private Transform[] symbols;
    private float symbolHeight;
    private int totalSymbols;
    private bool spinning = false;
    private float timer = 0f;

    void Start()
    {
        totalSymbols = visibleSymbols + bufferSymbols;
        InitSymbols();
    }

    void InitSymbols()
    {
        // Limpiar hijos previos
        foreach (Transform child in content)
            Destroy(child.gameObject);

        symbols = new Transform[totalSymbols];

        for (int i = 0; i < totalSymbols; i++)
        {
            int randomIndex = Random.Range(0, symbolPrefabs.Length);
            GameObject symbol = Instantiate(symbolPrefabs[randomIndex], content);

            SpriteRenderer sr = symbol.GetComponent<SpriteRenderer>();
            if (i == 0) symbolHeight = sr.bounds.size.y + symbolSpacing;

            // Posición exacta según índice
            symbol.transform.localPosition = new Vector3(0, -i * symbolHeight, 0);
            symbols[i] = symbol.transform;
        }
    }

    void Update()
    {
        if (!spinning) return;

        for (int i = 0; i < symbols.Length; i++)
        {
            symbols[i].localPosition += Vector3.down * speed * Time.deltaTime;

            // Si el símbolo sale del área visible + buffer
            if (symbols[i].localPosition.y < -symbolHeight * (totalSymbols - 1))
            {
                // Reposicionar exacto arriba del símbolo más alto
                float highestY = GetHighestSymbolY();
                symbols[i].localPosition = new Vector3(0, highestY + symbolHeight, 0);

                // Asignar sprite aleatorio
                int randomIndex = Random.Range(0, symbolPrefabs.Length);
                symbols[i].GetComponent<SpriteRenderer>().sprite = symbolPrefabs[randomIndex].GetComponent<SpriteRenderer>().sprite;
            }
        }

        timer += Time.deltaTime;
        if (timer >= stopDelay)
        {
            spinning = false;
            timer = 0f;
            SnapToGrid();
        }
    }

    float GetHighestSymbolY()
    {
        float maxY = float.MinValue;
        foreach (Transform s in symbols)
            if (s.localPosition.y > maxY)
                maxY = s.localPosition.y;
        return maxY;
    }

    void SnapToGrid()
    {
        for (int i = 0; i < symbols.Length; i++)
        {
            float snappedY = Mathf.Round(symbols[i].localPosition.y / symbolHeight) * symbolHeight;
            symbols[i].localPosition = new Vector3(0, snappedY, 0);
        }
    }

    public void Spin()
    {
        spinning = true;
        timer = 0f;
    }
}