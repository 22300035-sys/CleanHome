export interface RegisterUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface RegisterUserResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isEmailVerified: boolean;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  deviceName: string;
  platform: string;
}

export interface AuthenticationResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  sessionExpiresAt: string;
  sessionId: string;
  tokenType: string;
}

export interface SessionResponse {
  id: string;
  deviceName: string;
  platform: string;
  createdAt: string;
  lastActivityAt: string;
  expiresAt: string;
  isCurrent: boolean;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isEmailVerified: boolean;
}

export interface EmailAddressRequest {
  email: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface AccountRecoveryMessage {
  message: string;
}
