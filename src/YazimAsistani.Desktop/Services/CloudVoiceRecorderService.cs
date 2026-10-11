using System.IO;
using NAudio.Wave;

namespace YazimAsistani.Desktop.Services;

public sealed class CloudVoiceRecorderService : IDisposable
{
    private const int SampleRate = 16_000;
    private const int BitsPerSample = 16;
    private const int Channels = 1;

    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private string? _temporaryFile;
    private TaskCompletionSource<byte[]>? _stopCompletion;

    public bool IsRecording => _waveIn is not null;

    public void Start()
    {
        if (IsRecording)
        {
            return;
        }

        var temporaryFile = Path.Combine(
            Path.GetTempPath(),
            $"YazimAsistani-{Guid.NewGuid():N}.wav");
        var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
            BufferMilliseconds = 100
        };
        var writer = new WaveFileWriter(temporaryFile, waveIn.WaveFormat);

        waveIn.DataAvailable += HandleDataAvailable;
        waveIn.RecordingStopped += HandleRecordingStopped;
        _temporaryFile = temporaryFile;
        _waveIn = waveIn;
        _writer = writer;

        try
        {
            waveIn.StartRecording();
        }
        catch (Exception exception)
        {
            Cleanup(deleteTemporaryFile: true);
            throw new InvalidOperationException(
                "Mikrofon başlatılamadı. Mikrofon iznini ve ses aygıtını denetleyin.",
                exception);
        }
    }

    public async Task<byte[]> StopAsync(CancellationToken cancellationToken = default)
    {
        var waveIn = _waveIn;
        if (waveIn is null)
        {
            return [];
        }

        _stopCompletion = new TaskCompletionSource<byte[]>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        waveIn.StopRecording();
        return await _stopCompletion.Task
            .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_waveIn is not null)
        {
            try
            {
                _waveIn.StopRecording();
            }
            catch
            {
                Cleanup(deleteTemporaryFile: true);
            }
        }
        else
        {
            Cleanup(deleteTemporaryFile: true);
        }

        GC.SuppressFinalize(this);
    }

    private void HandleDataAvailable(object? sender, WaveInEventArgs eventArgs)
    {
        _writer?.Write(eventArgs.Buffer, 0, eventArgs.BytesRecorded);
    }

    private void HandleRecordingStopped(object? sender, StoppedEventArgs eventArgs)
    {
        var completion = _stopCompletion;
        var temporaryFile = _temporaryFile;

        try
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;

            if (eventArgs.Exception is not null)
            {
                completion?.TrySetException(new InvalidOperationException(
                    "Mikrofon kaydı beklenmedik biçimde durdu.",
                    eventArgs.Exception));
                return;
            }

            var audio = temporaryFile is not null && File.Exists(temporaryFile)
                ? File.ReadAllBytes(temporaryFile)
                : [];
            completion?.TrySetResult(audio);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            completion?.TrySetException(exception);
        }
        finally
        {
            Cleanup(deleteTemporaryFile: true);
        }
    }

    private void Cleanup(bool deleteTemporaryFile)
    {
        var waveIn = _waveIn;
        _waveIn = null;
        if (waveIn is not null)
        {
            waveIn.DataAvailable -= HandleDataAvailable;
            waveIn.RecordingStopped -= HandleRecordingStopped;
            waveIn.Dispose();
        }

        _writer?.Dispose();
        _writer = null;

        var temporaryFile = _temporaryFile;
        _temporaryFile = null;
        _stopCompletion = null;
        if (deleteTemporaryFile && temporaryFile is not null && File.Exists(temporaryFile))
        {
            try
            {
                File.Delete(temporaryFile);
            }
            catch (IOException)
            {
                // The operating system will eventually reclaim an unlocked temporary recording.
            }
            catch (UnauthorizedAccessException)
            {
                // Failure to remove a temporary recording must not crash the application.
            }
        }
    }
}
