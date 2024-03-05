using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;

namespace IpcCommunicator;

public abstract class BasePipe
{
    private const int IntSize = sizeof(int);

    protected PipeStream? PipeStream;

    public bool IsConnected => PipeStream?.IsConnected ?? false;

    public void Send(byte[] data)
    {
        try
        {
            ValidatePipeState();

            Span<byte> buffer = stackalloc byte[data.Length + IntSize];
            BitConverter.GetBytes(data.Length).CopyTo(buffer);
            data.CopyTo(buffer[IntSize..]);

            PipeStream.Write(buffer);
            PipeStream.Flush();
        }
        catch (Exception e)
        {
            ErrorCallback(e);
        }
    }

    public void Close()
    {
        if (IsConnected)
        {
            PipeStream?.WaitForPipeDrain();
        }
        PipeStream?.Close();
        PipeStream = null;
    }

    protected abstract void DataReceivedCallback(byte[] data);
    protected abstract void DisconnectedCallback();
    protected abstract void ErrorCallback(Exception exception);

    protected async Task BeginRead(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                ValidatePipeState();

                var lenBuff = new byte[IntSize];
                var receivedBytes = await ReadFromStreamAsync(lenBuff.AsMemory(0, IntSize), token)
                    .ConfigureAwait(false);

                if (receivedBytes == 0)
                {
                    DisconnectedCallback();
                    break;
                }

                if (receivedBytes != IntSize)
                    throw new ApplicationException("Pipe communication error");

                var packetSize = BitConverter.ToInt32(lenBuff);
                var packetBuffer = new byte[packetSize];

                receivedBytes = await ReadFromStreamAsync(packetBuffer.AsMemory(0, packetSize), token)
                    .ConfigureAwait(false);

                if (receivedBytes == 0)
                {
                    DisconnectedCallback();
                    break;
                }

                DataReceivedCallback(packetBuffer);
            }
            catch (OperationCanceledException)
            { }
            catch (Exception ex)
            {
                ErrorCallback(ex);
                break;
            }
        }
    }

    private async Task<int> ReadFromStreamAsync(Memory<byte> buffer, CancellationToken token)
    {
        try
        {
            var bytesRead = await PipeStream!.ReadAsync(buffer, token).ConfigureAwait(false);
            return bytesRead;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorCallback(ex);
            return 0;
        }
    }

    [MemberNotNull(nameof(PipeStream))]
    private void ValidatePipeState()
    {
        if (PipeStream is not { IsConnected: true })
            throw new ArgumentException("Pipe is not connected.");
    }
}