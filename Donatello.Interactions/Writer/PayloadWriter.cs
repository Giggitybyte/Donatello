namespace Donatello.Interactions.Writer;

using Donatello.Rest.Transport;
using System.Collections.Generic;
using System.Text.Json;

public abstract class PayloadWriter
{
    internal PayloadWriter() 
        => this.Attachments = new List<FileAttachment>(10);

    /// <summary></summary>
    protected List<FileAttachment> Attachments { get; private set; }

    /// <summary>Writes the properties of this object to JSON.</summary>
    internal abstract void WritePayload(Utf8JsonWriter json);
}
