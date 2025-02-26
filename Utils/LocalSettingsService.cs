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
            var path = Path.Combine(_applicationDataFolder, _localsettingsFile);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var result = JsonSerializer.Deserialize<IDictionary<string, object>>(json);
                if (result != null) _settings = result;
            }


            _isInitialized = true;
        }
    }

    public T? ReadSetting<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
                return JsonSerializer.Deserialize<T>((string)obj);
        }
        else
        {
            InitializeAsync().GetAwaiter().GetResult();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
            {
                if (obj is JsonElement el)
                    return el.Deserialize<T>();
                return (T?)obj;
            }
        }

        return default;
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
                return JsonSerializer.Deserialize<T>((string)obj);
        }
        else
        {
            await InitializeAsync();

            if (_settings != null && _settings.TryGetValue(key, out var obj))
                return JsonSerializer.Deserialize<T>((string)obj);
        }

        return default;
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

            _settings[key] = JsonSerializer.Serialize(value);


            await Task.Run(() =>
            {
                var jSettings = JsonSerializer.Serialize(_settings);
                if (!Directory.Exists(_applicationDataFolder)) Directory.CreateDirectory(_applicationDataFolder);
                File.WriteAllText(Path.Combine(_applicationDataFolder, _localsettingsFile), jSettings, Encoding.UTF8);
            });
        }
    }

    public void SaveSetting<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = JsonSerializer.Serialize(value);
        }
        else
        {
            InitializeAsync().GetAwaiter().GetResult();


            _settings[key] = value;


            Task.Run(() =>
            {
                var jSettings = JsonSerializer.Serialize(_settings);
                if (!Directory.Exists(_applicationDataFolder)) Directory.CreateDirectory(_applicationDataFolder);
                File.WriteAllText(Path.Combine(_applicationDataFolder, _localsettingsFile), jSettings, Encoding.UTF8);
            });
        }
    }
}