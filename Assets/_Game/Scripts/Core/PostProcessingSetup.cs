using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingSetup : MonoBehaviour
{
    void Start()
    {
        GameObject volumeGO = new GameObject("GlobalVolume");
        Volume volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.value = 0.8f;
        bloom.intensity.value = 2f;
        bloom.scatter.value = 0.6f;
        bloom.tint.value = new Color(0.6f, 0.8f, 1f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.contrast.value = 25f;
        color.saturation.value = 20f;
        color.colorFilter.value = new Color(0.85f, 0.9f, 1f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.value = 0.35f;
        vignette.smoothness.value = 0.5f;
        vignette.color.value = new Color(0f, 0f, 0.1f);

        volume.profile = profile;
    }
}
