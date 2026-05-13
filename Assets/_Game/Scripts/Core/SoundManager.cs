using UnityEngine;
using System.Collections;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Optional — drag real clips here to replace generated sounds")]
    public AudioClip buildPlaceClip;
    public AudioClip milestoneClip;
    public AudioClip ambientClip;
    public AudioClip droneHumClip;
    public AudioClip uiClickClip;
    public AudioClip eventAlertClip;
    public AudioClip upgradeClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume     = 0.6f;
    [Range(0f, 1f)] public float ambientVolume  = 0.15f;
    [Range(0f, 1f)] public float uiVolume       = 0.35f;

    private AudioSource sfxSource;
    private AudioSource ambientSource;
    private AudioSource uiSource;

    private AudioClip generatedPlace;
    private AudioClip generatedMilestone;
    private AudioClip generatedAmbient;
    private AudioClip generatedUIClick;
    private AudioClip generatedEventAlert;
    private AudioClip generatedUpgrade;

    // Dynamic ambient: pitch slowly rises as more buildings are placed
    private int _lastBuildingCount = 0;
    private float _targetAmbientPitch = 1f;

    void Awake()
    {
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop       = true;
        ambientSource.volume     = ambientVolume;
        ambientSource.playOnAwake = false;

        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
    }

    void Start()
    {
        generatedPlace      = GenerateBlip(700f, 300f, 0.15f);
        generatedMilestone  = GenerateChord();
        generatedAmbient    = GenerateAmbient();
        generatedUIClick    = GenerateUIClick();
        generatedEventAlert = GenerateEventAlert();
        generatedUpgrade    = GenerateUpgrade();

        ambientSource.clip = ambientClip != null ? ambientClip : generatedAmbient;
        ambientSource.Play();

        MilestoneManager.Instance.OnMilestoneCompleted += _ => PlayMilestone();
    }

    void Update()
    {
        // Smoothly shift ambient pitch based on building count (uses static registry, no FindObjects)
        int count = Building.Count;
        if (count != _lastBuildingCount)
        {
            _lastBuildingCount = count;
            // Each 4 buildings raises pitch by ~0.04, max +0.3
            _targetAmbientPitch = Mathf.Clamp(1f + count * 0.01f, 1f, 1.3f);
        }
        ambientSource.pitch = Mathf.Lerp(ambientSource.pitch, _targetAmbientPitch, Time.deltaTime * 0.5f);

        // Volume also gently rises with building activity
        float targetVol = Mathf.Clamp(ambientVolume + count * 0.004f, ambientVolume, ambientVolume * 2.5f);
        ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetVol, Time.deltaTime * 0.3f);
    }

    // ── Public SFX API ────────────────────────────────────────────────────

    public void PlayBuildPlace()
        => sfxSource.PlayOneShot(buildPlaceClip != null ? buildPlaceClip : generatedPlace, sfxVolume);

    public void PlayMilestone()
        => StartCoroutine(PlayFanfare());

    public void PlayUIClick()
        => uiSource.PlayOneShot(uiClickClip != null ? uiClickClip : generatedUIClick, uiVolume);

    public void PlayEventAlert()
        => sfxSource.PlayOneShot(eventAlertClip != null ? eventAlertClip : generatedEventAlert, sfxVolume);

    public void PlayUpgrade()
        => sfxSource.PlayOneShot(upgradeClip != null ? upgradeClip : generatedUpgrade, sfxVolume * 0.9f);

    // ── Milestone fanfare (ascending staccato + final chord) ─────────────

    IEnumerator PlayFanfare()
    {
        // Staccato ascent
        float[] ascent = { 440f, 523f, 659f, 784f };
        foreach (float freq in ascent)
        {
            sfxSource.PlayOneShot(GenerateTone(freq, 0.12f, 0.55f), sfxVolume);
            yield return new WaitForSeconds(0.08f);
        }
        yield return new WaitForSeconds(0.05f);
        // Big landing chord
        sfxSource.PlayOneShot(milestoneClip != null ? milestoneClip : generatedMilestone, sfxVolume * 1.2f);
    }

    // ── Procedural audio generators ───────────────────────────────────────

    // Frequency-sweep blip (building place)
    AudioClip GenerateBlip(float startFreq, float endFreq, float duration)
    {
        int rate    = 44100;
        int samples = (int)(rate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / rate;
            float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
            float env  = Mathf.Exp(-t * 18f);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.6f;
        }
        var clip = AudioClip.Create("Blip", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Short, crisp UI click
    AudioClip GenerateUIClick()
    {
        int rate    = 44100;
        int samples = (int)(rate * 0.04f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / rate;
            float env = Mathf.Exp(-t * 80f);
            data[i] = (Mathf.Sin(2 * Mathf.PI * 1800f * t)
                      + Mathf.Sin(2 * Mathf.PI * 3200f * t) * 0.3f) * env * 0.4f;
        }
        var clip = AudioClip.Create("UIClick", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Ominous descending alert for random events
    AudioClip GenerateEventAlert()
    {
        int rate    = 44100;
        int samples = (int)(rate * 0.5f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / rate;
            float freq = Mathf.Lerp(600f, 200f, t / 0.5f);
            float env  = Mathf.Exp(-t * 4f);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.5f
                    + Mathf.Sin(2 * Mathf.PI * freq * 0.5f * t) * env * 0.2f;
        }
        var clip = AudioClip.Create("EventAlert", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Bright ascending sweep for upgrades
    AudioClip GenerateUpgrade()
    {
        int rate    = 44100;
        int samples = (int)(rate * 0.25f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / rate;
            float freq = Mathf.Lerp(400f, 1200f, t / 0.25f);
            float env  = Mathf.Exp(-t * 8f);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.5f;
        }
        var clip = AudioClip.Create("Upgrade", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Single sustained tone
    AudioClip GenerateTone(float freq, float duration, float volume)
    {
        int rate    = 44100;
        int samples = (int)(rate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / rate;
            float env = Mathf.Exp(-t * 10f);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * volume;
        }
        var clip = AudioClip.Create("Tone", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Layered chord for milestone
    AudioClip GenerateChord()
    {
        int rate    = 44100;
        int samples = (int)(rate * 1.2f);
        float[] data = new float[samples];
        float[] freqs = { 523f, 659f, 784f, 1047f };
        foreach (float freq in freqs)
        {
            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / rate;
                float env = Mathf.Exp(-t * 2.5f);
                data[i] += Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.22f;
            }
        }
        var clip = AudioClip.Create("Chord", samples, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Low electronic ambient hum
    AudioClip GenerateAmbient()
    {
        int rate    = 44100;
        int samples = rate * 6;   // 6-second loop
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            data[i] = Mathf.Sin(2 * Mathf.PI * 55f  * t) * 0.28f
                    + Mathf.Sin(2 * Mathf.PI * 110f  * t) * 0.14f
                    + Mathf.Sin(2 * Mathf.PI * 82f   * t) * 0.10f
                    + Mathf.Sin(2 * Mathf.PI * 220f  * t) * 0.05f
                    // Slow modulation
                    * (0.8f + 0.2f * Mathf.Sin(2 * Mathf.PI * 0.2f * t));
        }
        var clip = AudioClip.Create("Ambient", samples, 1, rate, true);
        clip.SetData(data, 0);
        return clip;
    }
}
