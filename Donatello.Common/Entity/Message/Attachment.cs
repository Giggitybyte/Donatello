namespace Donatello.Entity;

using System;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary></summary>
public class Attachment
{
    private JsonElement _json;

    internal Attachment(JsonElement json)
    {
        _json = json;
    }

    /// <summary></summary>
    public ValueTask DownloadAsync()
        => throw new NotImplementedException();
}

