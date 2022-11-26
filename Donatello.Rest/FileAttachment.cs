namespace Donatello.Rest;

using System.IO;
using System.Net.Http;

public sealed class FileAttachment
{
    internal FileAttachment(string name, long size, HttpContent content)
    {
        this.Content = content;
        this.Name = name;
        this.Size = size;
    }

    /// <summary>File content.</summary>
    internal HttpContent Content { get; private init; }

    /// <summary>File name.</summary>
    public string Name { get; private init; }

    /// <summary>File size.</summary>
    public long Size { get; private init; }

    /// <summary>Creates a new local file attachment from a byte array.</summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="bytes">File bytes.</param>
    public static FileAttachment FromBytes(string fileName, byte[] bytes)
        => new(fileName, bytes.LongLength, new ByteArrayContent(bytes));

    /// <summary>Creates a new local file attachment from a stream.</summary>
    /// <param name="fileName">File name with extension.</param>
    /// <param name="stream">File stream.</param>
    public static FileAttachment FromStream(string fileName, Stream stream)
        => new(fileName, stream.Length, new StreamContent(stream));
}
