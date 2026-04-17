using System;
using System.Net.Sockets;

namespace CroomsBellSchedule.Core.Web
{
    public class Result
    {
        public bool OK { get; set; }
        public string Message { get; set; } = "Command OK";
        public Exception? Exception { get; set; }
        public bool IsRateLimitReached { get; set; }

        public static readonly Result Ok = new() { OK = true };

        public override string ToString()
        {
            if (OK)
                return "Server returned OK";
            if (IsRateLimitReached)
                return "Too many requests, try again later";
            if (Exception != null)
            {
                if (Exception is SocketException)
                {
                    return "Network error, check your connection";
                }
                else
                {
                    return Exception.Message;
                }
            }
            return "Unspecified error";
        }
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

        public override string ToString()
        {
            if (OK)
                return "Server returned OK";
            if (IsRateLimitReached)
                return "Too many requests, try again later";
            if (ErrorValue != null)
                return ErrorValue.error.Contains("permissions") ? "Your login information has expired or is incorrect. Please login again." : ErrorValue.error;
            if (Exception != null)
            {
                if (Exception is SocketException)
                {
                    return "Network error, check your connection";
                }
                else
                {
                    return Exception.Message;
                }
            }
            return "Unspecified error";
        }
    }
}
