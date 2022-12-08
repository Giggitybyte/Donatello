namespace Donatello.Extension.User;

using Donatello.Builder;
using Donatello.Entity;
using System.Text.Json.Nodes;
using System.Text.Json;
using System;
using System.Threading.Tasks;

public static class UserExtensionMethods
{
    public static ValueTask<DiscordGuildMember> GetMemberAsync(this DiscordUser user, DiscordGuild guild)
        => guild.GetMemberAsync(user.Id);

    public static TBuilder FromJson<TBuilder>(this TBuilder builder, JsonElement json) where TBuilder : EntityBuilder<TEntity>
    {
        var builder = (TBuilder)Activator.CreateInstance(typeof(TBuilder));
        builder.Json = JsonObject.Create(json);

        return builder;
    }

    public static TBuilder FromInstance<TBuilder>(TEntity instance) where TBuilder : EntityBuilder<TEntity>
        => FromJson<TBuilder>(instance.Json);
}

