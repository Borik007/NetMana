using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NetworkManager.DBus;
using Tmds.DBus;

namespace NetMana.Models;

public partial class WifiAp : ObservableObject
{
    [ObservableProperty]
    private string _ssid = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isConnectionRefused;
    
    [ObservableProperty]
    private byte _strength;

    public string MacAddress { get; set; } = string.Empty;
    public ObjectPath Path { get; set; }

    public int StrengthInt => (int)Strength;
    
    public static async Task<List<WifiAp>> GetSsidsInRangeAsync()
    {
        var connection = new Connection(Address.System);
        await connection.ConnectAsync();

        var networkManager = connection.CreateProxy<INetworkManager>(
            "org.freedesktop.NetworkManager",
            "/org/freedesktop/NetworkManager");

        var devicePaths = await networkManager.GetDevicesAsync();

        ObjectPath wifiDevicePath = default;

        foreach (var devicePath in devicePaths)
        {
            var device = connection.CreateProxy<IDevice>(
                "org.freedesktop.NetworkManager",
                devicePath);

            var deviceType = await device.GetDeviceTypeAsync();

            if (deviceType == 2) // NM_DEVICE_TYPE_WIFI
            {
                wifiDevicePath = devicePath;
                break;
            }
        }

        if (wifiDevicePath == default)
            return new List<WifiAp>();

        var wireless = connection.CreateProxy<IWireless>(
            "org.freedesktop.NetworkManager",
            wifiDevicePath);

        await wireless.RequestScanAsync(new Dictionary<string, object>());

        await Task.Delay(3000);

        var accessPointPaths = await wireless.GetAccessPointsAsync();

        var result = new List<WifiAp>();

        foreach (var accessPointPath in accessPointPaths)
        {
            var accessPoint = connection.CreateProxy<IAccessPoint>(
                "org.freedesktop.NetworkManager",
                accessPointPath);

            var props = await accessPoint.GetAllAsync();

            var ssid = Encoding.UTF8.GetString(props.Ssid);

            if (string.IsNullOrWhiteSpace(ssid))
                continue;
                
            var res = new WifiAp();
            res.Ssid = ssid;
            res.Strength = props.Strength;
            res.MacAddress = props.HwAddress;
            res.Path = accessPointPath;
            
            result.Add(res);
        }

        return result;
    }
}
