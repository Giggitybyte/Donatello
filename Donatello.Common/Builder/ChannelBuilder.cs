namespace Donatello.Builder;

using System;

public class ChannelBuilder<TBuilder> : EntityBuilder where TBuilder : class
{
    public TBuilder SetName(string name)
    {
        if (name.Length <= 100)
            this.Json["name"] = name;
        else
            throw new ArgumentException("Channel name cannot be greater than 100 characters.");

        return this as TBuilder;
    }
}
