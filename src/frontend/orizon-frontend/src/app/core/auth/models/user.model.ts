export interface User {
  id: string;
  email: string;
  displayName: string;
  profilePictureUrl?: string;
  timezone: string;
  themePreference: 'dark' | 'light';
}