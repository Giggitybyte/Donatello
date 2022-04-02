namespace Donatello.Interaction.Command;

using Qmmands;

/// <summary>The result of a Discord application command.</summary>
public sealed class DiscordCommandResult : CommandResult
{
    private bool _interactionSuccessful;

    internal DiscordCommandResult(bool interactionSuccessful)
    {
        _interactionSuccessful = interactionSuccessful;
    }

    public override bool IsSuccessful => _interactionSuccessful;
}
