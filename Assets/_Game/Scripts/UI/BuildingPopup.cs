using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingPopup : MonoBehaviour
{
    public static BuildingPopup Instance { get; private set; }

    private GameObject panel;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI refundText;
    private Building currentBuilding;
    private Canvas canvas;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        canvas = FindFirstObjectByType<Canvas>();
        CreatePopup();
    }

    void CreatePopup()
    {
        panel = new GameObject("BuildingPopup");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 90);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

        // Building name
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(panel.transform, false);
        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.6f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(8, 4);
        nameRect.offsetMax = new Vector2(-8, -4);
        nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 13;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // Remove button
        GameObject btnGO = new GameObject("RemoveButton");
        btnGO.transform.SetParent(panel.transform, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.1f, 0.05f);
        btnRect.anchorMax = new Vector2(0.9f, 0.55f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.6f, 0.1f, 0.1f, 1f);

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(RemoveBuilding);

        // Refund text inside button
        GameObject refundGO = new GameObject("RefundText");
        refundGO.transform.SetParent(btnGO.transform, false);
        RectTransform refundRect = refundGO.AddComponent<RectTransform>();
        refundRect.anchorMin = Vector2.zero;
        refundRect.anchorMax = Vector2.one;
        refundRect.offsetMin = Vector2.zero;
        refundRect.offsetMax = Vector2.zero;
        refundText = refundGO.AddComponent<TextMeshProUGUI>();
        refundText.fontSize = 11;
        refundText.alignment = TextAlignmentOptions.Center;
        refundText.color = Color.white;

        panel.SetActive(false);
    }

    public void Show(Building building)
    {
        currentBuilding = building;
        nameText.text = building.data.buildingName;
        refundText.text = $"Remove (+{building.data.scrapCost / 2} Scrap)";

        // Position popup above the building in screen space
        Vector3 screenPos = Camera.main.WorldToScreenPoint(building.transform.position + Vector3.up * 1.5f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localPos);
        panel.GetComponent<RectTransform>().anchoredPosition = localPos;

        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentBuilding = null;
    }

    void RemoveBuilding()
    {
        if (currentBuilding == null) return;

        int refund = currentBuilding.data.scrapCost / 2;
        ResourceManager.Instance.Add(ResourceType.Scrap, refund);

        Vector2Int cell = GridManager.Instance.GetGridPosition(currentBuilding.transform.position);
        GridManager.Instance.SetOccupied(cell.x, cell.y, false);

        Destroy(currentBuilding.gameObject);
        Hide();
    }
}
