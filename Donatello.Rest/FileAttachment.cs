﻿namespace Donatello.Rest;

using System.IO;
using System.Net.Http;

public sealed class FileAttachment 
{
    internal FileAttachment(string name, HttpContent content)
    {
        this.Name = name;
        this.Content = content;
    }

    /// <summary>File name.</summary>
    internal string Name { get; private init; }

    /// <summary>File content.</summary>
    internal HttpContent Content { get; private init; }

    /// <summary>Creates a new <see cref="FileAttachment"/> from a byte array.</summary>
    public static FileAttachment FromBytes(string fileName, byte[] bytes)
        => new(fileName, new ByteArrayContent(bytes));

    /// <summary>Creates a new <see cref="FileAttachment"/> from a stream.</summary>
    public static FileAttachment FromStream(string fileName, Stream stream)
        => new(fileName, new StreamContent(stream));
}
