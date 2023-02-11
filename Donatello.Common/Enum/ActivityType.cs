namespace Donatello.Enum;

/// <summary>Defines how the activity is displayed in the client.</summary>
public enum ActivityType : ushort
{
    /// <summary>Displayed as: <c>Playing {activity name}</c></summary>
    Game,

    /// <summary>Displayed as: <c>Streaming {activity details}</c></summary>
    Streaming,

    /// <summary>Displayed as: <c>Listening to {activity name}</c></summary>
    Listening,

    /// <summary>Displayed as: <c>Watching {activity name}</c></summary>
    Watching,

    /// <summary>Displayed as: <c>{emoji} {activity name}</c></summary>
    Custom,

    /// <summary>Displayed as: <c>Competing in {activity name}</c></summary>
    Competing
}
