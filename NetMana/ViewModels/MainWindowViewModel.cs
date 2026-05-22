using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetMana.Models;
using NetworkManager.DBus;
using Tmds.DBus;

namespace NetMana.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isLoading;
    
    public WifiApData WifiData { get; } = new();

    [ObservableProperty]
    private bool _isPasswordPromptVisible;

    [ObservableProperty]
    private string _currentPasswordInput = string.Empty;

    private WifiAp? _pendingWifi;

    public MainWindowViewModel()
    {
        WifiData.LoadData();
    }

    [RelayCommand]
    private async Task LoadWifiNetworksAsync()
    {
        IsLoading = true;
        WifiData.WifiList.Clear();
        
        try
        {
            var ap_list = await WifiAp.GetSsidsInRangeAsync();
            
            foreach (var ap in ap_list)
            {
                int index = -1;
                for (int i = 0; i < WifiData.WifiList.Count; i++)
                {
                    if(WifiData.WifiList[i].Ssid == ap.Ssid)
                    {
                        index = i;
                        break;
                    }
                }
                if(index == -1)
                    WifiData.WifiList.Add(ap);
                else if ((int)WifiData.WifiList[index].Strength < (int)ap.Strength && WifiData.WifiList[index].Ssid == ap.Ssid)
                    WifiData.WifiList[index] =  ap;
            }
        }
        finally
        {
            
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task WifiAction(WifiAp ap)
    {
        if (string.IsNullOrEmpty(ap.Password))
        {
            _pendingWifi = ap;
            CurrentPasswordInput = string.Empty;
            IsPasswordPromptVisible = true;
        }
        else
        {
            await WifiConnect(ap);
        }
    }

    [RelayCommand]
    private async Task ConfirmPassword()
    {
        if (_pendingWifi != null)
        {
            _pendingWifi.Password = CurrentPasswordInput ?? string.Empty;
            IsPasswordPromptVisible = false;
            WifiData.SaveData();
            await WifiConnect(_pendingWifi);
            _pendingWifi = null;
        }
    }

    [RelayCommand]
    private void CancelPassword()
    {
        IsPasswordPromptVisible = false;
        _pendingWifi = null;
    }

    private async Task WifiConnect(WifiAp ap)
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
            throw new InvalidOperationException("No Wi-Fi device found.");

        var wireless = connection.CreateProxy<IWireless>(
            "org.freedesktop.NetworkManager",
            wifiDevicePath);

        await wireless.RequestScanAsync(new Dictionary<string, object>());

        await Task.Delay(3000);

        var accessPointPaths = await wireless.GetAccessPointsAsync();

        ObjectPath targetAccessPointPath = default;

        foreach (var accessPointPath in accessPointPaths)
        {
            var accessPoint = connection.CreateProxy<IAccessPoint>(
                "org.freedesktop.NetworkManager",
                accessPointPath);

            var ssidBytes = await accessPoint.GetSsidAsync();
            var accessPointSsid = Encoding.UTF8.GetString(ssidBytes);

            if (accessPointSsid == ap.Ssid)
            {
                targetAccessPointPath = accessPointPath;
                break;
            }
        }

        if (targetAccessPointPath == default)
            throw new InvalidOperationException($"Wi-Fi network '{ap.Ssid}' was not found.");

        var settings = new Dictionary<string, IDictionary<string, object>>
        {
            ["connection"] = new Dictionary<string, object>
            {
                ["id"] = ap.Ssid,
                ["type"] = "802-11-wireless",
                ["uuid"] = Guid.NewGuid().ToString()
            },
            ["802-11-wireless"] = new Dictionary<string, object>
            {
                ["ssid"] = Encoding.UTF8.GetBytes(ap.Ssid),
                ["mode"] = "infrastructure"
            },
            ["802-11-wireless-security"] = new Dictionary<string, object>
            {
                ["key-mgmt"] = "wpa-psk",
                ["psk"] = ap.Password
            },
            ["ipv4"] = new Dictionary<string, object>
            {
                ["method"] = "auto"
            },
            ["ipv6"] = new Dictionary<string, object>
            {
                ["method"] = "auto"
            }
        };

        await networkManager.AddAndActivateConnectionAsync(
            settings,
            wifiDevicePath,
            targetAccessPointPath);
    }
    private void ConnectionRefused(WifiAp ap)
    {
        ap.IsConnectionRefused = true;
        // App returns to main window (IsPasswordPromptVisible = false)
        IsPasswordPromptVisible = false;
    }
}