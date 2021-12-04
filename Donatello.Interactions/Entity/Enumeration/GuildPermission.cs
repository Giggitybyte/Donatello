4namespace Donatello.Interactions.Entity.Enumeration;

using System;

/// <summary>Permission flags for roles and channel overwrites.</summary>
[Flags]
public enum GuildPermission : long
{
    None = 0,

    /// <summary>Permits the creation of channel invites.</summary>
    CreateInvite = 1l << 0,

    /// <summary>Permits removing users from a guild.</summary>
    KickUsers = 1l << 1,

    /// <summary>Permits banning users from a guild.</summary>
    BanUsers = 1l << 2,

    /// <summary>Implicitly grants all permissions and bypasses channel permission overwrites.</summary>
    Administrator = 1l << 3,

    /// <summary>Permits modification of channel properties.</summary>
    ManageChannels = 1l << 4,

    /// <summary>Permits modification of server properties.</summary>
    ManageServer = 1l << 5,

    /// <summary>Permits the use of reactions on a message.</summary>
    UseReactions = 1l << 6,

    /// <summary>Permits viewing of the audit logs.</summary>
    ViewAuditLog = 1l << 7,

    /// <summary>Permits the use of the <i>priority speaker</i> keybind in a voice channel.</summary>
    PrioritySpeaker = 1l << 8,

    /// <summary>Permits a user to "go live" and stream to a voice channel.</summary>
    StreamVideo = 1l << 9,

    /// <summary>Permits access to voice and text channels.</summary>
    ViewChannel = 1l << 10,

    /// <summary>Permits sending messages in a text channel.</summary>
    SendMessages = 1l << 11,

    /// <summary>Permits sending a text-to-speech message in a text channel.</summary>
    SendTextToSpeech = 1l << 12,

    /// <summary>Permits deletion of user messages.</summary>
    ManageMessages = 1l << 13,

    /// <summary>Allows messages to display embeds.</summary>
    DisplayEmbeds = 1l << 14,

    /// <summary>Permits the attachment of files to a message.</summary>
    UploadFiles = 1l << 15,

    /// <summary>Permits access to previous messages in a channel.</summary>
    ViewChannelHistory = 1l << 16,

    /// <summary>Permits the use of the <c>@here</c> and <c>@everyone</c> tags.</summary>
    MentionEveryone = 1l << 17,

    /// <summary>Permits the use of emotes from other servers.</summary>
    ExternalEmojis = 1l << 18,

    /// <summary>Permits viewing of server community insights.</summary>
    CommunityInsights = 1l << 19,

    /// <summary>Permits connection to voice and stage channels.</summary>
    Connect = 1l << 20,

    /// <summary>Permits speaking in a voice channel.</summary>
    Speak = 1l << 21,

    /// <summary>Permits muting users in a voice channel.</summary>
    MuteUsers = 1l << 22,

    /// <summary>Permits deafening users in a voice channel.</summary>
    DeafenUsers = 1l << 23,

    /// <summary>Permits moving users between voice channels.</summary>
    MoveUsers = 1l << 24,

    /// <summary>Permits use of <i>voice activity detection</i> in a voice channel.</summary>
    /// <remarks>Denying this permission will require the use of push-to-talk.</remarks>
    OpenMic = 1l << 25,

    /// <summary>Permits modifying own nickname.</summary>
    ModifyNickname = 1l << 26,

    /// <summary>Permits modification of users nicknames</summary>
    ManageNicknames = 1l << 27,

    /// <summary>Permits modification of roles.</summary>
    ManageRoles = 1l << 28,

    /// <summary>Permits the creation and modification of webhooks.</summary>
    ManageWebhooks = 1l << 29,

    /// <summary>Permits the creation and modification of custom emotes and stickers.</summary>
    ManageEmotes = 1l << 30,

    /// <summary>Permits the use of context menu commands and slash commands. </summary>
    ApplicationCommands = 1l << 31,

    /// <summary>Permits requests to speak in a stage channel.</summary>
    RequestToSpeak = 1l << 32,

    /// <summary>Permits archiving and deleting threads, and access to all private threads.</summary>
    ManageThreads = 1l << 34,

    /// <summary>Permits creating and participating in public threads.</summary>
    PublicThreads = 1l << 35,

    /// <summary>Permits creating and participating in private threads.</summary>
    PrivateThreads = 1l << 36,

    /// <summary>Permits the use of stickers from other servers.</summary>
    ExternalStickers = 1l << 37,

    /// <summary>Permits sending message in threads.</summary>
    SendThreadMessages = 1l << 38,

    /// <summary>Permits launching of an activity in a voice channel.</summary>
    StartActivites = 1l << 39
}
