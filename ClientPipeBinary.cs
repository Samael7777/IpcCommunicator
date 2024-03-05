using System.IO.Pipes;

namespace IpcCommunicator;

public class ClientPipeBinary : BasePipe, IDisposable
{
    protected NamedPipeClientStream ClientPipeStream;

    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<PipeBinaryDataReceivedEventArgs>? DataReceived;
    public event EventHandler<PipeErrorEventArgs>? Error; 

    public ClientPipeBinary(string serverName, string pipeName)
    {
        ServerName = serverName;
        PipeName = pipeName;

        ClientPipeStream = new NamedPipeClientStream(
            serverName,
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        PipeStream = ClientPipeStream;
    }

    public void Connect(int timeoutMs)
    {
        try
        {
            ClientPipeStream.Connect(timeoutMs);
            if (ClientPipeStream.IsConnected) Connected?.Invoke(this, EventArgs.Empty);
            _ = BeginRead();
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new PipeErrorEventArgs(ex));
        }
    }

    public string PipeName { get; }
    public string ServerName { get; }

    protected override void DataReceivedCallback(byte[] data)
    {
        DataReceived?.Invoke(this, new PipeBinaryDataReceivedEventArgs(data));
    }

    protected override void DisconnectedCallback()
    {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    protected override void ErrorCallback(Exception exception)
    {
        Error?.Invoke(this, new PipeErrorEventArgs(exception));
    }

    #region Dispose

    private bool _disposed;

    ~ClientPipeBinary()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            //dispose managed state (managed objects)
            Close();

        }
        //free unmanaged resources (unmanaged objects) and override finalizer
        //set large fields to null

        _disposed = true;
    }

    #endregion
}