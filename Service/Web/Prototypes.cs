using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CroomsBellScheduleCS.Service.Web
{
    public class AnnouncementData
    {
        public List<Announcement> announcements { get; set; } = [];
    }
    public class Announcement
    {
        public int id { get; set; }
        public string date { get; set; } = "";
        public string title { get; set; } = "";
        public string content { get; set; } = "";
        public bool important { get; set; }
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
    }
    public class ApiResponse<T>
    {
        public string status { get; set; } = "";
        public T? data { get; set; }
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
    }

    public class SetProfilePictureResultError
    {
        public string error { get; set; } = "";
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
}
