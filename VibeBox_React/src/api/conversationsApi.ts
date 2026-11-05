import { apiClient } from "@/api/apiClient";
import type { MessageResponse, SendMessageRequest } from "@/api/messagesApi";

export interface ConversationSummaryResponse {
    id: string;
    type: string;
    name: string | null;
    createdAtUtc: string;
    lastMessageAtUtc: string | null;
    otherUserId: string | null;
    otherUserEmail: string | null;
    lastReadAtUtc: string | null;
    lastMessageContent: string | null;
    lastMessageCreatedAtUtc: string | null;
    lastMessageSenderId: string | null;
    lastMessageHasAttachments: boolean;
}

interface CreateDirectConversationResponse {
    id: string;
}

interface SendConversationMessageResponse {
    id: string;
}

export const conversationsApi = {
    async getMyConversations(): Promise<ConversationSummaryResponse[]> {
        const response = await apiClient.get<ConversationSummaryResponse[]>(
            "/conversations",
        );

        return response.data;
    },

    async createDirect(userId: string): Promise<string> {
        const response = await apiClient.post<CreateDirectConversationResponse>(
            "/conversations/direct",
            { userId },
        );

        return response.data.id;
    },

    async getMessages(
        conversationId: string,
        limit = 50,
        beforeMessageId?: string,
    ): Promise<MessageResponse[]> {
        const response = await apiClient.get<MessageResponse[]>(
            `/conversations/${conversationId}/messages`,
            {
                params: {
                    limit,
                    beforeMessageId,
                },
            },
        );

        return response.data;
    },

    async sendMessage(
        conversationId: string,
        request: SendMessageRequest,
    ): Promise<SendConversationMessageResponse> {
        const response = await apiClient.post<SendConversationMessageResponse>(
            `/conversations/${conversationId}/messages`,
            request,
        );

        return response.data;
    },

    async uploadAttachments(
        conversationId: string,
        files: File[],
        content?: string,
    ) {
        const formData = new FormData();
        const trimmedContent = content?.trim();

        if (trimmedContent) {
            formData.append("content", trimmedContent);
        }

        files.forEach((file) => {
            formData.append("files", file);
        });

        return apiClient.post<{ id: string }>(
            `/conversations/${conversationId}/messages/attachments`,
            formData,
        );
    },

    async deleteMessage(
        conversationId: string,
        messageId: string,
    ): Promise<void> {
        await apiClient.delete(
            `/conversations/${conversationId}/messages/${messageId}`,
        );
    },
};