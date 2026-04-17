using CroomsBellSchedule.Service;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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

        public async Task<MikhailHostingEventsRespose?> GetEvents()
        {
               try
            {
                var response = await _client.GetAsync($"{MikhailHostingBase}/apiv2/bell/getevents");

                if (response.IsSuccessStatusCode)
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    MikhailHostingEventsRespose? data = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.MikhailHostingEventsRespose);

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
    }
}
