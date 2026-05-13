using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Shows "+N Resource" pop-up numbers in screen space when drones deposit resources.
/// Pooled — re-uses up to <see cref="PoolSize"/> label GameObjects to avoid GC churn
/// from frequent drone deposits.  Auto-instantiates.
/// </summary>
public class FloatingText : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<FloatingText>() == null)
            new GameObject("_FloatingText").AddComponent<FloatingText>();
    }

    const int   PoolSize    = 24;
    const float Lifetime    = 1.6f;
    const float RisePixels  = 55f;
    const float FadeStart   = 0.6f;       // fraction of lifetime when fade begins

    Canvas _canvas;

    // Pool of inactive label objects ready to reuse
    readonly Stack<GameObject> _pool = new();
    // Currently animating labels (so OnDestroy can clean them up if scene reloads)
    readonly List<GameObject>  _active = new();

    void Start()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvas = c; break; }

        ResourceManager.OnResourceCollected += OnCollected;
    }

    void OnDestroy() => ResourceManager.OnResourceCollected -= OnCollected;

    void OnCollected(ResourceType type, float amount, Vector3 worldPos)
    {
        if (_canvas == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 0.8f);
        if (screenPos.z < 0f) return;   // behind camera

        StartCoroutine(Animate(type, amount, screenPos));
    }

    // ── Pool ──────────────────────────────────────────────────────────────

    GameObject Acquire()
    {
        if (_pool.Count > 0)
        {
            var go = _pool.Pop();
            go.SetActive(true);
            return go;
        }
        return BuildNew();
    }

    void Release(GameObject go)
    {
        if (go == null) return;
        _active.Remove(go);
        if (_pool.Count >= PoolSize) { Destroy(go); return; }
        go.SetActive(false);
        _pool.Push(go);
    }

    GameObject BuildNew()
    {
        var go = new GameObject("FloatLabel");
        go.transform.SetParent(_canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(120f, 24f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize       = 13f;
        tmp.fontStyle      = FontStyles.Bold;
        tmp.alignment      = TextAlignmentOptions.Center;
        tmp.raycastTarget  = false;   // decorative — never block clicks
        return go;
    }

    // ── Animation ─────────────────────────────────────────────────────────

    IEnumerator Animate(ResourceType type, float amount, Vector3 startScreen)
    {
        GameObject go = Acquire();
        _active.Add(go);

        var rt  = (RectTransform)go.transform;
        var tmp = go.GetComponent<TextMeshProUGUI>();

        tmp.text   = $"+{Mathf.CeilToInt(amount)} {type}";
        tmp.color  = ResourceColor(type);
        tmp.alpha  = 1f;
        rt.anchoredPosition = new Vector2(startScreen.x, startScreen.y);
        Vector2 startPos = rt.anchoredPosition;

        float elapsed = 0f;
        while (elapsed < Lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Lifetime;
            rt.anchoredPosition = startPos + Vector2.up * (RisePixels * t);
            tmp.alpha = t < FadeStart ? 1f : 1f - ((t - FadeStart) / (1f - FadeStart));
            yield return null;
        }

        Release(go);
    }

    // ── Palette ───────────────────────────────────────────────────────────

    static Color ResourceColor(ResourceType type) => type switch
    {
        ResourceType.Scrap      => new Color(0.95f, 0.75f, 0.30f),
        ResourceType.Energy     => new Color(1.00f, 0.65f, 0.10f),
        ResourceType.Polymer    => new Color(0.75f, 0.35f, 1.00f),
        ResourceType.Data       => new Color(0.10f, 0.85f, 1.00f),
        ResourceType.Population => new Color(0.20f, 1.00f, 0.50f),
        ResourceType.Nano       => new Color(1.00f, 1.00f, 0.40f),
        _                       => Color.white,
    };
}
