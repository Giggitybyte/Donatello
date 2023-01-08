namespace Donatello.Entity;

using System;

public partial class DiscordGuild
{
    /// <summary>Permission flags for roles and channel overwrites.</summary>
    [Flags]
    public enum Permission : ulong
    {
        None = 0,

        /// <summary>Permits the creation of channel invites.</summary>
        CreateInvite = 1ul << 0,

        /// <summary>Permits removing users from a guild.</summary>
        KickUsers = 1ul << 1,

        /// <summary>Permits banning users from a guild.</summary>
        BanUsers = 1ul << 2,

        /// <summary>Implicitly grants all permissions and bypasses channel permission overwrites.</summary>
        Administrator = 1ul << 3,

        /// <summary>Permits modification of channel properties.</summary>
        ManageChannels = 1ul << 4,

        /// <summary>Permits modification of server properties.</summary>
        ManageServer = 1ul << 5,

        /// <summary>Permits the use of reactions on a message.</summary>
        UseReactions = 1ul << 6,

        /// <summary>Permits viewing of the audit logs.</summary>
        ViewAuditLog = 1ul << 7,

        /// <summary>Permits the use of the <i>priority speaker</i> keybind in a voice channel.</summary>
        PrioritySpeaker = 1ul << 8,

        /// <summary>Permits a user to "go live" and stream to a voice channel.</summary>
        StreamVideo = 1ul << 9,

        /// <summary>Permits access to voice and text channels.</summary>
        ViewChannel = 1ul << 10,

        /// <summary>Permits sending messages in a text channel.</summary>
        SendMessages = 1ul << 11,

        /// <summary>Permits sending a text-to-speech message in a text channel.</summary>
        SendTextToSpeech = 1ul << 12,

        /// <summary>Permits deletion of user messages.</summary>
        ManageMessages = 1ul << 13,

        /// <summary>Allows messages to display embeds.</summary>
        DisplayEmbeds = 1ul << 14,

        /// <summary>Permits the attachment of files to a message.</summary>
        UploadFiles = 1ul << 15,

        /// <summary>Permits access to previous messages in a channel.</summary>
        ViewChannelHistory = 1ul << 16,

        /// <summary>Permits the use of the <c>@here</c> and <c>@everyone</c> tags.</summary>
        MentionEveryone = 1ul << 17,

        /// <summary>Permits the use of emotes from other servers.</summary>
        ExternalEmojis = 1ul << 18,

        /// <summary>Permits viewing of server community insights.</summary>
        CommunityInsights = 1ul << 19,

        /// <summary>Permits connection to voice and stage channels.</summary>
        Connect = 1ul << 20,

        /// <summary>Permits speaking in a voice channel.</summary>
        Speak = 1ul << 21,

        /// <summary>Permits muting users in a voice channel.</summary>
        MuteUsers = 1ul << 22,

        /// <summary>Permits deafening users in a voice channel.</summary>
        DeafenUsers = 1ul << 23,

        /// <summary>Permits moving users between voice channels.</summary>
        MoveUsers = 1ul << 24,

        /// <summary>Permits use of <i>voice activity detection</i> in a voice channel.</summary>
        /// <remarks>Denying this permission will require the use of push-to-talk.</remarks>
        OpenMic = 1ul << 25,

        /// <summary>Permits modifying own nickname.</summary>
        ModifyNickname = 1ul << 26,

        /// <summary>Permits modification of users nicknames</summary>
        ManageNicknames = 1ul << 27,

        /// <summary>Permits modification of roles.</summary>
        ManageRoles = 1ul << 28,

        /// <summary>Permits the creation and modification of webhooks.</summary>
        ManageWebhooks = 1ul << 29,

        /// <summary>Permits the creation and modification of custom emotes and stickers.</summary>
        ManageEmotes = 1ul << 30,

        /// <summary>Permits the use of context menu commands and slash commands. </summary>
        ApplicationCommands = 1ul << 31,

        /// <summary>Permits requests to speak in a stage channel.</summary>
        RequestToSpeak = 1ul << 32,

        /// <summary>Permits archiving and deleting threads, and access to all private threads.</summary>
        ManageThreads = 1ul << 34,

        /// <summary>Permits creating and participating in public threads.</summary>
        PublicThreads = 1ul << 35,

        /// <summary>Permits creating and participating in private threads.</summary>
        PrivateThreads = 1ul << 36,

        /// <summary>Permits the use of stickers from other servers.</summary>
        ExternalStickers = 1ul << 37,

        /// <summary>Permits sending message in threads.</summary>
        SendThreadMessages = 1ul << 38,

        /// <summary>Permits launching of an activity in a voice channel.</summary>
        StartActivites = 1ul << 39,

        /// <summary>Permits a user to issue <see href="https://support.discord.com/hc/en-us/articles/4413305239191">time-outs</see> to other users.</summary>
        ModerateMembers = 1ul << 40
    }
}