import {
    messagesApi,
    type MessageResponse,
} from "@/api/messagesApi";
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

    async sendMessage(channelId: string, content: string): Promise<void> {
        try {
            await messagesApi.sendMessage(channelId, { content });
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
};