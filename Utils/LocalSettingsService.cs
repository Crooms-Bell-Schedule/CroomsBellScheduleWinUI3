using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using CroomsBellScheduleCS.Utils;

public class LocalSettingsService
{
    private const string _defaultApplicationDataFolder = "CroomsBellSchedule/";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";
    private readonly string _applicationDataFolder;

    private readonly string _localApplicationData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private readonly string _localsettingsFile;

    private bool _isInitialized;

    private IDictionary<string, object> _settings;

    public LocalSettingsService()
    {
        _applicationDataFolder = Path.Combine(_localApplicationData, _defaultApplicationDataFolder);
        _localsettingsFile = _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            string path = Path.Combine(_applicationDataFolder, _localsettingsFile);
            if (File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path);
                var result = JsonSerializer.Deserialize<IDictionary<string, object>>(json);
                if (result != null) _settings = result;
            }


            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key, T norm)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object? obj))
                return JsonSerializer.Deserialize<T>((string)obj);
            return norm;
        }
        else
        {
            await InitializeAsync();

            if (_settings != null && _settings.TryGetValue(key, out object? obj))
            {
                if (obj is JsonElement el)
                    return el.Deserialize<T>();
                return (T?)obj;
            }
        }

        return norm;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = JsonSerializer.Serialize(value);
        }
        else
        {
            await InitializeAsync();

            if (value != null)
                _settings[key] = value;


            await Task.Run(() =>
            {
                string jSettings = JsonSerializer.Serialize(_settings);
                if (!Directory.Exists(_applicationDataFolder)) Directory.CreateDirectory(_applicationDataFolder);
                File.WriteAllText(Path.Combine(_applicationDataFolder, _localsettingsFile), jSettings, Encoding.UTF8);
            });
        }
    }
}