using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CroomsBellSchedule.Service;

namespace CroomsBellSchedule.Core.Web
{
    public class MikhailHostingClient
    {
        public static string MikhailHostingBase = "https://mikhail.croomssched.tech";
        private readonly HttpClient _client = new()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public MikhailHostingClient()
        {
            if (!string.IsNullOrEmpty(SettingsManager.Settings.MikhailHostingBase))
                MikhailHostingBase = SettingsManager.Settings.MikhailHostingBase;
        }
        #region Utils
        public static Result<T?> DecodeResponse<T>(string responseText)
        {
            if (responseText.Contains("Too many requests, please try again later."))
            {
                return new() { IsRateLimitReached = true };
            }
            if (responseText.StartsWith("<"))
            {
                return new()
                {
                    OK = false,
                    Exception = new("Ratelimit reached")
                };
            }
            Result<T?> result = new();
            try
            {
                ApiSimpleResponse? resp = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.ApiSimpleResponse);

                if (resp == null)
                {
                    result.OK = false;
                    result.Exception = new Exception("failed to decode json response");
                    return result;
                }

                if (resp.status == "OK")
                {
                    var typeInfo = SourceGenerationContext.Default.GetTypeInfo(typeof(ApiResponse<T>)) ?? throw new Exception("typeinfo not present: " + typeof(ApiResponse<T>).Name);
                    var apiResp = (ApiResponse<T>?)JsonSerializer.Deserialize(responseText, typeInfo);

                    if (apiResp != null)
                    {
                        result.Value = apiResp.data;
                        result.OK = true;
                    }
                    else
                    {
                        result.Exception = new("data decoded was null");
                        result.OK = false;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(resp.code))
                    {
                        result.ErrorCode = resp.code;
                        return result;
                    }

                    var typeInfo = SourceGenerationContext.Default.GetTypeInfo(typeof(ApiResponse<ErrorResponse>)) ?? throw new Exception("typeinfo not present: ApiResponse<ErrorResponse>");
                    var apiResp = (ApiResponse<ErrorResponse>?)JsonSerializer.Deserialize(responseText, typeInfo);

                    result.OK = false;
                    if (apiResp != null)
                    {
                        result.ErrorValue = apiResp.data;
                        result.ErrorCode = apiResp.code;
                    }
                    else
                    {
                        result.Exception = new("Failed to read error response message");
                        result.ErrorCode = "ERR_UNKNOWN";
                    }

                }
            }
            catch (Exception ex)
            {
                result.OK = false;
                result.Exception = ex;
            }

            return result;
        }
        public static string FormatResult<T>(Result<T> result)
        {
            if (result.OK)
                return "Server returned OK";
            if (result.IsRateLimitReached)
                return "Too many requests, try again later";
            if (result.ErrorValue != null)
                return result.ErrorValue.error.Contains("permissions") ? "Your login information has expired or is incorrect. Please login again." : result.ErrorValue.error;
            if (result.Exception != null)
            {
                if (result.Exception is SocketException)
                {
                    return "Network error, check your connection";
                }
                else
                {
                    return result.Exception.Message;
                }
            }
            return "Unspecified error";
        }
        public static string FormatResult(Result result)
        {
            if (result.OK)
                return "Server returned OK";
            if (result.IsRateLimitReached)
                return "Too many requests, try again later";
            if (result.Message != null)
                return result.Message;
            if (result.Exception != null)
            {
                if (result.Exception is SocketException)
                {
                    return "Network error, check your connection";
                }
                else
                {
                    return result.Exception.Message;
                }
            }
            return "Unspecified error";
        }
        private static string DoSHA512(string input)
        {
            var hashedInputBytes = SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(input));

            // Convert to text
            // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
            var hashedInputStringBuilder = new System.Text.StringBuilder(128);
            foreach (var b in hashedInputBytes)
                hashedInputStringBuilder.Append(b.ToString("X2"));
            return hashedInputStringBuilder.ToString();
        }
        private void AddAuthorization()
        {
            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");


            _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"\"{SettingsManager.Settings.SessionID}\"");
        }
        #endregion

        private async Task<Result<T?>> DoGetRequestAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.GetAsync(url, cancellationToken);

                var responseText = await response.Content.ReadAsStringAsync();

                return DecodeResponse<T>(responseText);
            }
            catch (Exception ex)
            {
                return new() { Exception = ex };
            }
        }
        private async Task<Result<T?>> DoPostRequestAsync<T>(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                var content = new StringContent("{}");
                var response = await _client.PostAsync(url, content, cancellationToken);

                var responseText = await response.Content.ReadAsStringAsync();

                return DecodeResponse<T>(responseText);
            }
            catch (Exception ex)
            {
                return new() { Exception = ex };
            }
        }

        public async Task AppStartup()
        {
            try
            {
                await _client.GetAsync(MikhailHostingBase + "/apiv2/telemetry/Startup");
            }
            catch
            {

            }
        }

        public async Task<string?> AuthenticatePrivateBeta(string accessCode)
        {
            var data = new PrivateBetaRequest() { AccessCode = accessCode };
            var reqString = JsonSerializer.Serialize(data, SourceGenerationContext.Default.PrivateBetaRequest);

            try
            {
                var s = new StringContent(reqString);
                s.Headers.ContentType = new("application/json");

                var response = await _client.PostAsync($"{MikhailHostingBase}/apiv2/app/AuthenticatePrivateBeta", s);

                if (!response.IsSuccessStatusCode) return null;

                var responseText = await response.Content.ReadAsStringAsync();
                PrivateBetaResponse? responseJson = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.PrivateBetaResponse);
                if (responseJson == null) return null;
                if (!responseJson.valid) return null;
                if (string.IsNullOrEmpty(responseJson.data)) return null;

                return responseJson.data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> LivestreamExists()
        {
            try
            {
                var response = await _client.GetAsync($"{MikhailHostingBase}/apiv2/app/GetLiveStreamData");

                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    LivestreamAvailabilityResponse? data = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.LivestreamAvailabilityResponse);

                    if (data != null)
                    {
                        return data.exists;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<GetMaintenanceBannerResponse?> GetMaintenanceBanner()
        {
            try
            {
                var response = await _client.GetAsync($"{MikhailHostingBase}/apiv2/app/GetMaintenanceInfo");

                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    GetMaintenanceBannerResponse? data = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.GetMaintenanceBannerResponse);

                    if (data != null)
                    {
                        return data;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
