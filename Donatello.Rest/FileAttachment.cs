namespace Donatello.Rest;

using System.Net.Http;

public sealed class FileAttachment
{
    /// <summary>File name.</summary>
    public string Name { get; internal init; }

    /// <summary>File size.</summary>
    public long Size { get; internal init; }

    /// <summary>File content.</summary>
    public HttpContent Content { get; internal init; }
}
