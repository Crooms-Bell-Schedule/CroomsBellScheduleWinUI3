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
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
