using UnityEngine;
using TMPro;
using System.Collections;

public class ResultMessageUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text msg;            // tu TextMeshPro-UGUI
    [SerializeField] private RectTransform panelRoot; // tu GameObject (solo Transform)

    [Header("Tiempo visible")]
    [SerializeField] private float showSeconds = 2.5f;

    void Awake()
    {
        if (!msg) msg = GetComponent<TMP_Text>();
        if (msg) msg.text = "";
        if (panelRoot) panelRoot.gameObject.SetActive(false);
    }

    // Llamado desde RouletteRoundController
    public void ShowResult(int winningNumber, int totalStake, int profit, int net, bool isRed, bool isGreen)
    {
        // Texto simple (ajústalo si quieres otro formato)
        string colorName = isGreen ? "VERDE" : (isRed ? "ROJO" : "NEGRO");
        string txt = $"Número: {winningNumber} ({colorName})  •  "
                   + (net >= 0 ? $"¡Ganaste {profit}! (neto +{net})" : $"Perdiste {Mathf.Abs(net)}")
                   + $"  •  Apostado: {totalStake}";

        if (msg) msg.text = txt;

        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        if (panelRoot) panelRoot.gameObject.SetActive(true);

        yield return new WaitForSeconds(showSeconds);

        if (msg) msg.text = "";
        if (panelRoot) panelRoot.gameObject.SetActive(false);
    }

    // Útil si quieres mostrar texto custom desde otros lados
    public void ShowRaw(string text)
    {
        if (msg) msg.text = text;
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    TMPro.TextMeshProUGUI _simpleLabel;
    GameObject _simpleRoot;
    Coroutine _simpleCo;

    public void ShowSimple(string msg, float seconds = 1.5f)
    {
        // fallback: usa el propio GO como root y busca un TMP hijo
        if (_simpleRoot == null) _simpleRoot = gameObject;
        if (_simpleLabel == null) _simpleLabel = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

        if (_simpleLabel == null)
        {
            Debug.LogWarning("[ResultMessageUI] No encontré un TextMeshProUGUI para ShowSimple.");
            return;
        }

        _simpleLabel.text = msg;
        _simpleRoot.SetActive(true);

        if (_simpleCo != null) StopCoroutine(_simpleCo);
        _simpleCo = StartCoroutine(HideSimple(seconds));
    }

    System.Collections.IEnumerator HideSimple(float s)
    {
        yield return new WaitForSeconds(s);
        if (_simpleRoot) _simpleRoot.SetActive(false);
    }
}