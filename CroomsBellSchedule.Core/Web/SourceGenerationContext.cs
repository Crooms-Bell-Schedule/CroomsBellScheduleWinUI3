using System.Text.Json.Serialization;
using CroomsBellSchedule.Core.Provider;
using static CroomsBellSchedule.Core.Provider.APIProvider;
using static CroomsBellSchedule.Service.SettingsManager;

namespace CroomsBellSchedule.Core.Web
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
    [JsonSerializable(typeof(ApiResponse<bool>))]
    [JsonSerializable(typeof(ApiResponse<bool?>))]

    [JsonSerializable(typeof(FeedMessage))]
    [JsonSerializable(typeof(DeletePostMessage))]
    [JsonSerializable(typeof(UpdatePostMessage))]
    [JsonSerializable(typeof(NewPostMessage))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
