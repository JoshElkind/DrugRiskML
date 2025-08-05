using DrugRiskAPI.DTOs;
using System.Text.Json;
using System.Text;

namespace DrugRiskAPI.Services
{
    public class TableauService : ITableauService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TableauService> _logger;
        private readonly HttpClient _httpClient;
        private string? _cachedToken;
        private DateTime _tokenExpiry;

        public TableauService(IConfiguration configuration, ILogger<TableauService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<TableauEmbedUrl> GetEmbedUrlAsync(string dashboardName, Dictionary<string, string>? filters = null)
        {
            try
            {
                var serverUrl = _configuration["Tableau:ServerUrl"];
                var siteId = _configuration["Tableau:SiteId"];
                var actualDashboardName = _configuration["Tableau:DashboardName"] ?? dashboardName;

                _logger.LogInformation($"Generating Tableau embed URL for dashboard: {actualDashboardName}");

                var token = await GetTableauTokenAsync();

                string embedUrl = await BuildEmbedUrlAsync(serverUrl, siteId, actualDashboardName, filters, token);

                return new TableauEmbedUrl
                {
                    Url = embedUrl,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    UserContext = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Tableau embed URL");
                throw;
            }
        }

        public async Task<TableauEmbedUrl> GetUserSpecificAnalyticsAsync(string drugName, decimal userRiskScore, string userRiskLevel, string? userId = null)
        {
            try
            {
                var serverUrl = _configuration["Tableau:ServerUrl"];
                var siteId = _configuration["Tableau:SiteId"];
                var actualDashboardName = _configuration["Tableau:DashboardName"] ?? "DrugRiskAnalytics/Dashboard";

                _logger.LogInformation($"Generating user-specific Tableau URL for drug: {drugName}, risk: {userRiskScore}");

                var token = await GetTableauTokenAsync();

                var userFilters = new Dictionary<string, string>
                {
                    ["vf_drugName"] = drugName,
                    ["vf_UserRiskScore"] = userRiskScore.ToString("F1"),
                    ["vf_UserRiskLevel"] = userRiskLevel,
                    ["vf_UserId"] = userId ?? "anonymous"
                };

                await ConfigureSnowflakeDataSourceAsync(token);

                string embedUrl = await BuildEmbedUrlAsync(serverUrl, siteId, actualDashboardName, userFilters, token);

                var userContext = new Dictionary<string, object>
                {
                    ["DrugName"] = drugName,
                    ["UserRiskScore"] = userRiskScore,
                    ["UserRiskLevel"] = userRiskLevel,
                    ["UserId"] = userId ?? "anonymous"
                };

                return new TableauEmbedUrl
                {
                    Url = embedUrl,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    UserContext = userContext
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user-specific Tableau URL");
                throw;
            }
        }

        private async Task<string> BuildEmbedUrlAsync(string serverUrl, string siteId, string dashboardName, Dictionary<string, string>? filters, string token)
        {
            string embedUrl = $"{serverUrl}/t/{siteId}/embed/views/{dashboardName}";

            var urlParams = new List<string>();

            if (filters != null && filters.Any())
            {
                foreach (var filter in filters)
                {
                    urlParams.Add($"{Uri.EscapeDataString(filter.Key)}={Uri.EscapeDataString(filter.Value)}");
                }
            }

            urlParams.Add(":embed=yes");
            urlParams.Add(":showAppBanner=no");
            urlParams.Add(":display_count=no");
            urlParams.Add(":showVizHome=no");
            urlParams.Add(":origin=viz_share_link");
            urlParams.Add(":loadOrderID=0");

            if (!string.IsNullOrEmpty(token) && token != "embed")
            {
                urlParams.Add($"token={token}");
            }

            urlParams.Add(":toolbar=bottom");
            urlParams.Add(":showShareOptions=false");

            if (urlParams.Any())
            {
                embedUrl += "?" + string.Join("&", urlParams);
            }

            return embedUrl;
        }

        private async Task ConfigureSnowflakeDataSourceAsync(string tableauToken)
        {
            try
            {
                var snowflakeUser = Environment.GetEnvironmentVariable("SNOWFLAKE_USER");
                var snowflakePassword = Environment.GetEnvironmentVariable("SNOWFLAKE_PASSWORD");
                var snowflakeAccount = _configuration["Snowflake:Account"];
                var serverUrl = _configuration["Tableau:ServerUrl"];
                var siteId = _configuration["Tableau:SiteId"];

                _logger.LogInformation($"Snowflake credentials check - User: {(string.IsNullOrEmpty(snowflakeUser) ? "Not set" : "Set")}, Password: {(string.IsNullOrEmpty(snowflakePassword) ? "Not set" : "Set")}, Account: {snowflakeAccount}");

                if (string.IsNullOrEmpty(snowflakeUser) || string.IsNullOrEmpty(snowflakePassword) || 
                    string.IsNullOrEmpty(tableauToken) || tableauToken == "embed")
                {
                    _logger.LogWarning("Cannot configure Snowflake data source - missing credentials or token");
                    return;
                }

                await UpdateDataSourceCredentialsAsync(serverUrl, siteId, tableauToken, snowflakeUser, snowflakePassword, snowflakeAccount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring Snowflake data source");
            }
        }

        private async Task UpdateDataSourceCredentialsAsync(string serverUrl, string siteId, string token, string dbUser, string dbPassword, string? dbAccount = null)
        {
            try
            {
                var dataSourcesUrl = $"{serverUrl}/api/3.19/sites/{siteId}/datasources";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Tableau-Auth", token);
                _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var response = await _httpClient.GetAsync(dataSourcesUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get data sources: {response.StatusCode}");
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Data sources response length: {content.Length}");

                if (content.Contains("snowflake", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found Snowflake data sources - attempting to update credentials");
                    
                    var dataSourceIds = ExtractDataSourceIds(content);
                    
                    foreach (var dataSourceId in dataSourceIds)
                    {
                        await UpdateDataSourceConnectionAsync(serverUrl, siteId, token, dataSourceId, dbUser, dbPassword, dbAccount);
                    }
                }
                else
                {
                    _logger.LogInformation("No Snowflake data sources found in response");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating data source credentials");
            }
        }

        private List<string> ExtractDataSourceIds(string xmlContent)
        {
            var dataSourceIds = new List<string>();
            try
            {
                var lines = xmlContent.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("id=") && line.Contains("snowflake"))
                    {
                        var idStart = line.IndexOf("id=\"") + 4;
                        var idEnd = line.IndexOf("\"", idStart);
                        if (idStart > 3 && idEnd > idStart)
                        {
                            var id = line.Substring(idStart, idEnd - idStart);
                            dataSourceIds.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data source IDs");
            }
            return dataSourceIds;
        }

        private async Task UpdateDataSourceConnectionAsync(string serverUrl, string siteId, string token, string dataSourceId, string dbUser, string dbPassword, string? dbAccount = null)
        {
            try
            {
                // get connections for this data source
                var connectionsUrl = $"{serverUrl}/api/3.19/sites/{siteId}/datasources/{dataSourceId}/connections";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Tableau-Auth", token);

                var response = await _httpClient.GetAsync(connectionsUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get connections for data source {dataSourceId}: {response.StatusCode}");
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Connections response for {dataSourceId}: {content}");

                var updateUrl = $"{serverUrl}/api/3.19/sites/{siteId}/datasources/{dataSourceId}/connections/1";
                
                var updateData = new
                {
                    connection = new
                    {
                        userName = dbUser,
                        password = dbPassword,
                        embedPassword = true
                    }
                };

                var json = JsonSerializer.Serialize(updateData);
                var content2 = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Tableau-Auth", token);

                var updateResponse = await _httpClient.PutAsync(updateUrl, content2);
                if (updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully updated credentials for data source {dataSourceId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to update credentials for data source {dataSourceId}: {updateResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating connection for data source {dataSourceId}");
            }
        }

        public async Task<string> GetTableauTokenAsync()
        {
            try
            {
                
                if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
                {
                    _logger.LogInformation("Using cached Tableau token");
                    return _cachedToken;
                }

                var username = Environment.GetEnvironmentVariable("TABLEAU_USERNAME");
                var email = Environment.GetEnvironmentVariable("TABLEAU_EMAIL");
                var password = Environment.GetEnvironmentVariable("TABLEAU_PASSWORD");
                var serverUrl = _configuration["Tableau:ServerUrl"];
                var siteId = _configuration["Tableau:SiteId"];

                _logger.LogInformation($"Tableau credentials check - Username: {(string.IsNullOrEmpty(username) ? "Not set" : "Set")}, Email: {(string.IsNullOrEmpty(email) ? "Not set" : "Set")}, Password: {(string.IsNullOrEmpty(password) ? "Not set" : "Set")}");

                // use username if available, otherwise use email
                var authUsername = !string.IsNullOrEmpty(username) ? username : email;

                // try to get authenticated token
                if (!string.IsNullOrEmpty(authUsername) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(serverUrl))
                {
                    _logger.LogInformation($"Attempting to get authenticated Tableau token for user: {authUsername}");
                    var token = await AuthenticateWithTableauAsync(serverUrl, siteId ?? "jdelkind-f1b10c20eb", authUsername, password);
                    
                    if (!string.IsNullOrEmpty(token))
                    {
                        _cachedToken = token;
                        _tokenExpiry = DateTime.UtcNow.AddMinutes(50); 
                        _logger.LogInformation("Successfully obtained authenticated Tableau token");
                        return token;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to obtain authenticated token, falling back to public embedding");
                    }
                }
                else
                {
                    _logger.LogInformation("Tableau credentials not available, using public embedding");
                }

                var trustedToken = await GetTrustedTicketAsync();
                if (!string.IsNullOrEmpty(trustedToken))
                {
                    return trustedToken;
                }

                _logger.LogInformation("Using public embedding mode");
                return "embed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Tableau token");
                return "embed";
            }
        }

        private async Task<string> AuthenticateWithTableauAsync(string serverUrl, string siteId, string username, string password)
        {
            try
            {
                var authUrl = $"{serverUrl}/api/3.19/auth/signin";
                
                var siteConfigurations = new[]
                {
                    new { contentUrl = siteId },
                    new { contentUrl = "" }, // Default site
                    new { contentUrl = "jdelkind-f1b10c20eb" }
                };

                foreach (var siteConfig in siteConfigurations)
                {
                    var requestBody = new
                    {
                        credentials = new
                        {
                            name = username,
                            password = password,
                        },
                        site = siteConfig
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogInformation($"Attempting Tableau authentication with URL: {authUrl}");
                    _logger.LogInformation($"Request payload: {json}");

                    var response = await _httpClient.PostAsync(authUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation($"Tableau API response status: {response.StatusCode}");
                    _logger.LogInformation($"Response content: {responseContent}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Parse the token from XML response
                        var token = ExtractTokenFromResponse(responseContent);
                        if (!string.IsNullOrEmpty(token))
                        {
                            _logger.LogInformation($"Authentication successful with site: {siteConfig.contentUrl}");
                            return token;
                        }
                    }
                    
                    _logger.LogWarning($"Authentication failed for site '{siteConfig.contentUrl}': {response.StatusCode}");
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Tableau authentication");
                return string.Empty;
            }
        }

        private async Task<string> GetTrustedTicketAsync()
        {
            try
            {
                var trustedUrl = _configuration["Tableau:TrustedUrl"];
                var trustedUsername = _configuration["Tableau:TrustedUsername"];
                
                if (string.IsNullOrEmpty(trustedUrl) || string.IsNullOrEmpty(trustedUsername))
                {
                    return string.Empty;
                }

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", trustedUsername),
                    new KeyValuePair<string, string>("target_site", _configuration["Tableau:SiteId"] ?? ""),
                });

                var response = await _httpClient.PostAsync(trustedUrl, formData);
                var ticket = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && ticket != "-1")
                {
                    _logger.LogInformation("Successfully obtained trusted ticket");
                    return ticket;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trusted ticket");
                return string.Empty;
            }
        }

        private string ExtractTokenFromResponse(string responseContent)
        {
            try
            {
                // Handle both JSON and XML responses
                if (responseContent.Contains("\"token\""))
                {
                    // JSON response
                    var tokenStart = responseContent.IndexOf("\"token\":\"") + 9;
                    var tokenEnd = responseContent.IndexOf("\"", tokenStart);
                    if (tokenStart > 8 && tokenEnd > tokenStart)
                    {
                        return responseContent.Substring(tokenStart, tokenEnd - tokenStart);
                    }
                }
                else if (responseContent.Contains("<credentials") && responseContent.Contains("token="))
                {
                    // XML response
                    var tokenStart = responseContent.IndexOf("token=\"") + 7;
                    var tokenEnd = responseContent.IndexOf("\"", tokenStart);
                    if (tokenStart > 6 && tokenEnd > tokenStart)
                    {
                        return responseContent.Substring(tokenStart, tokenEnd - tokenStart);
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<List<string>> GetAvailableDashboardsAsync()
        {
            try
            {
                return new List<string>
                {
                    "DrugRiskAnalytics/Dashboard",
                    "DrugRiskAnalytics/RiskDashboard",
                    "DrugRiskAnalytics/Average Risks",
                    "DrugRiskAnalytics/Level Distribution",
                    "DrugRiskAnalytics/Risk Score Popularity",
                    "DrugRiskAnalytics/Risk Statistics"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available dashboards");
                return new List<string> { "DrugRiskAnalytics/Dashboard" };
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                if (token == "embed")
                    return true;

                if (token == _cachedToken && DateTime.UtcNow < _tokenExpiry)
                    return true;

             
                return token.Length > 10 && !token.Contains(" ");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Tableau token");
                return false;
            }
        }
    }
} 