using System.IO.Pipes;

namespace IpcCommunicator;

public class ServerPipeBinary : BasePipe, IDisposable
{
    protected NamedPipeServerStream ServerPipeStream;
    
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;
    public event EventHandler<PipeBinaryDataReceivedEventArgs>? DataReceived;
    public event EventHandler<PipeErrorEventArgs>? Error; 

    public ServerPipeBinary(string pipeName, int maxServerInstances)
    {
        PipeName = pipeName;

        ServerPipeStream = new NamedPipeServerStream(
            pipeName, 
            PipeDirection.InOut,
            maxServerInstances, 
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);

        PipeStream = ServerPipeStream;
        ServerPipeStream.BeginWaitForConnection(PipeConnected, null);
    }

    public string PipeName { get; }

    protected virtual void PipeConnected(IAsyncResult ar)
    {
        try
        {
            ServerPipeStream.EndWaitForConnection(ar);
            Connected?.Invoke(this, EventArgs.Empty);
            _ = BeginRead();
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new PipeErrorEventArgs(ex));
        }
    }

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

   ~ServerPipeBinary()
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