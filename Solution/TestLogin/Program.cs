using System.Text;
using System.Text.Json;

// Test login with real API
var httpClient = new HttpClient();

var loginRequest = new
{
    Username = "admin",
    Password = "admin123"
};

var json = JsonSerializer.Serialize(loginRequest);
var content = new StringContent(json, Encoding.UTF8, "application/json");

try
{
    var response = await httpClient.PostAsync("https://localhost:5001/api/auth/login", content);
    var responseContent = await response.Content.ReadAsStringAsync();
    
    Console.WriteLine($"Status: {response.StatusCode}");
    Console.WriteLine($"Response: {responseContent}");
    
    if (response.IsSuccessStatusCode)
    {
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var isSuccess = result.GetProperty("isSuccess").GetBoolean();
        Console.WriteLine($"Authentication Success: {isSuccess}");
        
        if (!isSuccess)
        {
            var errorMessage = result.GetProperty("errorMessage").GetString();
            Console.WriteLine($"Error: {errorMessage}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}