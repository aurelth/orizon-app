namespace Orizon.Application.DTOs.Weather;

public class WeatherDto
{
    public double CurrentTemperature { get; set; }
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public string WeatherEmoji { get; set; } = string.Empty;
    public double Humidity { get; set; }
    public double WindSpeed { get; set; }
    public double Visibility { get; set; }
    public string LocationName { get; set; } = string.Empty;

    // Precipitação hora a hora — chave = hora (0-23), valor = mm de chuva
    public Dictionary<int, double> HourlyPrecipitation { get; set; } = new();

    // Janela de chuva — null se não vai chover
    public int? RainStartHour { get; set; }
    public int? RainEndHour { get; set; }
    public bool WillRain => RainStartHour.HasValue;
}