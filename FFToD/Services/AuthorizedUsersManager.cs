using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace FFToD.Services;

public class AuthorizedUsersManager
{
    private readonly IPluginLog pluginLog;
    private readonly HttpClient httpClient;
    private readonly string githubUrl = "https://raw.githubusercontent.com/kirin-xiv/KirinPlugins/main/plugins/FFToD/authorized-users.txt";
    private readonly string localCacheFile;
    
    private List<string> cachedUsers = new();
    private DateTime lastFetch = DateTime.MinValue;
    private readonly TimeSpan cacheExpiry = TimeSpan.FromMinutes(5); // Cache for 5 minutes
    
    public AuthorizedUsersManager(IPluginLog pluginLog, string pluginDirectory)
    {
        this.pluginLog = pluginLog;
        this.httpClient = new HttpClient();
        this.httpClient.Timeout = TimeSpan.FromSeconds(10);
        this.localCacheFile = Path.Combine(pluginDirectory, "authorized-users-cache.txt");
        
        // Load cached users on startup
        LoadCachedUsers();
        
        // Start background refresh
        _ = Task.Run(RefreshUsersAsync);
    }

    public async Task<List<string>> GetAuthorizedUsersAsync()
    {
        // Return cached users if they're recent enough
        if (DateTime.Now - lastFetch < cacheExpiry && cachedUsers.Count > 0)
        {
            return new List<string>(cachedUsers);
        }

        // Try to fetch fresh users
        try
        {
            await RefreshUsersAsync();
        }
        catch (Exception ex)
        {
            pluginLog.Warning($"Failed to refresh authorized users: {ex.Message}");
        }

        return new List<string>(cachedUsers);
    }

    public List<string> GetAuthorizedUsersSync()
    {
        return new List<string>(cachedUsers);
    }

    private async Task RefreshUsersAsync()
    {
        try
        {
            pluginLog.Debug("Fetching authorized users from GitHub...");
            var response = await httpClient.GetStringAsync(githubUrl);
            
            var users = new List<string>();
            var lines = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                {
                    users.Add(trimmed);
                }
            }
            
            cachedUsers = users;
            lastFetch = DateTime.Now;
            
            // Save to local cache
            await SaveCachedUsersAsync();
            
            pluginLog.Information($"Successfully loaded {users.Count} authorized users from GitHub");
        }
        catch (HttpRequestException ex)
        {
            pluginLog.Warning($"HTTP error fetching authorized users: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            pluginLog.Warning("Timeout fetching authorized users from GitHub");
        }
        catch (Exception ex)
        {
            pluginLog.Error($"Unexpected error fetching authorized users: {ex}");
        }
    }

    private void LoadCachedUsers()
    {
        try
        {
            if (File.Exists(localCacheFile))
            {
                var lines = File.ReadAllLines(localCacheFile);
                var users = new List<string>();
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                    {
                        users.Add(trimmed);
                    }
                }
                
                cachedUsers = users;
                pluginLog.Information($"Loaded {users.Count} authorized users from cache");
            }
            else
            {
                pluginLog.Information("No cached users found - will fetch from GitHub");
            }
        }
        catch (Exception ex)
        {
            pluginLog.Error($"Error loading cached users: {ex}");
        }
    }

    private async Task SaveCachedUsersAsync()
    {
        try
        {
            var content = string.Join(Environment.NewLine, cachedUsers);
            await File.WriteAllTextAsync(localCacheFile, content);
        }
        catch (Exception ex)
        {
            pluginLog.Warning($"Failed to save user cache: {ex.Message}");
        }
    }

    public void Dispose()
    {
        httpClient?.Dispose();
    }
}