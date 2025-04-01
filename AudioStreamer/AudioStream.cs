using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioStreamer;

public abstract class AudioStream : IDisposable
{
    public abstract bool Started { get; }

    public abstract int NumChannels { get; }

    public abstract int SampleRate { get; }

    public abstract int BitsPerSample { get; }

    public bool IsDisposed { get; private set; }

    private readonly List<AudioStreamReader> readers = new List<AudioStreamReader>();

    /// <summary>
    /// Main buffer that readers read from
    /// </summary>
    private Memory<byte> mainBuffer;

    /// <summary>
    /// End position of the last written data in main buffer
    /// </summary>
    private int position;

    /// <summary>
    /// Iteration counter, aka how many times the position in the main buffer has restarted from 0.
    /// </summary>
    private uint iteration;

    private readonly object readLock = new object();

    public AudioStream(uint bufferSize)
    {
        mainBuffer = new Memory<byte>(new byte[bufferSize]);
    }

    public abstract void Start();

    public abstract void Stop();

    public AudioStreamReader CreateReader()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        var reader = new AudioStreamReader(this);
        readers.Add(reader);

        return reader;
    }

    protected abstract int ReadInternal(Span<byte> buffer);

    internal int Read(AudioStreamReader reader, Span<byte> buffer)
    {
        if (reader.AudioStream != this)
            throw new ArgumentException("Reader does not belong to this stream");

        if (IsDisposed)
            throw new ObjectDisposedException(nameof(AudioStream));

        lock (readLock)
        {
            if (reader.IsFirstRead)
            {
                reader.IsFirstRead = false;

                // Set the initial position and iteration for the reader
                if (buffer.Length < position)
                {
                    // Desired bytes is less than available bytes in the current iteration
                    reader.ParentBufferIteration = iteration;
                    reader.ParentBufferPosition = position - buffer.Length;
                }
                else
                {
                    if (iteration > 0)
                    {
                        reader.ParentBufferIteration = iteration - 1;
                        reader.ParentBufferPosition = mainBuffer.Length - buffer.Length + position;
                    }
                    else
                    {
                        reader.ParentBufferIteration = 0;
                        reader.ParentBufferPosition = 0;
                    }
                }
            }

            if (iteration - reader.ParentBufferIteration > 1)
                throw new ArgumentException("Reader is too far behind the main buffer.");

            if (buffer.Length == 0)
                return 0;

            int readBytes = 0;

            while (readBytes < buffer.Length)
            {
                if (iteration - reader.ParentBufferIteration == 1)
                {
                    // Reader is 1 iteration behind, read from the end of the buffer
                    int numToCopy = Math.Min(buffer.Length, mainBuffer.Length - reader.ParentBufferPosition);
                    mainBuffer.Slice(reader.ParentBufferPosition, numToCopy).Span.CopyTo(buffer.Slice(readBytes, numToCopy));

                    readBytes += numToCopy;
                    reader.ParentBufferPosition += numToCopy;
                }
                else
                {
                    // Reader is on current iteration, read from the start of the buffer
                    if (reader.ParentBufferPosition < position)
                    {
                        int numToCopy = Math.Min(position - reader.ParentBufferPosition, buffer.Length - readBytes);
                        mainBuffer.Slice(reader.ParentBufferPosition, numToCopy).Span.CopyTo(buffer.Slice(readBytes, numToCopy));

                        readBytes += numToCopy;
                        reader.ParentBufferPosition += numToCopy;
                    }
                    else
                    {
                        // Reader is at the end of available data, read more data into buffer
                        int numBytesReadIntoBuffer = ReadIntoBuffer(buffer.Length - readBytes);

                        if (numBytesReadIntoBuffer == 0)
                            break;

                        continue;
                    }
                }

                if (reader.ParentBufferPosition == mainBuffer.Length)
                {
                    reader.ParentBufferIteration += 1;
                    reader.ParentBufferPosition = 0;
                }
            }

            return readBytes;
        }
    }

    private int ReadIntoBuffer(int numBytes)
    {
        if (numBytes >= mainBuffer.Length)
            throw new ArgumentOutOfRangeException(nameof(numBytes), "Can't read more than buffer size.");

        int totalRead = 0;

        while (numBytes > 0)
        {
            int numBytesRead = ReadInternal(mainBuffer.Span.Slice(position, Math.Min(numBytes, mainBuffer.Length - position)));

            if (numBytesRead == 0)
                break;

            totalRead += numBytesRead;
            numBytes -= numBytesRead;

            if (position + numBytesRead == mainBuffer.Length)
            {
                iteration += 1;
                position = 0;
            }
            else
            {
                position += numBytesRead;
            }
        }

        return totalRead;
    }

    protected void ResizeBuffer(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        if (size == mainBuffer.Length)
            return;

        var newBuffer = new Memory<byte>(new byte[size]);
        mainBuffer.Span.Slice(0, Math.Min(position, size)).CopyTo(newBuffer.Span);
        mainBuffer = newBuffer;

        foreach (var reader in readers)
        {
            reader.ParentBufferPosition = Math.Min(reader.ParentBufferPosition, position);
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (disposing)
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
            }

            readers.Clear();
        }

        mainBuffer = null;
        IsDisposed = true;
    }
}
