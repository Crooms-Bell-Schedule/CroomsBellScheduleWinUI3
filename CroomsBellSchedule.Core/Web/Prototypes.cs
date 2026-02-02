using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CroomsBellSchedule.Core.Web
{
    // HTTP API Schema
    public class AnnouncementContent
    {
        public string title { get; set; } = null!;
        public string message { get; set; } = null!;
    }

    public class Announcement
    {
        public string id { get; set; } = null!;
        public string expires { get; set; } = null!;
        public AnnouncementContent data { get; set; } = null!;
        public string created { get; set; } = null!;
        public List<string> targets { get; set; } = [];
        public bool priority { get; set; }
    }
    public class LoginRequest
    {
        public string username { get; set; } = "";
        public string password { get; set; } = "";
    }
    public class CommandResponse
    {
        public bool result { get; set; } = false;
    }
    public class LoginResponse
    {
        public string sid { get; set; } = "";
        public string uid { get; set; } = "";
    }
    public class BellScheduleProperties
    {
        public string data { get; set; } = "";
        public string status { get; set; } = "";
    }
    public class LunchEntry
    {
        public string name { get; set; } = "";
        public string image { get; set; } = "";
    }

    public class TeacherQuote
    {
        public string quote { get; set; } = "";
        public string teacher { get; set; } = "";
    }
    public class FeedEntry
    {
        public string data { get; set; } = "";
        public string store { get; set; } = "";
        public string id { get; set; } = "";
        public string uid { get; set; } = "";
        public DateTime create { get; set; }
        public DateTime delete { get; set; }
        public string createdBy { get; set; } = "";
        public bool verified { get; set; } = false;
    }
    public class SubmitFeedRequest
    {
        public string data { get; set; } = "";
    }
    public class UsernameChangeRequest
    {
        public string username { get; set; } = "";
    }
    public class ApiSimpleResponse
    {
        public string status { get; set; } = "";
        public string? code { get; set; }
    }
    public class ApiResponse<T>
    {
        public string status { get; set; } = "";
        public T? data { get; set; }
        public string? code { get; set; }
    }
    public class ErrorResponse
    {
        public string error { get; set; } = "No response from server";
    }

    public class UserDetailsResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";
        [JsonPropertyName("displayname")]
        public string DisplayName { get; set; } = "";
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    public class SetProfilePictureResultError
    {
        public string error { get; set; } = "";
        public string? file { get; set; } = "";
    }

    public class SetProfilePictureResult
    {
        public string status { get; set; } = "";
        public SetProfilePictureResultError? data { get; set; }
    }

    public class GetUserResult
    {
        public string id { get; set; } = "";
        public string username { get; set; } = "";
    }
    public class Survey
    {
        public string name { get; set; } = "";
        public string id { get; set; } = "";
        public string link { get; set; } = "";
    }
    public class LivestreamAvailabilityResponse
    {
        public bool exists { get; set; }
    }

    // Websocket Schema
    public class FeedMessage
    {
        public string Message { get; set; } = null!;

        public static string? GetMessageType(string data)
        {
            var obj = JsonSerializer.Deserialize(data, SourceGenerationContext.Default.FeedMessage);
            if (obj == null) return null;

            return obj.Message;
        }

        public static FeedMessage? Deserialize(string data)
        {
            string? msgType;
            switch (msgType = GetMessageType(data))
            {
                case "DeletePost":
                    return JsonSerializer.Deserialize(data, SourceGenerationContext.Default.DeletePostMessage);
                case "UpdatePost":
                    return JsonSerializer.Deserialize(data, SourceGenerationContext.Default.UpdatePostMessage);
                case "NewPost":
                    return JsonSerializer.Deserialize(data, SourceGenerationContext.Default.NewPostMessage);
                default:
                    Debug.WriteLine("unknown WebSocket message: " + msgType);
                    return null;
            }
        }
    }

    public class DeletePostMessage : FeedMessage
    {
        public string ID { get; set; } = null!;
    }
    public class UpdatePostMessage : FeedMessage
    {
        public string ID { get; set; } = null!;
        public string NewContent { get; set; } = null!;
    }
    public class NewPostMessage : FeedMessage
    {
        public string ID { get; set; } = null!;
        public FeedEntry Data { get; set; } = null!;
    }
    public record PrivateBetaRequest
    {
        public string? AccessCode { get; set; }
    }

    public record PrivateBetaResponse
    {
        public bool valid { get; set; }
        public string data { get; set; } = "";
    }
}
