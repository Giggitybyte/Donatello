namespace Donatello.Rest.Endpoint;

using Donatello.Rest.Transport;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>Basic implementations for application endpoints.</summary>
public static class ApplicationEndpoints
{
    /// <summary>Fetches all global commands for your application.</summary>
    /// <returns>Array of <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command objects</see>.</returns>
    public static Task<HttpResponse> GetGlobalAppCommandsAsync(this DiscordHttpClient httpClient, ulong applicationId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"applications/{applicationId}/commands");

    /// <summary>Create a new global command. New global commands will be available in all guilds after 1 hour.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/interactions/application-commands#create-global-application-command">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns><see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command object</see></returns>
    public static Task<HttpResponse> CreateGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Post, $"applications/{applicationId}/commands", jsonWriter);

    /// <summary>Fetch a global command for your application.</summary>
    /// <returns><see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command object</see></returns>
    public static Task<HttpResponse> GetGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, ulong commandId)
        => httpClient.SendRequestAsync(HttpMethod.Get, $"applications/{applicationId}/commands/{commandId}");

    /// <summary>Changes attributes of a global command.</summary>
    /// <remarks><see href="https://discord.com/developers/docs/interactions/application-commands#edit-global-application-command">Click here to see valid JSON parameters</see>.</remarks>
    /// <returns>Updated <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command object</see>.</returns>
    public static Task<HttpResponse> ModifyGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, ulong commandId, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Patch, $"applications/{applicationId}/commands/{commandId}", jsonWriter);

    /// <summary>Permanently deletes a global command.</summary>
    public static Task<HttpResponse> DeleteGlobalAppCommandAsync(this DiscordHttpClient httpClient, ulong applicationId, ulong commandId)
        => httpClient.SendRequestAsync(HttpMethod.Delete, $"applications/{applicationId}/commands/{commandId}");

    /// <summary>
    /// Accepts an array of 
    /// <see href="https://discord.com/developers/docs/interactions/application-commands#application-command-object">application command objects</see>
    /// which will replace all existing global commands.
    /// </summary>
    public static Task<HttpResponse> OverwriteGlobalCommandsAsync(this DiscordHttpClient httpClient, ulong applicationId, Action<Utf8JsonWriter> jsonWriter)
        => httpClient.SendRequestAsync(HttpMethod.Put, $"applications/{applicationId}/commands", jsonWriter);
}
