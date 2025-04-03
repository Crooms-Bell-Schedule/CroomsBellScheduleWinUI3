using CroomsBellScheduleCS.Provider;
using System.Text.Json.Serialization;
using static CroomsBellScheduleCS.Provider.APIProvider;
using static CroomsBellScheduleCS.Utils.SettingsManager;

namespace CroomsBellScheduleCS.Utils
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Root))]
    [JsonSerializable(typeof(SettingsRoot))]
    [JsonSerializable(typeof(LocalBellRoot))]
    [JsonSerializable(typeof(LoginRequest))]
    [JsonSerializable(typeof(ApiResponse<ErrorResponse>))]
    [JsonSerializable(typeof(ApiResponse<LoginResponse>))]
    [JsonSerializable(typeof(ApiResponse<CommandResponse>))]
    [JsonSerializable(typeof(ApiResponse<FeedEntry[]>))]
    [JsonSerializable(typeof(ApiResponse<FeedEntry>))]
    [JsonSerializable(typeof(ApiResponse<UsernameChangeRequest>))]
    [JsonSerializable(typeof(SubmitFeedRequest))]
    [JsonSerializable(typeof(ApiSimpleResponse))]
    [JsonSerializable(typeof(UsernameChangeRequest))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
