using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PlanPeople.App.Services;

public class AppSettings
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "zh-CN";
    public bool CloseToTray { get; set; } = true;
    public bool RunAtLogin { get; set; } = false;

    // 在 AppSettings 类中加入：
    public double IslandWidth { get; set; } = 320;
    public double IslandHeight { get; set; } = 48;
    public string IslandStyle { get; set; } = "Island"; // "Island" 或 "TopBar"

    // 新增：首次启动标志
    public bool IsFirstRun { get; set; } = true;

    // AI 配置
    public string AiBaseUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4";
    public string AiApiKeyEnc { get; set; } = ""; 
    public string AiModel { get; set; } = "glm-4-flash";

    // WebDAV 同步配置
    public string WebDavUrl { get; set; } = "";
    public string WebDavUser { get; set; } = "";
    public string WebDavPassEnc { get; set; } = "";
    public string SyncEncryptPassEnc { get; set; } = "";
    // 在 AppSettings 类中添加：
public string UserName { get; set; } = "";

    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }

    public string GetAiApiKey() => Decrypt(AiApiKeyEnc);
    public void SetAiApiKey(string apiKey) => AiApiKeyEnc = Encrypt(apiKey);
    public string GetWebDavPass() => Decrypt(WebDavPassEnc);
    public void SetWebDavPass(string pass) => WebDavPassEnc = Encrypt(pass);
    public string GetSyncEncryptPass() => Decrypt(SyncEncryptPassEnc);
    public void SetSyncEncryptPass(string pass) => SyncEncryptPassEnc = Encrypt(pass);

    private static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        try
        {
            var data = Encoding.UTF8.GetBytes(plainText);
            var bytes = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }
        catch { return ""; }
    }

    private static string Decrypt(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64)) return "";
        try
        {
            var bytes = Convert.FromBase64String(encryptedBase64);
            var data = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch { return ""; }
    }
}