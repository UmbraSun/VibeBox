
import { apiClient } from "@/api/apiClient";

export interface MessageResponse {
    id: string;
    channelId: string;
    senderId: string;
    senderEmail: string;
    content: string;
    createdAtUtc: string;
    updatedAtUtc: string;
}

export interface SendMessageRequest {
    content: string;
}

interface SendMessageResponse {
    id: string;
}

export const messagesApi = {
    async getChannelMessages(
        channelId: string,
        limit = 50,
    ): Promise<MessageResponse[]> {
        const response = await apiClient.get<MessageResponse[]>(
            `/channels/${channelId}/messages`,
            { params: { limit } },
        );

        return response.data;
    },

    async sendMessage(
        channelId: string,
        request: SendMessageRequest,
    ): Promise<SendMessageResponse> {
        const response = await apiClient.post<SendMessageResponse>(
            `/channels/${channelId}/messages`,
            request,
        );

        return response.data;
    },
};
