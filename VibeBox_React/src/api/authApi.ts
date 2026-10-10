import { apiClient } from "@/api/apiClient";

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface RegisterResponse {
  userId: string;
  message: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export type RefreshTokenResponse = LoginResponse;

export const authApi = {
  async register(
    request: RegisterRequest,
  ): Promise<RegisterResponse> {
    const response = await apiClient.post<RegisterResponse>(
      "/auth/register",
      request,
    );

    return response.data;
  },

  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>(
      "/auth/login",
      request,
    );

    return response.data;
  },

  async refresh(
    request: RefreshTokenRequest,
  ): Promise<RefreshTokenResponse> {
    const response = await apiClient.post<RefreshTokenResponse>(
      "/auth/refresh",
      request,
    );

    return response.data;
  },
};
