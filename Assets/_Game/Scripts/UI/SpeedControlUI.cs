using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Bottom-left game speed panel: PAUSE / 1x / 2x.
/// Auto-instantiates at scene load — no scene wiring required.
/// Positioned above the build menu panel.
/// </summary>
public class SpeedControlUI : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (FindFirstObjectByType<SpeedControlUI>() == null)
            new GameObject("_SpeedControlUI").AddComponent<SpeedControlUI>();
    }

    static readonly (string label, float speed)[] _speeds =
    {
        ("II",  0f),
        ("1x",  1f),
        ("2x",  2f),
    };

    Graphic[]         _bgs;
    TextMeshProUGUI[] _lbls;
    int               _current = 1;   // default 1x

    // ── Bootstrap ─────────────────────────────────────────────────────────

    void Start()
    {
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
        if (canvas == null) return;
        Build(canvas.transform);
    }

    // ── Build ─────────────────────────────────────────────────────────────

    void Build(Transform root)
    {
        var panel = new GameObject("SpeedPanel");
        panel.transform.SetParent(root, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(10f, 180f);
        rt.sizeDelta        = new Vector2(114f, 30f);
        UIUtils.Rounded(panel, new Color(0.03f, 0.05f, 0.13f, 0.95f), 8);

        var hlg = panel.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment     = TextAnchor.MiddleCenter;
        hlg.spacing            = 2f;
        hlg.padding            = new RectOffset(3, 3, 3, 3);
        hlg.childControlWidth  = true;  hlg.childForceExpandWidth  = true;
        hlg.childControlHeight = true;  hlg.childForceExpandHeight = true;

        _bgs  = new Graphic[_speeds.Length];
        _lbls = new TextMeshProUGUI[_speeds.Length];

        for (int i = 0; i < _speeds.Length; i++)
        {
            int  idx    = i;
            bool active = (i == _current);
            var (lbl, _) = _speeds[i];

            var btnGO = new GameObject("Btn_" + lbl);
            btnGO.transform.SetParent(panel.transform, false);
            btnGO.AddComponent<RectTransform>();

            _bgs[i] = UIUtils.Rounded(btnGO,
                active ? new Color(0.04f, 0.24f, 0.52f, 1f)
                       : new Color(0.05f, 0.09f, 0.20f, 1f), 6);

            var btn = btnGO.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => SetSpeed(idx));

            // Hover highlight
            Graphic bg = _bgs[i];
            int     ci = i;
            var et = btnGO.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            { if (_current != ci) bg.color = new Color(0.07f, 0.16f, 0.34f, 1f); });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            { if (_current != ci) bg.color = new Color(0.05f, 0.09f, 0.20f, 1f); });
            et.triggers.Add(exit);

            _lbls[i] = UIUtils.Label(btnGO.transform, "L", lbl, 9.5f,
                active ? Color.white : new Color(0.45f, 0.60f, 0.80f),
                FontStyles.Bold, TextAlignmentOptions.Center);
            var lr = _lbls[i].GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.sizeDelta = Vector2.zero;
        }
    }

    // ── Set speed ─────────────────────────────────────────────────────────

    void SetSpeed(int index)
    {
        _current       = index;
        Time.timeScale = _speeds[index].speed;

        for (int i = 0; i < _speeds.Length; i++)
        {
            bool active = (i == index);
            _bgs[i].color  = active
                ? new Color(0.04f, 0.24f, 0.52f, 1f)
                : new Color(0.05f, 0.09f, 0.20f, 1f);
            _lbls[i].color = active ? Color.white : new Color(0.45f, 0.60f, 0.80f);
        }
    }
}
