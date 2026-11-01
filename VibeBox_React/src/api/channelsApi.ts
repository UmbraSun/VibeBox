import { apiClient } from "@/api/apiClient";

export interface ChannelLiveKitTokenResponse {
    serverUrl: string;
    participantToken: string;
    roomName: string;
}

export const channelsApi = {
    async getLiveKitToken(
        channelId: string,
    ): Promise<ChannelLiveKitTokenResponse> {
        const response = await apiClient.post<ChannelLiveKitTokenResponse>(
            `/channels/${channelId}/livekit-token`,
        );

        return response.data;
    },
};