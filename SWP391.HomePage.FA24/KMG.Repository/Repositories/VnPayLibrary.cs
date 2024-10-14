using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

public class VnPayLibrary
{
    private SortedList<string, string> _requestData = new SortedList<string, string>();
    private SortedList<string, string> _responseData = new SortedList<string, string>();

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
        {
            _requestData.Add(key, value);
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
        {
            _responseData.Add(key, value);
        }
    }

    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var query = string.Join("&", _requestData.Select(x => $"{x.Key}={x.Value}"));
        var secureHash = CreateHash(query, hashSecret);
        return $"{baseUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    public string GetResponseData(string key)
    {
        return _responseData.ContainsKey(key) ? _responseData[key] : null;
    }

    public bool ValidateSignature(string receivedHash, string secretKey)
    {
        var rawHash = string.Join("&", _responseData.Select(x => $"{x.Key}={x.Value}"));
        var computedHash = CreateHash(rawHash, secretKey);
        return receivedHash.Equals(computedHash);
    }

    private string CreateHash(string input, string secretKey)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToUpper();
    }
}

public static class Utils
{
    public static string GetIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
