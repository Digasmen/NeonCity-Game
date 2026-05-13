using UnityEngine;
using UnityEditor;
using System.IO;

public static class GameDevTools
{
    [MenuItem("NeonCity/New Game (Clear Save)")]
    public static void ClearSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "neoncity.save");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save cleared: " + path);
        }
        else
        {
            Debug.Log("No save file found at: " + path);
        }
    }
}

public static class SoundExporter
{
    [MenuItem("NeonCity/Export Generated Sounds")]
    public static void ExportAll()
    {
        string folder = "Assets/_Game/Audio";

        ExportWav(folder, "BuildPlace",   GenerateBlip(700f, 300f, 0.15f));
        ExportWav(folder, "Milestone",    GenerateChord());
        ExportWav(folder, "Ambient",      GenerateAmbient());
        ExportWav(folder, "NoteC5",       GenerateTone(523f, 0.4f, 0.5f));
        ExportWav(folder, "NoteE5",       GenerateTone(659f, 0.4f, 0.5f));
        ExportWav(folder, "NoteG5",       GenerateTone(784f, 0.4f, 0.5f));

        AssetDatabase.Refresh();
        Debug.Log("Sounds exported to " + folder);
    }

    static void ExportWav(string folder, string name, float[] data)
    {
        string path = Path.Combine(Application.dataPath, folder.Replace("Assets/", ""), name + ".wav");
        using FileStream fs = new FileStream(path, FileMode.Create);
        using BinaryWriter bw = new BinaryWriter(fs);

        int sampleRate = 44100;
        int channels   = 1;
        int bitsPerSample = 16;
        int byteRate   = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize   = data.Length * blockAlign;

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)bitsPerSample);

        // data chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);
        foreach (float s in data)
            bw.Write((short)Mathf.Clamp(s * 32767f, -32768f, 32767f));
    }

    static float[] GenerateBlip(float startFreq, float endFreq, float duration)
    {
        int rate = 44100;
        int samples = (int)(rate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-t * 18f) * 0.6f;
        }
        return data;
    }

    static float[] GenerateTone(float freq, float duration, float volume)
    {
        int rate = 44100;
        int samples = (int)(rate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-t * 4f) * volume;
        }
        return data;
    }

    static float[] GenerateChord()
    {
        int rate = 44100;
        int samples = (int)(rate * 0.8f);
        float[] data = new float[samples];
        float[] freqs = { 523f, 659f, 784f };
        foreach (float freq in freqs)
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                data[i] += Mathf.Sin(2 * Mathf.PI * freq * t) * Mathf.Exp(-t * 3f) * 0.25f;
            }
        return data;
    }

    static float[] GenerateAmbient()
    {
        int rate = 44100;
        int samples = rate * 4;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / rate;
            data[i] = Mathf.Sin(2 * Mathf.PI * 60f  * t) * 0.3f
                    + Mathf.Sin(2 * Mathf.PI * 120f * t) * 0.15f
                    + Mathf.Sin(2 * Mathf.PI * 90f  * t) * 0.1f;
        }
        return data;
    }
}
