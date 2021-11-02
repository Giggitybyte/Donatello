namespace Donatello.Rest.Extensions.Endpoint;

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for application endpoints.</summary>
public static class ApplicationExtensions
{
    /// <summary>Returns an array of <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command objects</see>.</summary>
    public static Task<HttpResponse> GetGlobalAppCommandsAsync(this DiscordHttpClient httpClient, ulong applicationId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"applications/{applicationId}/commands");

    /// <summary>Returns the newly created <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command</see>.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/interactions/application-commands#create-global-application-command">Click here to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> CreateGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"applications/{applicationId}/commands", jsonBuilder);

    /// <summary>Returns an <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command object</see>.</summary>
    public static Task<HttpResponse> GetGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, ulong commandId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"applications/{applicationId}/commands/{commandId}");

    /// <summary>
    /// Changes attributes of a global command. Returns an updated
    /// <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command object</see>.
    /// </summary>
    /// <remarks><see href="https://discord.com/developers/docs/interactions/application-commands#edit-global-application-command">Click here to see valid JSON parameters</see>.</remarks>
    public static Task<HttpResponse> ModifyGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, ulong commandId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"applications/{applicationId}/commands/{commandId}", jsonBuilder);

    /// <summary>Permanently deletes a global command.</summary>
    public static Task<HttpResponse> DeleteGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, ulong commandId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"applications/{applicationId}/commands/{commandId}");

    /// <summary>
    /// Accepts an array of 
    /// <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command objects</see>
    /// which will replace all existing global commands.
    /// </summary>
    public static Task<HttpResponse> OverwriteGlobalCommandsAsync(this DiscordHttpClient httpClient, ulong applicationId, Action<Utf8JsonWriter> jsonBuilder)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"applications/{applicationId}/commands", jsonBuilder);
}
