using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text.Json;

var tenantId = "tenantId";
var clientId = "clientId"; // из Azure App Registration

var redirectUri = "http://localhost:53100";
var scopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };

var app = PublicClientApplicationBuilder.Create(clientId)
    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
    .WithRedirectUri(redirectUri)
    .Build();

var result = await app.AcquireTokenInteractive(scopes)
                      .WithPrompt(Prompt.ForceLogin)
                      .ExecuteAsync();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\n✅ Access token acquired.\n");
Console.ResetColor();

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

// 🔹 Минимальный запрос — список всех доступных отчётов
var reportsResponse = await httpClient.GetAsync("https://api.powerbi.com/v1.0/myorg/reports");
if (!reportsResponse.IsSuccessStatusCode)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Failed to get reports: {reportsResponse.StatusCode}");
    Console.WriteLine(await reportsResponse.Content.ReadAsStringAsync());
    Console.ResetColor();
    return;
}

using var stream = await reportsResponse.Content.ReadAsStreamAsync();
using var json = await JsonDocument.ParseAsync(stream);

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n📄 Available Reports:\n");
Console.ResetColor();

foreach (var report in json.RootElement.GetProperty("value").EnumerateArray())
{
    var name = report.GetProperty("name").GetString();
    var id = report.GetProperty("id").GetString();
    Console.WriteLine($"📄 {name} (ID: {id})");
}
