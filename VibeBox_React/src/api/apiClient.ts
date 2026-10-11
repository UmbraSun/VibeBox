
import axios, { type InternalAxiosRequestConfig } from "axios";
import { useAuthStore } from "@/features/auth/store/authStore";

const baseURL = import.meta.env.VITE_API_BASE_URL;

export const apiClient = axios.create({
  baseURL,
  headers: {
    "Content-Type": "application/json",
  },
});

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
}

type RetriableRequest = InternalAxiosRequestConfig & {
  _retry?: boolean;
};

let refreshPromise: Promise<string> | null = null;

function refreshAccessToken(): Promise<string> {
  const { refreshToken, setTokens, clearTokens } =
    useAuthStore.getState();

  if (!refreshToken) {
    clearTokens();
    return Promise.reject(new Error("Refresh token is missing."));
  }

  if (!refreshPromise) {
    refreshPromise = axios
      .post<TokenResponse>(
        `${baseURL}/auth/refresh`,
        { refreshToken },
        {
          headers: {
            "Content-Type": "application/json",
          },
        },
      )
      .then(({ data }) => {
        setTokens(data.accessToken, data.refreshToken);
        return data.accessToken;
      })
      .catch((error: unknown) => {
        clearTokens();
        throw error;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

apiClient.interceptors.request.use((config) => {
  const accessToken = useAuthStore.getState().accessToken;

  if (accessToken) {
    config.headers.set("Authorization", `Bearer ${accessToken}`);
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401) {
      return Promise.reject(error);
    }

    const originalRequest = error.config as RetriableRequest | undefined;

    if (!originalRequest || originalRequest._retry) {
      return Promise.reject(error);
    }

    const url = originalRequest.url ?? "";
    const isAuthEndpoint = [
      "/auth/login",
      "/auth/register",
      "/auth/refresh",
    ].some((endpoint) => url.includes(endpoint));

    if (isAuthEndpoint) {
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      const accessToken = await refreshAccessToken();

      originalRequest.headers.set(
        "Authorization",
        `Bearer ${accessToken}`,
      );

      return apiClient(originalRequest);
    } catch {
      return Promise.reject(error);
    }
  },
);
