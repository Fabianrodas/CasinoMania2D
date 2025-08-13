using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class SlotReel : MonoBehaviour
{
    [Header("References")]
    public Transform content;
    public GameObject[] symbolPrefabs;
    public Transform paylineRef;

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

    // >>> NUEVO: evento al detenerse y propiedad de lectura
    public event Action<SlotReel, Sprite> OnStopped; 
    public bool IsSpinning => spinning;

    void Start()
    {
        totalSymbols = visibleSymbols + bufferSymbols;
        InitSymbols();
    }

    void InitSymbols()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        symbols = new Transform[totalSymbols];

        for (int i = 0; i < totalSymbols; i++)
        {
            int randomIndex = Random.Range(0, symbolPrefabs.Length);
            GameObject symbol = Instantiate(symbolPrefabs[randomIndex], content);

            SpriteRenderer sr = symbol.GetComponent<SpriteRenderer>();
            if (i == 0) symbolHeight = sr.bounds.size.y + symbolSpacing;

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

            if (symbols[i].localPosition.y < -symbolHeight * (totalSymbols - 1))
            {
                float highestY = GetHighestSymbolY();
                symbols[i].localPosition = new Vector3(0, highestY + symbolHeight, 0);

                // reasignar sprite aleatorio desde tus prefabs
                int randomIndex = Random.Range(0, symbolPrefabs.Length);
                var srTo = symbols[i].GetComponent<SpriteRenderer>();
                var srFrom = symbolPrefabs[randomIndex].GetComponent<SpriteRenderer>();
                srTo.sprite = srFrom.sprite;
            }
        }

        timer += Time.deltaTime;
        if (timer >= stopDelay)
        {
            spinning = false;
            timer = 0f;
            SnapToGrid();

            // >>> NUEVO: avisar resultado
            Sprite center = GetCenterSprite();
            OnStopped?.Invoke(this, center);
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

    Sprite GetCenterSprite()
    {
        // Si hay paylineRef, usamos su Y en el espacio local de 'content'
        float targetY;
        if (paylineRef != null)
            targetY = content.InverseTransformPoint(paylineRef.position).y;
        else
        {
            // Fallback al cálculo teórico (para 3 visibles, el centro es -1*h)
            targetY = -symbolHeight * (visibleSymbols / 2);
        }

        Transform closest = null;
        float best = float.MaxValue;

        for (int i = 0; i < symbols.Length; i++)
        {
            float d = Mathf.Abs(symbols[i].localPosition.y - targetY);
            if (d < best)
            {
                best = d;
                closest = symbols[i];
            }
        }

        return closest.GetComponent<SpriteRenderer>().sprite;
    }
    
    public void Spin()
    {
        spinning = true;
        timer = 0f;
    }
}
