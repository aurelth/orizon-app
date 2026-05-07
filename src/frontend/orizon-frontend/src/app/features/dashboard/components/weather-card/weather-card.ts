import { Component, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WeatherData } from '../../../../core/briefing/models/briefing.model';

@Component({
  selector: 'app-weather-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './weather-card.html',
  styleUrl: './weather-card.scss',
})
export class WeatherCardComponent {
  readonly weather = input<WeatherData>({} as WeatherData);
  readonly weatherSummary = input<string>('');

  readonly precipitationHours = computed(() =>
    Object.entries(this.weather().hourlyPrecipitation ?? {})
      .map(([hour, value]) => ({ hour: Number(hour), value }))
      .sort((a, b) => a.hour - b.hour)
      .slice(6, 22)
  );

  readonly maxPrecipitation = computed(() => {
    const values = this.precipitationHours().map((h) => h.value);
    return Math.max(...values, 1);
  });

  formatHour(hour: number): string {
    return `${hour}h`;
  }
}