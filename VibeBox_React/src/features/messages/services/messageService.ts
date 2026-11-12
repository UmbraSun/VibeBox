import {
    messagesApi,
    type MessageResponse,
} from "@/api/messagesApi";
import { conversationsApi } from "@/api/conversationsApi";
import { getErrorMessage } from "@/features/messages/utils/errorUtils";

export const messageService = {
    async getMessages(channelId: string): Promise<MessageResponse[]> {
        try {
            return await messagesApi.getChannelMessages(channelId);
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to load messages."),
            );
        }
    },

    async getConversationMessages(
        conversationId: string,
        limit = 50,
        beforeMessageId?: string,
    ): Promise<MessageResponse[]> {
        return conversationsApi.getMessages(
            conversationId,
            limit,
            beforeMessageId,
        );
    },

    async sendMessage(
        channelId: string,
        content: string,
        replyToMessageId?: string | null,
    ): Promise<void> {
        try {
            await messagesApi.sendMessage(channelId, {
                content,
                replyToMessageId,
            });
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to send message."),
            );
        }
    },

    async sendConversationMessage(
        conversationId: string,
        content: string,
        replyToMessageId?: string | null,
    ): Promise<void> {
        try {
            await conversationsApi.sendMessage(conversationId, {
                content,
                replyToMessageId,
            });
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to send message."),
            );
        }
    },

    async sendAttachments(
        channelId: string,
        files: File[],
        content?: string,
    ): Promise<void> {
        try {
            await messagesApi.uploadAttachments(channelId, files, content);
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to upload attachments."),
            );
        }
    },

    async sendConversationAttachments(
        conversationId: string,
        files: File[],
        content?: string,
    ): Promise<void> {
        try {
            await conversationsApi.uploadAttachments(
                conversationId,
                files,
                content,
            );
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to upload attachments."),
            );
        }
    },

    async deleteMessage(
        channelId: string,
        messageId: string,
    ): Promise<void> {
        try {
            await messagesApi.deleteMessage(channelId, messageId);
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to delete message."),
            );
        }
    },

    async deleteConversationMessage(
        conversationId: string,
        messageId: string,
    ): Promise<void> {
        try {
            await conversationsApi.deleteMessage(conversationId, messageId);
        } catch (error: unknown) {
            throw new Error(
                getErrorMessage(error, "Unable to delete message."),
            );
        }
    },
};