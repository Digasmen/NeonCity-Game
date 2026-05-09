using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectivesUI : MonoBehaviour
{
    private GameObject panel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descText;
    private TextMeshProUGUI progressText;
    private Image progressBar;
    private GameObject notificationBanner;
    private TextMeshProUGUI notificationText;
    private float notificationTimer;

    void Start()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        CreateUI(canvas.transform);

        MilestoneManager.Instance.OnMilestoneChanged += OnMilestoneChanged;
        MilestoneManager.Instance.OnMilestoneCompleted += OnMilestoneCompleted;
    }

    void CreateUI(Transform canvasTransform)
    {
        // Objectives panel — left side
        panel = new GameObject("ObjectivesPanel");
        panel.transform.SetParent(canvasTransform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(10, 0);
        rect.sizeDelta = new Vector2(200, 110);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.12f, 0.92f);

        // Title
        titleText = CreateLabel(panel.transform, "OBJECTIVES", 13, FontStyles.Bold,
            new Vector2(0, 0.78f), new Vector2(1, 1), new Color(0.4f, 0.8f, 1f));

        // Description
        descText = CreateLabel(panel.transform, "", 11, FontStyles.Normal,
            new Vector2(0, 0.42f), new Vector2(1, 0.75f), Color.white);
        descText.enableWordWrapping = true;

        // Progress text
        progressText = CreateLabel(panel.transform, "", 10, FontStyles.Normal,
            new Vector2(0, 0.25f), new Vector2(1, 0.42f), new Color(0.4f, 1f, 0.6f));

        // Progress bar background
        GameObject barBg = new GameObject("BarBg");
        barBg.transform.SetParent(panel.transform, false);
        RectTransform barBgRect = barBg.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.05f, 0.07f);
        barBgRect.anchorMax = new Vector2(0.95f, 0.22f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.1f, 0.2f, 1f);

        // Progress bar fill
        GameObject barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform fillRect = barFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        progressBar = barFill.AddComponent<Image>();
        progressBar.color = new Color(0.2f, 0.8f, 1f);

        // Notification banner (center screen)
        notificationBanner = new GameObject("NotificationBanner");
        notificationBanner.transform.SetParent(canvasTransform, false);
        RectTransform notifRect = notificationBanner.AddComponent<RectTransform>();
        notifRect.anchorMin = new Vector2(0.5f, 0.7f);
        notifRect.anchorMax = new Vector2(0.5f, 0.7f);
        notifRect.pivot = new Vector2(0.5f, 0.5f);
        notifRect.sizeDelta = new Vector2(300, 50);
        Image notifBg = notificationBanner.AddComponent<Image>();
        notifBg.color = new Color(0.05f, 0.4f, 0.2f, 0.95f);
        notificationText = CreateLabel(notificationBanner.transform, "", 14, FontStyles.Bold,
            Vector2.zero, Vector2.one, Color.white);
        notificationBanner.SetActive(false);
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, float size, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(6, 2);
        r.offsetMax = new Vector2(-6, -2);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        return tmp;
    }

    void Update()
    {
        if (MilestoneManager.Instance.Current == null)
        {
            descText.text = "All objectives complete!";
            progressText.text = "";
            progressBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            return;
        }

        float progress = MilestoneManager.Instance.GetProgress();
        float current = ResourceManager.Instance.Get(MilestoneManager.Instance.Current.resourceType);
        float target = MilestoneManager.Instance.Current.targetAmount;

        progressText.text = $"{current:0} / {target:0}";
        progressBar.rectTransform.anchorMax = new Vector2(progress, 1f);

        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0)
                notificationBanner.SetActive(false);
        }
    }

    void OnMilestoneChanged(MilestoneData milestone)
    {
        if (milestone == null) return;
        descText.text = milestone.description;
    }

    void OnMilestoneCompleted(MilestoneData milestone)
    {
        string msg = string.IsNullOrEmpty(milestone.completionMessage)
            ? "Objective complete!"
            : milestone.completionMessage;
        notificationText.text = msg;
        notificationBanner.SetActive(true);
        notificationTimer = 3f;
    }
}
