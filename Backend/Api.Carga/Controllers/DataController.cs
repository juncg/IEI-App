public class CargaService
{
    private readonly HttpClient _httpClient;

    public CargaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> GetInfoFromExternalApi(int id) //this one will return raw data
    {
        // 1 = CV , 2 = CAT , 3 = CAT    
        if (id == 1)
        {
            var response = await _httpClient.GetAsync($"https://apiValenciana.com/users/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return json;
        }
        else if (id == 2)
        {
            var response = await _httpClient.GetAsync($"https://apiCatalana.com/users/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return json;
        }
        else if (id == 3)
        {
            var response = await _httpClient.GetAsync($"https://apiGallega.com/users/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return json;
        }
        else
        {
            return null;
        }

    }
}