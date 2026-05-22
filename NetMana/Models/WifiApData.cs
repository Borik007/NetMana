using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;


namespace NetMana.Models;

public partial class WifiApData : ObservableObject
{

    public ObservableCollection<WifiAp> WifiList { get; set; } = new ObservableCollection<WifiAp>();
    public ObservableCollection<WifiSavedata> WifiSavedata { get; set; } = new ObservableCollection<WifiSavedata>();
    public void SaveData()
    {
        string path = GetDataPath("NetMana");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        path = System.IO.Path.Combine(path, "WifiAp");
        
        var listToSave = WifiList.Where(ap => !string.IsNullOrEmpty(ap.Password)).ToList();
        foreach (var ap in listToSave)
        {
            WifiSavedata.Add(new WifiSavedata { Ssid = ap.Ssid, Password = ap.Password });
        }
        File.WriteAllText(path, JsonSerializer.Serialize(WifiSavedata));
    }
    public void LoadData()
    {
        string path = GetDataPath("NetMana");
        path = System.IO.Path.Combine(path, "WifiAp");
        if (File.Exists(path))
        {
            var loadedData = JsonSerializer.Deserialize<List<WifiSavedata>>(File.ReadAllText(path));
            if (loadedData != null)
            {
                WifiSavedata = new ObservableCollection<WifiSavedata>(loadedData);
            }
        }
    }

    public void MergeData()
    {
        foreach (var ap in WifiList)
        {
            if(WifiSavedata.Any(saved => saved.Ssid == ap.Ssid))
            {
                ap.Password = WifiSavedata.First(saved => saved.Ssid == ap.Ssid).Password;
            }
        }
    }
    public static string GetDataPath(string appName)
    {
        // 1. Check if the XDG_DATA_HOME environment variable is set
        string? xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

        if (string.IsNullOrEmpty(xdgDataHome))
        {
            // 2. Fallback: Combine the user's Home profile with ".local/share"
            // Environment.SpecialFolder.UserProfile points directly to ~ or /home/username
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            xdgDataHome = System.IO.Path.Combine(homeDir, ".local", "share");
        }

        // 3. Append your application's name to avoid cluttering the base directory
        string fullAppPath = System.IO.Path.Combine(xdgDataHome, appName);

        return fullAppPath;
    }
}
public class WifiSavedata
{
    public string Ssid { get; set; }
    public string Password { get; set; }
}