using System.Text.Json.Serialization;

namespace Lexicon.Models.Chat;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationMessageRole
{
    [JsonStringEnumMemberName("user")]
    User = 0,

    [JsonStringEnumMemberName("assistant")]
    Assistant = 1,
}
