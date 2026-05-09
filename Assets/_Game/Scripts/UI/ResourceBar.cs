using UnityEngine;
using TMPro;

public class ResourceBar : MonoBehaviour
{
    [Header("Resource Labels")]
    public TextMeshProUGUI scrapText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI polymerText;
    public TextMeshProUGUI dataText;
    public TextMeshProUGUI populationText;

    void Update()
    {
        UpdateLabel(scrapText,      ResourceType.Scrap);
        UpdateLabel(energyText,     ResourceType.Energy);
        UpdateLabel(polymerText,    ResourceType.Polymer);
        UpdateLabel(dataText,       ResourceType.Data);
        UpdateLabel(populationText, ResourceType.Population);
    }

    void UpdateLabel(TextMeshProUGUI label, ResourceType type)
    {
        if (label == null) return;
        float amount = ResourceManager.Instance.Get(type);
        float rate = ResourceManager.Instance.GetRate(type);
        string rateStr = rate > 0 ? $" <color=#00FF88>+{rate:0}/m</color>" : rate < 0 ? $" <color=#FF4444>{rate:0}/m</color>" : "";
        label.text = $"{type}\n<b>{amount:0}</b>{rateStr}";
    }
}
