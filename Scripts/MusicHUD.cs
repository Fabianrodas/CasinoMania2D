using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MusicHUD : MonoBehaviour
{
    public static MusicHUD Instance;

    [SerializeField] private Image musicIcon;      // hijo "musica"
    [SerializeField] private Slider volumeSlider;  // hijo "Slider" o "VolumeSlider"
    [SerializeField] private int sortingOrder = 5000;

    float _frozenScale = 1f;

    // guardamos las escalas que dejaste en el editor
    Vector3 _rootScale = Vector3.one;
    Vector3 _iconScale = Vector3.one;
    Vector3 _sliderScale = Vector3.one;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (transform.parent != null) transform.SetParent(null, false);

        // --- Canvas en root (o subimos el que haya en hijos) ---
        Canvas canvas = GetComponent<Canvas>();
        if (!canvas)
        {
            var childCanvas = GetComponentInChildren<Canvas>(true);
            if (childCanvas)
            {
                childCanvas.transform.SetParent(transform, false);
                canvas = childCanvas;
            }
            else
            {
                canvas = gameObject.AddComponent<Canvas>();
            }
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        // Scaler + Raycaster en el mismo objeto del Canvas
        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        if (!canvas.GetComponent<GraphicRaycaster>()) canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Congelar factor de escala según el CanvasScaler de la escena (menú)
        _frozenScale = CaptureMenuScaleFactor();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = _frozenScale;

        // Root anclado arriba‑derecha (posición base)
        var rt = canvas.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);

        AutoWire();

        // —— GUARDAR SOLO LAS ESCALAS QUE TÚ PUSISTE ——
        _rootScale = transform.localScale;
        if (musicIcon)   _iconScale   = musicIcon.rectTransform.localScale;
        if (volumeSlider) _sliderScale = volumeSlider.GetComponent<RectTransform>().localScale;

        // Ajustar anchors/posiciones (sin tocar sizeDelta)
        FixChildrenLayout();

        DontDestroyOnLoad(gameObject);
        BindToMusicManager();

        SceneManager.sceneLoaded += OnSceneLoadedEnsureEventSystem;
    }

    void OnDestroy()  => SceneManager.sceneLoaded -= OnSceneLoadedEnsureEventSystem;
    void OnEnable()   => BindToMusicManager();

    float CaptureMenuScaleFactor()
    {
        var sceneScaler = FindObjectOfType<CanvasScaler>();
        if (sceneScaler && sceneScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            var refRes = sceneScaler.referenceResolution;
            float match = sceneScaler.matchWidthOrHeight;
            float w = Screen.width, h = Screen.height;
            float logW = Mathf.Log(w / refRes.x, 2);
            float logH = Mathf.Log(h / refRes.y, 2);
            float logWeighted = Mathf.Lerp(logW, logH, match);
            return Mathf.Pow(2, logWeighted);
        }
        return 1f;
    }

    void AutoWire()
    {
        if (!musicIcon)    musicIcon    = transform.Find("musica")?.GetComponent<Image>();
        if (!volumeSlider) volumeSlider = transform.Find("Slider")?.GetComponent<Slider>()
                                ?? transform.Find("VolumeSlider")?.GetComponent<Slider>();
    }

    void FixChildrenLayout()
    {
        if (musicIcon)
        {
            var rt = musicIcon.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f); // pivote centrado vertical
            rt.anchoredPosition = new Vector2(-170f, -30f);
            rt.localScale = _iconScale;
            musicIcon.raycastTarget = false;
        }

        if (volumeSlider)
        {
            var rt = volumeSlider.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-24f, -20f);
            rt.localScale = _sliderScale; // tu escala personalizada
            volumeSlider.minValue = 0; volumeSlider.maxValue = 1;
        }

        // aplicar escala del root que tenías en el editor
        transform.localScale = _rootScale;
    }

    void BindToMusicManager()
    {
        if (!volumeSlider || MusicManager.Instance == null || MusicManager.Instance.Audio == null) return;
        volumeSlider.onValueChanged.RemoveListener(MusicManager.Instance.SetVolume);
        volumeSlider.value = MusicManager.Instance.Audio.volume;
        volumeSlider.onValueChanged.AddListener(MusicManager.Instance.SetVolume);
    }

    void OnSceneLoadedEnsureEventSystem(Scene s, LoadSceneMode m)
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // Reafirmar orden y factor de escala
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas) { canvas.overrideSorting = true; canvas.sortingOrder = sortingOrder; }
        var scaler = canvas ? canvas.GetComponent<CanvasScaler>() : null;
        if (scaler) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = _frozenScale; }

        // Reaplicar SOLO tus escalas personalizadas
        transform.localScale = _rootScale;
        if (musicIcon)    musicIcon.rectTransform.localScale = _iconScale;
        if (volumeSlider) volumeSlider.GetComponent<RectTransform>().localScale = _sliderScale;

        BindToMusicManager();
    }
}