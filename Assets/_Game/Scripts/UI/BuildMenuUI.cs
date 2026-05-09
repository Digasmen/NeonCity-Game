using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BuildMenuUI : MonoBehaviour
{
    [Header("Buildings to show in menu")]
    public List<BuildingData> availableBuildings;

    private GameObject menuPanel;
    private bool isOpen = false;

    private List<(BuildingData data, Image cardImage, TextMeshProUGUI costText)> cards = new();

    void Update()
    {
        if (!isOpen) return;
        foreach (var (data, cardImage, costText) in cards)
        {
            bool canAfford = ResourceManager.Instance.CanAfford(ResourceType.Scrap, data.scrapCost);
            cardImage.color = canAfford ? new Color(0.1f, 0.1f, 0.2f, 1f) : new Color(0.3f, 0.05f, 0.05f, 1f);
            costText.color = canAfford ? new Color(0.4f, 1f, 0.6f) : new Color(1f, 0.3f, 0.3f);
        }
    }

    void Start()
    {
        CreateUI();
    }

    void CreateUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // BUILD button
        GameObject btnGO = CreateButton(canvas.transform, "BuildMenuButton", "BUILD", new Vector2(0, 40), new Vector2(120, 50));
        btnGO.GetComponent<Button>().onClick.AddListener(ToggleMenu);

        // Menu panel (hidden by default)
        menuPanel = new GameObject("BuildMenuPanel");
        menuPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 95);
        panelRect.sizeDelta = new Vector2(0, 140);

        Image panelImg = menuPanel.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.04f, 0.1f, 0.95f);

        HorizontalLayoutGroup layout = menuPanel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;

        // Building cards
        foreach (var data in availableBuildings)
            CreateBuildingCard(data);

        menuPanel.SetActive(false);
    }

    void CreateBuildingCard(BuildingData data)
    {
        GameObject card = new GameObject(data.buildingName + "_Card");
        card.transform.SetParent(menuPanel.transform, false);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.2f, 1f);

        Button btn = card.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.2f, 0.2f, 0.4f, 1f);
        cb.pressedColor = new Color(0.05f, 0.5f, 0.8f, 1f);
        btn.colors = cb;

        // Name label — top half of card
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(4, 2);
        nameRect.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = data.buildingName;
        nameText.fontSize = 11;
        nameText.enableWordWrapping = true;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;

        // Cost label — bottom half of card
        GameObject costGO = new GameObject("Cost");
        costGO.transform.SetParent(card.transform, false);
        RectTransform costRect = costGO.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0, 0);
        costRect.anchorMax = new Vector2(1, 0.5f);
        costRect.offsetMin = new Vector2(4, 2);
        costRect.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI costText = costGO.AddComponent<TextMeshProUGUI>();
        costText.text = $"{data.scrapCost} Scrap";
        costText.fontSize = 11;
        costText.alignment = TextAlignmentOptions.Center;
        costText.color = new Color(0.4f, 1f, 0.6f);

        cards.Add((data, bg, costText));

        BuildingData captured = data;
        btn.onClick.AddListener(() =>
        {
            BuildingPlacer.Instance.StartPlacement(captured);
            CloseMenu();
        });
    }

    GameObject CreateButton(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.05f, 0.4f, 0.8f, 1f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.1f, 0.6f, 1f, 1f);
        cb.pressedColor = new Color(0.02f, 0.25f, 0.5f, 1f);
        btn.colors = cb;

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return go;
    }

    void ToggleMenu()
    {
        isOpen = !isOpen;
        menuPanel.SetActive(isOpen);
    }

    void CloseMenu()
    {
        isOpen = false;
        menuPanel.SetActive(false);
    }
}
