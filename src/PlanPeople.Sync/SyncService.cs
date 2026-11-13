using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlanPeople.Core.Models;
using PlanPeople.Sync.Crypto;
using PlanPeople.Sync.WebDav;

namespace PlanPeople.Sync;

public class SyncService
{
    private readonly WebDavClient _webDav;
    private readonly string _encryptPass;

    public SyncService(string baseUrl, string username, string password, string encryptPass)
    {
        _webDav = new WebDavClient(baseUrl, username, password);
        _encryptPass = encryptPass;
    }

    public async Task<bool> PushEventsAsync(IEnumerable<CalendarEvent> events, string fileName = "work.ics")
    {
        try
        {
            var json = JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
            var encryptedBytes = AesGcmCipher.Encrypt(json, _encryptPass);

            // 改为纯英文路径，规避中文编码问题
            var remotePath = $"PlanPeople/{fileName}";
            return await _webDav.UploadFileAsync(remotePath, encryptedBytes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Push 失败: {ex.Message}");
            return false;
        }
    }

    public async Task<List<CalendarEvent>> PullEventsAsync(string fileName = "work.ics")
    {
        try
        {
            var remotePath = $"PlanPeople/{fileName}";
            var encryptedBytes = await _webDav.DownloadFileAsync(remotePath);
            if (encryptedBytes == null || encryptedBytes.Length == 0) 
                return new List<CalendarEvent>();

            var json = AesGcmCipher.Decrypt(encryptedBytes, _encryptPass);
            var events = JsonSerializer.Deserialize<List<CalendarEvent>>(json);
            return events ?? new List<CalendarEvent>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pull 失败: {ex.Message}");
            return new List<CalendarEvent>();
        }
    }
}