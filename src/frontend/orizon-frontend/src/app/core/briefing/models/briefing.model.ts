export interface WeatherData {
  currentTemperature: number;
  minTemperature: number;
  maxTemperature: number;
  description: string;
  weatherEmoji: string;
  humidity: number;
  windSpeed: number;
  locationName: string;
  hourlyPrecipitation: Record<number, number>;
  rainStartHour: number | null;
  rainEndHour: number | null;
  willRain: boolean;
}

export interface EmailSummary {
  from: string;
  subject: string;
  aiSummary: string;
  category: string;
  categoryEmoji: string;
  receivedAt: string;
}

export interface CalendarEvent {
  title: string;
  startTime: string;
  endTime: string;
  participants: string[];
  meetLink: string | null;
  description: string | null;
  conflictsWithRain: boolean;
  isBirthday: boolean;
  isAllDay: boolean;
}

export interface TrelloTask {
  cardId: string;
  title: string;
  boardName: string;
  boardColor: string;
  listName: string;
  columnType: string;
  movedToInProgressAt: string | null;
  daysInProgress: number | null;
  isStuck: boolean;
}

export interface GoogleTask {
  id: string;
  title: string;
  notes: string | null;
  dueDate: string | null;
  isOverdue: boolean;
  taskListName: string;
}

export interface AISummary {
  greeting: string;
  weatherSummary: string;
  suggestions: string;
  priorityTask: string | null;
  actionChips: string[];
}

export interface BriefingResult {
  briefingId: string;
  date: string;
  userName: string;
  weather: WeatherData;
  emails: EmailSummary[];
  calendarEvents: CalendarEvent[];
  trelloTasks: TrelloTask[] | null;
  googleTasks: GoogleTask[] | null;
  aiSummary: AISummary;
  generatedAt: string;
}