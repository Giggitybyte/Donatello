namespace Donatello.Common.Extension;

using System.Threading.Tasks;
using Entity.Guild;
using Entity.User;

public static class UserExtensionMethods
{
    public static ValueTask<GuildMember> GetMemberAsync(this User user, Guild guild)
        => guild.GetMemberAsync(user.Id);

   /* public static TBuilder FromJson<TBuilder>(this TBuilder builder, JsonElement json) where TBuilder : EntityBuilder
    {
        var entitybuilder = (TBuilder)Activator.CreateInstance(typeof(TBuilder));
        entitybuilder.Json = JsonObject.Create(json);

        return entitybuilder;
    }

    public static TBuilder FromInstance<TBuilder>(TEntity instance) where TBuilder : EntityBuilder
        => FromJson<TBuilder>(instance.Json);*/
}

