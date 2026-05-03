export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  email: string;
  displayName: string;
  expiresIn: number;
}