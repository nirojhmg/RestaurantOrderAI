using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class RestaurantApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public RestaurantApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["RestaurantApi:BaseUrl"]
                   ?? throw new InvalidOperationException("RestaurantApi:BaseUrl is not configured.");
    }

    // GET method for restaurant info
    public async Task<RestaurantInfo> GetRestaurantInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}GetCoinf");
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            if (apiResponse?.IsSuccess != true) return null;
            var list = JsonSerializer.Deserialize<List<RestaurantInfo>>(apiResponse.Result);
            return list?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    // POST method for menu groups
    public async Task<List<MenuGroup>> GetMenuGroupsAsync(string mobileNumber, string rrNo)
    {
        try
        {
            var url = $"{_baseUrl}MenuGroup?MobileNumber={mobileNumber}&RRNo={rrNo}";
            var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            if (apiResponse?.IsSuccess != true) return new List<MenuGroup>();
            return JsonSerializer.Deserialize<List<MenuGroup>>(apiResponse.Result);
        }
        catch
        {
            return new List<MenuGroup>();
        }
    }

    // POST method for menu items
    public async Task<List<MenuItem>> GetMenuItemsAsync(string mobileNumber, string rrNo, string groupName)
    {
        try
        {
            var url = $"{_baseUrl}MenuItem?MobileNumber={mobileNumber}&RRNo={rrNo}&MenuGroup={Uri.EscapeDataString(groupName)}";
            var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            if (apiResponse?.IsSuccess != true) return new List<MenuItem>();
            return JsonSerializer.Deserialize<List<MenuItem>>(apiResponse.Result);
        }
        catch
        {
            return new List<MenuItem>();
        }
    }

    // POST method for saving order
    public async Task<bool> SaveOrderAsync(string mobileNumber, string rrNo, string tableNum, string iNum, string postingType, int qty, string remarks)
    {
        try
        {
            var url = $"{_baseUrl}SaveOrder?MobileNumber={mobileNumber}&RRNo={rrNo}&TableNum={tableNum}&INum={iNum}&PostingType={postingType}&Qty={qty}&OrderRemarks={Uri.EscapeDataString(remarks)}";
            var response = await _httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            return apiResponse?.IsSuccess == true;
        }
        catch
        {
            return false;
        }
    }
}