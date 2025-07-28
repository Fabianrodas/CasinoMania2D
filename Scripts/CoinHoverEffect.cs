using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class CoinHoverEffect : MonoBehaviour, IPointerEnterHandler
{
    [Header("Coin Visuals")]
    public Sprite[] coinFrames;
    public Image coinImagePrefab;

    [Header("Coin Behavior")]
    public int numberOfCoins = 6;
    public float spreadRadius = 60f; // distancia horizontal aleatoria
    public float verticalOffset = 80f;
    public float fallDistance = 100f;
    public float scaleFactor = 0.5f;

    [Header("Timing")]
    public float minFallDuration = 0.6f;
    public float maxFallDuration = 1.2f;
    public float frameRate = 0.05f;
    public float delayBetweenCoins = 0.05f;

    private bool hasPlayed = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hasPlayed)
        {
            hasPlayed = true;
            StartCoroutine(PlayCoinBurst());
        }
    }

    IEnumerator PlayCoinBurst()
    {
        for (int i = 0; i < numberOfCoins; i++)
        {
            StartCoroutine(SpawnSingleCoin(i * delayBetweenCoins));
        }

        yield return new WaitForSeconds(maxFallDuration + 0.5f);
        hasPlayed = false;
    }

    IEnumerator SpawnSingleCoin(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Crear la instancia
        Image coin = Instantiate(coinImagePrefab, transform.parent);
        coin.raycastTarget = false; // para que no bloquee el botón
        RectTransform rt = coin.rectTransform;

        // Posición inicial con dispersión
        Vector2 basePos = ((RectTransform)transform).anchoredPosition + new Vector2(0, verticalOffset);
        float xOffset = Random.Range(-spreadRadius, spreadRadius);
        Vector2 startPos = basePos + new Vector2(xOffset, 0);
        Vector2 endPos = startPos - new Vector2(0, fallDistance);

        rt.anchoredPosition = startPos;
        rt.localScale = Vector3.one * scaleFactor;

        float thisFallDuration = Random.Range(minFallDuration, maxFallDuration);

        StartCoroutine(PlayCoinFrames(coin));

        float t = 0f;
        while (t < thisFallDuration)
        {
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t / thisFallDuration);
            t += Time.deltaTime;
            yield return null;
        }

        Destroy(coin.gameObject);
    }

    IEnumerator PlayCoinFrames(Image coinImage)
    {
        int index = 0;
        while (coinImage != null)
        {
            coinImage.sprite = coinFrames[index];
            index = (index + 1) % coinFrames.Length;
            yield return new WaitForSeconds(frameRate);
        }
    }
}