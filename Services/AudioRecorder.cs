using NAudio.Wave;
using System.IO;

namespace TranscribeWin.Services;

public class AudioRecorder : IDisposable
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _memoryStream;
    private WaveFileWriter? _waveWriter;
    private bool _isRecording;
    private int _silentChunks;
    private const int SilenceThreshold = 500;     // RMS threshold for silence
    private const int MaxSilentChunks = 30;        // ~1.5s of silence at 50 chunks/sec

    public bool IsRecording => _isRecording;

    public event Action? RecordingStarted;
    public event Action<byte[]>? RecordingStopped;
    public event Action<float>? VolumeChanged;

    public static List<string> GetMicrophones()
    {
        var devices = new List<string>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add(caps.ProductName);
        }
        return devices;
    }

    public void StartRecording(int deviceIndex = 0)
    {
        if (_isRecording) return;

        _memoryStream = new MemoryStream();
        var format = new WaveFormat(16000, 16, 1); // 16kHz, 16bit, mono

        _waveIn = new WaveInEvent
        {
            DeviceNumber = deviceIndex,
            WaveFormat = format,
            BufferMilliseconds = 20
        };

        _waveWriter = new WaveFileWriter(_memoryStream, format);
        _silentChunks = 0;

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _waveIn.StartRecording();
        _isRecording = true;
        RecordingStarted?.Invoke();
    }

    public void StopRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;
        _waveIn?.StopRecording();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _waveWriter?.Write(e.Buffer, 0, e.BytesRecorded);

        // Calculate RMS volume
        int sampleCount = e.BytesRecorded / 2;
        if (sampleCount == 0) return;

        double sum = 0;
        for (int i = 0; i < e.BytesRecorded - 1; i += 2)
        {
            short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
            sum += (long)sample * sample;
        }
        float rms = (float)Math.Sqrt(sum / sampleCount);
        float normalizedVolume = Math.Min(rms / 10000f, 1.0f);
        VolumeChanged?.Invoke(normalizedVolume);

        // Silence detection
        if (rms < SilenceThreshold)
            _silentChunks++;
        else
            _silentChunks = 0;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        byte[]? audioData = null;

        if (_waveWriter != null && _memoryStream != null)
        {
            _waveWriter.Flush();
            _waveWriter.Dispose();
            audioData = _memoryStream.ToArray();
            _memoryStream.Dispose();
        }

        _waveIn?.Dispose();
        _waveIn = null;
        _waveWriter = null;
        _memoryStream = null;

        if (audioData != null && audioData.Length > 44) // WAV header is 44 bytes
        {
            RecordingStopped?.Invoke(audioData);
        }
    }

    public void Dispose()
    {
        StopRecording();
        _waveIn?.Dispose();
        _waveWriter?.Dispose();
        _memoryStream?.Dispose();
    }
}
