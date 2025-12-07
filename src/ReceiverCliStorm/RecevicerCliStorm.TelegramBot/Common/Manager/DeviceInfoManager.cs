using System.Text.Json;
using ReceiverCliStorm.TelegramBot.Common.Dto;

namespace ReceiverCliStorm.TelegramBot.Common.Manager;

public static class DeviceInfoManager
{
    private static IEnumerable<DeviceInfo> _deviceInfos;

    static DeviceInfoManager()
    {
        _deviceInfos = new List<DeviceInfo>();
    }
    
    public static void SetDevice(string devicePath)
    {
        string data = File.ReadAllText(devicePath);

        IEnumerable<DeviceInfo>? deviceInfos = JsonSerializer.Deserialize<IEnumerable<DeviceInfo>>(data);

        _deviceInfos = deviceInfos ?? throw new NullReferenceException(nameof(deviceInfos));
    }
    
    public static DeviceInfo RandomDevice()
    {
        if (_deviceInfos is null || !_deviceInfos.Any())
        {
            throw new NullReferenceException(nameof(_deviceInfos));
        }

        return _deviceInfos.ElementAt(new Random().Next(0, _deviceInfos.Count()));
    }
}