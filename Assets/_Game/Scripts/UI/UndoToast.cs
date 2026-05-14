using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Top-center "UNDO" toast shown after each building placement.
/// Gives the player 3 real-time seconds to cancel the placement, refunding full Scrap cost.
/// Unscaled time so game pause does not reset the timer.
/// </summary>
public class UndoToast : MonoBehaviour
{
    public static UndoToast Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<UndoToast>() == null)
            new GameObject("_UndoToast").AddComponent<UndoToast>();
    }

    // ── Layout constants ──────────────────────────────────────────────────
    const float ToastW        = 220f;
    const float ToastH        = 44f;
    const float TopOffsetY    = 76f;   // below the resource bar
    const float UndoDuration  = 3f;    // real-time seconds

    // ── State ─────────────────────────────────────────────────────────────
    public struct Snapshot
    {
        public Building   building;
        public float      scrapRefund;
        public Vector2Int cell;
    }

    Snapshot     _pending;
    bool         _active = false;
    Coroutine    _countdownCo;

    // UI elements (rebuilt each time)
    GameObject   _toastGO;
    Image        _bar;
    Canvas       _canvas;

    void Awake() => Instance = this;

    void Start()
    {
        _canvas = FindFirstObjectByType<Canvas>();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Show the undo toast for a freshly placed building.</summary>
    public void ShowUndo(Snapshot snap)
    {
        Dismiss();           // replace any previous toast immediately

        _pending = snap;
        _active  = true;
        Build();

        _countdownCo = StartCoroutine(CountdownAndDismiss());
    }

    // ── Build UI ──────────────────────────────────────────────────────────

    void Build()
    {
        if (_canvas == null) return;

        _toastGO = new GameObject("UndoToast");
        _toastGO.transform.SetParent(_canvas.transform, false);

        var rt = _toastGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -TopOffsetY);
        rt.sizeDelta        = new Vector2(ToastW, ToastH);

        // Panel background
        UIUtils.Rounded(_toastGO, new Color(0.04f, 0.08f, 0.18f, 0.96f), UITheme.Rsm);

        // Cyan left accent stripe
        var stripe = new GameObject("Stripe");
        stripe.transform.SetParent(_toastGO.transform, false);
        var stripeImg = stripe.AddComponent<Image>();
        stripeImg.color = UITheme.Amber;
        stripeImg.raycastTarget = false;
        var sRT = stripeImg.rectTransform;
        sRT.anchorMin        = new Vector2(0f, 0.2f);
        sRT.anchorMax        = new Vector2(0f, 0.8f);
        sRT.pivot            = new Vector2(0f, 0.5f);
        sRT.anchoredPosition = Vector2.zero;
        sRT.sizeDelta        = new Vector2(2f, 0f);

        // Label
        var lbl = UIUtils.Label(_toastGO.transform, "Lbl",
            "UNDO PLACEMENT", UITheme.FCaption, UITheme.TextHi,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        lbl.raycastTarget = false;
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;  lRT.anchorMax = new Vector2(0.7f, 1f);
        lRT.offsetMin = new Vector2(12f, 0f);
        lRT.offsetMax = Vector2.zero;

        // "TAP" hint on the right
        var hint = UIUtils.Label(_toastGO.transform, "Hint",
            "TAP", UITheme.FCaption, UITheme.Amber,
            FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        hint.raycastTarget = false;
        var hRT = hint.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.7f, 0f);  hRT.anchorMax = Vector2.one;
        hRT.offsetMin = Vector2.zero;
        hRT.offsetMax = new Vector2(-10f, 0f);

        // Countdown bar at bottom
        var barBg = new GameObject("BarBg");
        barBg.transform.SetParent(_toastGO.transform, false);
        var barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(1f, 1f, 1f, 0.06f);
        barBgImg.raycastTarget = false;
        var bgRT = barBgImg.rectTransform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = new Vector2(1f, 0f);
        bgRT.pivot = new Vector2(0f, 0f);
        bgRT.anchoredPosition = Vector2.zero;
        bgRT.sizeDelta = new Vector2(0f, 3f);

        var barFill = new GameObject("Bar");
        barFill.transform.SetParent(_toastGO.transform, false);
        _bar = barFill.AddComponent<Image>();
        _bar.color = UITheme.Amber;
        _bar.raycastTarget = false;
        _bar.type = Image.Type.Filled;
        _bar.fillMethod = Image.FillMethod.Horizontal;
        _bar.fillAmount = 1f;
        var fRT = _bar.rectTransform;
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(1f, 0f);
        fRT.pivot = new Vector2(0f, 0f);
        fRT.anchoredPosition = Vector2.zero;
        fRT.sizeDelta = new Vector2(0f, 3f);

        // Click to undo
        var btn = _toastGO.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(ExecuteUndo);

        // Slide-down entrance
        StartCoroutine(SlideIn(rt, -TopOffsetY - ToastH, -TopOffsetY));
    }

    // ── Countdown coroutine ───────────────────────────────────────────────

    IEnumerator CountdownAndDismiss()
    {
        float start = Time.unscaledTime;
        while (Time.unscaledTime - start < UndoDuration)
        {
            if (_bar != null)
                _bar.fillAmount = 1f - (Time.unscaledTime - start) / UndoDuration;
            yield return null;
        }
        Dismiss();
    }

    // ── Actions ───────────────────────────────────────────────────────────

    void ExecuteUndo()
    {
        if (!_active) return;
        if (_countdownCo != null) { StopCoroutine(_countdownCo); _countdownCo = null; }

        Building b    = _pending.building;
        Vector2Int cell = _pending.cell;

        if (b != null)
        {
            // Restore grid state (Building.OnDestroy handles rate removal)
            GridManager.Instance.SetOccupied(cell.x, cell.y, false);
            GridManager.Instance.UnregisterBuilding(cell);
            Destroy(b.gameObject);

            // Refund Scrap
            ResourceManager.Instance.Add(ResourceType.Scrap, _pending.scrapRefund);

            // Refresh adjacency on neighbours after one frame (building OnDestroy needs to run first)
            StartCoroutine(RefreshNeighboursNextFrame(cell));
        }

        _active = false;
        DestroyToast();
    }

    void Dismiss()
    {
        if (_countdownCo != null) { StopCoroutine(_countdownCo); _countdownCo = null; }
        _active = false;
        DestroyToast();
    }

    void DestroyToast()
    {
        if (_toastGO != null) { Destroy(_toastGO); _toastGO = null; }
        _bar = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    IEnumerator SlideIn(RectTransform rt, float fromY, float toY)
    {
        float e = 0f;
        const float dur = 0.16f;
        while (e < dur)
        {
            e += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / dur), 3f);
            rt.anchoredPosition = new Vector2(0f, Mathf.Lerp(fromY, toY, t));
            yield return null;
        }
        rt.anchoredPosition = new Vector2(0f, toY);
    }

    IEnumerator RefreshNeighboursNextFrame(Vector2Int cell)
    {
        yield return null; // let Building.OnDestroy run
        foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Building nb = GridManager.Instance.GetBuildingAt(cell + d);
            nb?.RefreshAdjacency();
        }
    }
}
