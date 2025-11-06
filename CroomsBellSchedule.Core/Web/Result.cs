using System;

namespace CroomsBellSchedule.Core.Web
{
    public class Result
    {
        public bool OK { get; set; }
        public string Message { get; set; } = "Command OK";
        public Exception? Exception { get; set; }
        public bool IsRateLimitReached { get; set; }

        public static readonly Result Ok = new() { OK = true };
    }
    public class Result<T>
    {
        public bool OK { get; set; }
        public T? Value { get; set; }
        public string? ErrorCode { get; set; }
        public ErrorResponse? ErrorValue { get; set; }
        public Exception? Exception { get; set; }
        public bool IsRateLimitReached { get; set; }



        public static Result<T> FromException(Exception ex)
        {
            return new()
            {
                OK = false,
                Value = default,
                ErrorCode = "E_APPERR",
                Exception = ex
            };
        }
    }
}
