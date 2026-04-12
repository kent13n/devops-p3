export interface RegisterRequest {
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserDto {
  id: string;
  email: string;
  createdAt: string;
}

export interface AuthResponse {
  user: UserDto;
  token: string;
  expiresAt: string;
}

export interface ErrorResponse {
  code: string;
  message: string;
  details?: Record<string, string>;
}
