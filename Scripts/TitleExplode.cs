using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleExplode : MonoBehaviour
{
    [Header("Timing")]
    public float delayBetweenLetters = 0.06f;
    public float letterDuration = 0.35f;

    [Header("Efecto tipo 'explosión'")]
    public float startScale = 0.25f;
    public float overshootScale = 1.15f;
    public Vector2 startPosJitter = new Vector2(8f, 8f);
    public float startRotJitter = 10f;

    [Header("Disparo")]
    public bool playOnEnable = true;

    private UIButtonPulseCredits[] pulses;

    private struct LetterState
    {
        public RectTransform rt;
        public CanvasGroup cg;
        public Vector3 basePos;
        public Quaternion baseRot;
    }

    private readonly List<LetterState> letters = new List<LetterState>();
    private Coroutine playing;

    void Awake()
    {
        letters.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            var rt = child as RectTransform;
            if (rt == null) continue;
            if (child.GetComponent<Image>() == null) continue;

            var cg = child.GetComponent<CanvasGroup>();
            if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();

            letters.Add(new LetterState
            {
                rt = rt,
                cg = cg,
                basePos = rt.anchoredPosition3D,
                baseRot = rt.localRotation
            });
        }
    }

    void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        pulses = GetComponentsInChildren<UIButtonPulseCredits>(true);
        foreach (var p in pulses)
        {
            p.SetBaseScale(Vector3.one);   
            p.SetPulse(false);        
            p.transform.localScale = Vector3.one;
        }

        if (playing != null) StopCoroutine(playing);
        playing = StartCoroutine(PlayRoutine());
    }

    public void StopAndResetInstant()
    {
        if (playing != null) StopCoroutine(playing);
        foreach (var L in letters)
        {
            L.rt.localScale = Vector3.one;
            L.rt.anchoredPosition3D = L.basePos;
            L.rt.localRotation = L.baseRot;
            L.cg.alpha = 1f;
        }
    }

    private IEnumerator PlayRoutine()
    {
        foreach (var L in letters)
        {
            Vector2 jitter = new Vector2(
                Random.Range(-startPosJitter.x, startPosJitter.x),
                Random.Range(-startPosJitter.y, startPosJitter.y)
            );

            float rot = Random.Range(-startRotJitter, startRotJitter);

            L.rt.localScale = Vector3.one * startScale;
            L.rt.anchoredPosition3D = L.basePos + (Vector3)jitter;
            L.rt.localRotation = Quaternion.Euler(0, 0, rot);
            L.cg.alpha = 0f;
        }

        for (int i = 0; i < letters.Count; i++)
        {
            StartCoroutine(AnimateOne(letters[i]));
            yield return new WaitForSeconds(delayBetweenLetters);
        }

        yield return new WaitForSeconds(letterDuration);

        if (pulses != null)
        {
            foreach (var p in pulses){
                p.SetBaseScale(Vector3.one);
                p.SetPulse(true);
            }          
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }

    private IEnumerator AnimateOne(LetterState L)
    {
        float t = 0f;

        Vector3 startPos = L.rt.anchoredPosition3D;
        Quaternion startRot = L.rt.localRotation;
        float startAlpha = 0f;

        Vector3 endPos = L.basePos;
        Quaternion endRot = L.baseRot;

        float half = letterDuration * 0.6f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / half);

            float a = EaseOutCubic(u);
            L.cg.alpha = Mathf.Lerp(startAlpha, 1f, a);

            L.rt.anchoredPosition3D = Vector3.Lerp(startPos, endPos, a);
            L.rt.localRotation = Quaternion.Slerp(startRot, endRot, a);

            float s = Mathf.Lerp(startScale, overshootScale, EaseOutBack(u));
            L.rt.localScale = Vector3.one * s;

            yield return null;
        }

        t = 0f;
        float settle = letterDuration - half;
        while (t < settle)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / settle);
            float s = Mathf.Lerp(overshootScale, 1f, EaseOutCubic(u));
            L.rt.localScale = Vector3.one * s;
            yield return null;
        }

        L.rt.localScale = Vector3.one;
        L.rt.anchoredPosition3D = endPos;
        L.rt.localRotation = endRot;
        L.cg.alpha = 1f;
    }

    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    private static float EaseOutBack(float x, float k = 1.70158f)
    {
        float c1 = k;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
