using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orizon.Application.DTOs.Weather;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.External;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _baseUrl;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Weather:BaseUrl"]
            ?? "https://api.open-meteo.com/v1";
    }

    public async Task<WeatherDto> GetWeatherAsync(
        double latitude,
        double longitude,
        string timezone,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/forecast" +
            $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m,relative_humidity_2m" +
            $"&daily=temperature_2m_max,temperature_2m_min,weather_code" +
            $"&hourly=precipitation,precipitation_probability" +
            $"&timezone={Uri.EscapeDataString(timezone)}" +
            $"&forecast_days=1";

        _logger.LogInformation("Buscando clima para lat={Lat}, lon={Lon}", latitude, longitude);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var current = data.GetProperty("current");
        var daily = data.GetProperty("daily");
        var hourly = data.GetProperty("hourly");

        var currentTemp = current.GetProperty("temperature_2m").GetDouble();
        var weatherCode = current.GetProperty("weather_code").GetInt32();
        var windSpeed = current.GetProperty("wind_speed_10m").GetDouble();
        var maxTemp = daily.GetProperty("temperature_2m_max")[0].GetDouble();
        var minTemp = daily.GetProperty("temperature_2m_min")[0].GetDouble();

        var hourlyTimes = hourly.GetProperty("time").EnumerateArray().ToList();
        var hourlyPrecip = hourly.GetProperty("precipitation").EnumerateArray().ToList();

        var precipitationByHour = new Dictionary<int, double>();
        int? rainStartHour = null;
        int? rainEndHour = null;

        for (var i = 0; i < hourlyTimes.Count; i++)
        {
            var hour = DateTime.Parse(hourlyTimes[i].GetString()!).Hour;
            var precip = hourlyPrecip[i].GetDouble();
            precipitationByHour[hour] = precip;
            if (precip > 0.1)
            {
                rainStartHour ??= hour;
                rainEndHour = hour;
            }
        }
        
        var locationName = await GetLocationNameAsync(latitude, longitude, cancellationToken);

        return new WeatherDto
        {
            CurrentTemperature = currentTemp,
            MinTemperature = minTemp,
            MaxTemperature = maxTemp,
            Description = GetWeatherDescription(weatherCode),
            WeatherEmoji = GetWeatherEmoji(weatherCode),
            WindSpeed = windSpeed,
            Humidity = current.GetProperty("relative_humidity_2m").GetDouble(),
            HourlyPrecipitation = precipitationByHour,
            RainStartHour = rainStartHour,
            RainEndHour = rainEndHour,
            LocationName = locationName,
        };
    }

    private async Task<string> GetLocationNameAsync(
        double latitude, double longitude, CancellationToken ct)
    {
        try
        {
            var url = "https://nominatim.openstreetmap.org/reverse" +
                      $"?lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                      $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}" +
                      $"&format=json&accept-language=pt-BR";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Orizon/1.0 (orizonapp.io)");

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            if (!data.TryGetProperty("address", out var address))
                return string.Empty;

            var city = TryGetString(address, "city")
                    ?? TryGetString(address, "town")
                    ?? TryGetString(address, "village")
                    ?? TryGetString(address, "municipality");

            var state = TryGetString(address, "state");
            var stateAbbrev = GetBrazilianStateAbbreviation(state);

            if (city is null) return string.Empty;

            return stateAbbrev is not null ? $"{city}, {stateAbbrev}" : city;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Falha ao obter nome da localização via geocoding " +
                "para lat={Lat}, lon={Lon}", latitude, longitude);
            return string.Empty;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;

    public static string? GetBrazilianStateAbbreviation(string? stateName) => stateName switch
    {
        "Acre" => "AC",
        "Alagoas" => "AL",
        "Amapá" => "AP",
        "Amazonas" => "AM",
        "Bahia" => "BA",
        "Ceará" => "CE",
        "Distrito Federal" => "DF",
        "Espírito Santo" => "ES",
        "Goiás" => "GO",
        "Maranhão" => "MA",
        "Mato Grosso" => "MT",
        "Mato Grosso do Sul" => "MS",
        "Minas Gerais" => "MG",
        "Pará" => "PA",
        "Paraíba" => "PB",
        "Paraná" => "PR",
        "Pernambuco" => "PE",
        "Piauí" => "PI",
        "Rio de Janeiro" => "RJ",
        "Rio Grande do Norte" => "RN",
        "Rio Grande do Sul" => "RS",
        "Rondônia" => "RO",
        "Roraima" => "RR",
        "Santa Catarina" => "SC",
        "São Paulo" => "SP",
        "Sergipe" => "SE",
        "Tocantins" => "TO",
        _ => null
    };

    private static string GetWeatherDescription(int code) => code switch
    {
        0 => "Céu limpo",
        1 or 2 or 3 => "Parcialmente nublado",
        45 or 48 => "Neblina",
        51 or 53 or 55 => "Chuvisco",
        61 or 63 or 65 => "Chuva",
        71 or 73 or 75 => "Neve",
        80 or 81 or 82 => "Pancadas de chuva",
        95 => "Tempestade",
        96 or 99 => "Tempestade com granizo",
        _ => "Condição desconhecida"
    };

    private static string GetWeatherEmoji(int code) => code switch
    {
        0 => "☀️",
        1 or 2 or 3 => "⛅",
        45 or 48 => "🌫️",
        51 or 53 or 55 => "🌦️",
        61 or 63 or 65 => "🌧️",
        71 or 73 or 75 => "❄️",
        80 or 81 or 82 => "🌦️",
        95 => "⛈️",
        96 or 99 => "⛈️",
        _ => "🌡️"
    };
}