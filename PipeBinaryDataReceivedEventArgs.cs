namespace IpcCommunicator;

public class PipeBinaryDataReceivedEventArgs : EventArgs
{
    public PipeBinaryDataReceivedEventArgs(byte[] data)
    {
        Data = data;
    }

    public byte[] Data { get; }
}