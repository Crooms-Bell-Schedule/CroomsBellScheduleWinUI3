using CroomsBellScheduleCS.Provider;
using CroomsBellScheduleCS.Web;
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
    [JsonSerializable(typeof(ApiResponse<UserDetailsResponse>))]
    [JsonSerializable(typeof(BellScheduleProperties))]
    [JsonSerializable(typeof(ApiResponse<LunchEntry[]>))]
    [JsonSerializable(typeof(SubmitFeedRequest))]
    [JsonSerializable(typeof(ApiSimpleResponse))]
    [JsonSerializable(typeof(UsernameChangeRequest))]
    [JsonSerializable(typeof(AnnouncementData))]
    [JsonSerializable(typeof(ApiResponse<Survey[]>))]
    [JsonSerializable(typeof(ApiResponse<SetProfilePictureResult>))]
    [JsonSerializable(typeof(ApiResponse<GetUserResult>))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
