import { apiClient } from "@/api/apiClient";

export interface UserSearchResponse {
    id: string;
    email: string;
}

export const usersApi = {
    async search(email: string): Promise<UserSearchResponse[]> {
        const response = await apiClient.get<UserSearchResponse[]>(
            "/users/search",
            {
                params: { email },
            },
        );

        return response.data;
    },
};