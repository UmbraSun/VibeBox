
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
    unreadCount: number;
    lastMessageContent: string | null;
    lastMessageCreatedAtUtc: string | null;
    lastMessageSenderId: string | null;
    lastMessageHasAttachments: boolean;
}

export interface ConversationMemberResponse {
    userId: string;
    email: string;
    joinedAtUtc: string;
}

export interface ConversationDetailsResponse {
    id: string;
    type: string;
    name: string | null;
    createdAtUtc: string;
    currentUserId: string;
    members: ConversationMemberResponse[];
}

interface CreateDirectConversationResponse {
    id: string;
}

interface SendConversationMessageResponse {
    id: string;
}

interface CreateGroupConversationResponse {
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

    async createGroup(name: string, userIds: string[]): Promise<string> {
        const response = await apiClient.post<CreateGroupConversationResponse>(
            "/conversations/group",
            {
                name: name.trim(),
                userIds,
            },
        );

        return response.data.id;
    },

    async getDetails(
        conversationId: string,
    ): Promise<ConversationDetailsResponse> {
        const response = await apiClient.get<ConversationDetailsResponse>(
            `/conversations/${conversationId}`,
        );

        return response.data;
    },

    async markAsRead(
        conversationId: string,
        readAtUtc: string,
    ): Promise<void> {
        await apiClient.patch(`/conversations/${conversationId}/read`, {
            readAtUtc,
        });
    },

    async addMembers(
        conversationId: string,
        userIds: string[],
    ): Promise<void> {
        await apiClient.post(
            `/conversations/${conversationId}/members`,
            { userIds },
        );
    },

    async removeMember(
        conversationId: string,
        userId: string,
    ): Promise<void> {
        await apiClient.delete(
            `/conversations/${conversationId}/members/${userId}`,
        );
    },

    async leaveConversation(
        conversationId: string,
    ): Promise<void> {
        await apiClient.delete(
            `/conversations/${conversationId}/members/me`,
        );
    },

    async renameConversation(
        conversationId: string,
        name: string,
    ): Promise<void> {
        await apiClient.patch(
            `/conversations/${conversationId}/name`,
            { name: name.trim() },
        );
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
        replyToMessageId?: string | null,
    ) {
        const formData = new FormData();
        const trimmedContent = content?.trim();

        if (trimmedContent) {
            formData.append("content", trimmedContent);
        }

        if (replyToMessageId) {
            formData.append("replyToMessageId", replyToMessageId);
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
