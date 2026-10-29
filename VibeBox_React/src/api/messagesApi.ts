
import { apiClient } from "@/api/apiClient";

export interface MessageAttachmentResponse {
    id: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
}

export interface MessageResponse {
    id: string;
    channelId: string;
    senderId: string;
    senderEmail: string;
    content: string | null;
    createdAtUtc: string;
    updatedAtUtc: string | null;
    attachments: MessageAttachmentResponse[];
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

    async uploadAttachment(
        channelId: string,
        file: File,
    ): Promise<SendMessageResponse> {
        const formData = new FormData();
        formData.append("file", file);

        const response = await apiClient.post<SendMessageResponse>(
            `/channels/${channelId}/messages/attachments`,
            formData,
        );

        return response.data;
    },

    async getAttachment(attachmentId: string): Promise<Blob> {
        const response = await apiClient.get<Blob>(
            `/attachments/${attachmentId}`,
            { responseType: "blob" },
        );

        return response.data;
    },
};
