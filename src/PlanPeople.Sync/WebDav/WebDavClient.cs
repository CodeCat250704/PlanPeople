using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PlanPeople.Sync.WebDav;

public class WebDavClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public WebDavClient(string baseUrl, string username, string password)
    {
        _baseUrl = baseUrl.TrimEnd('/') + "/";
        
        _httpClient = new HttpClient();
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
    }

    public async Task<bool> UploadFileAsync(string remotePath, byte[] content)
    {
        var url = _baseUrl + remotePath.TrimStart('/');
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new ByteArrayContent(content);
        
        try
        {
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[WebDAV 上传失败] 状态码: {response.StatusCode}, 报错: {errorBody}");
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebDAV 网络异常] {ex.Message}");
            return false;
        }
    }

    public async Task<byte[]?> DownloadFileAsync(string remotePath)
    {
        var url = _baseUrl + remotePath.TrimStart('/');
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[WebDAV 下载失败] 状态码: {response.StatusCode}, 报错: {errorBody}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebDAV 网络异常] {ex.Message}");
        }
        return null;
    }
}