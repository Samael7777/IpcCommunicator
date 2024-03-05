namespace IpcCommunicator;

public class PipeErrorEventArgs : EventArgs
{
    public PipeErrorEventArgs(Exception ex)
    {
        Exception = ex;
    }

    public Exception Exception { get; }
}