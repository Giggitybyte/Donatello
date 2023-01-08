namespace Donatello.Builder;

using System;

public sealed class ChannelBuilder : EntityBuilder
{
    public class Guild
    {
        private readonly ChannelBuilder _parent;

        
        
        public Guild SetPosition(int position)
        {
            
        }
    }

    public TBuilder SetName(string name)
    {
        if (name.Length <= 100)
            this.Json["name"] = name;
        else
            throw new ArgumentException("Channel name cannot be greater than 100 characters.");

        return this as TBuilder;
    }
}
