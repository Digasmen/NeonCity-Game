using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Slide-in toast banners for milestone completions and building unlocks.
/// Stacks from the top-right corner, uses unscaled time so they work while paused.
/// Auto-instantiates at scene load — no scene wiring required.
/// </summary>
public class ToastUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<ToastUI>() == null)
            new GameObject("_ToastUI").AddComponent<ToastUI>();
    }

    const float W           = 300f;
    const float H           = 58f;
    const float Gap         = 6f;
    const float MarginTop   = 14f;
    const float MarginRight = 14f;
    const float SlideTime   = 0.28f;
    const float HoldTime    = 2.8f;
    const float FadeTime    = 0.25f;

    Canvas _canvas;
    int    _stackCount;

    // ── Bootstrap ─────────────────────────────────────────────────────────

    void Start()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { _canvas = c; break; }
        if (_canvas == null) return;

        if (MilestoneManager.Instance != null)
            MilestoneManager.Instance.OnMilestoneCompleted += OnMilestoneCompleted;

        BuildMenuUI.OnBuildingUnlocked += OnBuildingUnlocked;
        SaveManager.OnSaved            += OnAutoSaved;
    }

    void OnDestroy()
    {
        if (MilestoneManager.Instance != null)
            MilestoneManager.Instance.OnMilestoneCompleted -= OnMilestoneCompleted;

        BuildMenuUI.OnBuildingUnlocked -= OnBuildingUnlocked;
        SaveManager.OnSaved            -= OnAutoSaved;
    }

    void OnMilestoneCompleted(MilestoneData m)
        => Enqueue("MILESTONE COMPLETE", m.completionMessage.ToUpper(), UIUtils.Green);

    void OnBuildingUnlocked(BuildingData b)
        => Enqueue("BUILDING UNLOCKED", b.buildingName.ToUpper(), UIUtils.Cyan);

    void OnAutoSaved()
        => Enqueue("AUTO-SAVE", "PROGRESS SAVED", new Color(0.55f, 0.55f, 0.65f));

    // ── Public API ────────────────────────────────────────────────────────

    public void Enqueue(string title, string body, Color accent)
    {
        if (_canvas == null) return;
        StartCoroutine(Run(title, body, accent, _stackCount++));
    }

    // ── Coroutine ─────────────────────────────────────────────────────────

    IEnumerator Run(string title, string body, Color accent, int slot)
    {
        var go = new GameObject("Toast");
        go.transform.SetParent(_canvas.transform, false);

        // ── RectTransform ─────────────────────────────────────────────────
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.sizeDelta        = new Vector2(W, H);
        float targetY       = -(MarginTop + slot * (H + Gap));
        rt.anchoredPosition = new Vector2(W + 20f, targetY);   // starts off-screen right

        UIUtils.Rounded(go, new Color(0.04f, 0.07f, 0.16f, 0.97f), 10);

        // Left accent bar
        var bar    = new GameObject("Bar"); bar.transform.SetParent(go.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = accent;
        barImg.rectTransform.anchorMin        = Vector2.zero;
        barImg.rectTransform.anchorMax        = new Vector2(0f, 1f);
        barImg.rectTransform.pivot            = new Vector2(0f, 0.5f);
        barImg.rectTransform.anchoredPosition = Vector2.zero;
        barImg.rectTransform.sizeDelta        = new Vector2(3f, 0f);

        // Top glow line
        var glow    = new GameObject("Glow"); glow.transform.SetParent(go.transform, false);
        var glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(accent.r, accent.g, accent.b, 0.4f);
        glowImg.rectTransform.anchorMin        = new Vector2(0f, 1f);
        glowImg.rectTransform.anchorMax        = new Vector2(1f, 1f);
        glowImg.rectTransform.pivot            = new Vector2(0.5f, 1f);
        glowImg.rectTransform.anchoredPosition = Vector2.zero;
        glowImg.rectTransform.sizeDelta        = new Vector2(0f, 1f);

        // Title
        var titleLbl = UIUtils.Label(go.transform, "Title", title, 8.5f,
            accent, FontStyles.Bold, TextAlignmentOptions.Left);
        var tRT = titleLbl.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.52f); tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(13f, 0f);   tRT.offsetMax = new Vector2(-10f, -5f);

        // Body
        var bodyLbl = UIUtils.Label(go.transform, "Body", body, 12f,
            UIUtils.TextMain, FontStyles.Bold, TextAlignmentOptions.Left);
        var bRT = bodyLbl.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0f, 0f);   bRT.anchorMax = new Vector2(1f, 0.56f);
        bRT.offsetMin = new Vector2(13f, 4f);  bRT.offsetMax = new Vector2(-10f, 0f);

        var cg = go.AddComponent<CanvasGroup>();

        // ── Slide in (ease-out cubic) ─────────────────────────────────────
        float elapsed = 0f;
        while (elapsed < SlideTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / SlideTime), 3f);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(W + 20f, -MarginRight, t), targetY);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(-MarginRight, targetY);

        // ── Hold ──────────────────────────────────────────────────────────
        yield return new WaitForSecondsRealtime(HoldTime);

        // ── Fade out ──────────────────────────────────────────────────────
        elapsed = 0f;
        while (elapsed < FadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(elapsed / FadeTime);
            yield return null;
        }

        _stackCount--;
        Destroy(go);
    }
}
