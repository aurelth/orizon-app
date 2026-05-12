export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  profilePictureUrl: string | null;
  locationName: string;
  latitude: number;
  longitude: number;
  timezone: string;
  isTraveling: boolean;
  travelLocationName: string | null;
  themePreference: 'Dark' | 'Light';
  googleConnected: boolean;
  trelloEnabled: boolean;
}