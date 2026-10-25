
import { apiClient } from "@/api/apiClient";

export interface SpaceSummary {
    id: string;
    name: string;
    ownerId: string;
    createdAtUtc: string;
}

export type ChannelType = 0 | 1;

export interface ChannelSummary {
    id: string;
    spaceId: string;
    name: string;
    type: ChannelType;
    createdAtUtc: string;
}

export interface CreateSpaceRequest {
    name: string;
}

export interface CreateChannelRequest {
    name: string;
    type: ChannelType;
}

export interface SpaceMember {
    userId: string;
    email: string;
    role: string;
    joinedAtUtc: string;
}

export interface AddSpaceMemberRequest {
    email: string;
}

interface CreateResourceResponse {
    id: string;
}

export const spacesApi = {
    async getMySpaces(): Promise<SpaceSummary[]> {
        const response = await apiClient.get<SpaceSummary[]>("/spaces");
        return response.data;
    },

    async createSpace(
        request: CreateSpaceRequest,
    ): Promise<CreateResourceResponse> {
        const response = await apiClient.post<CreateResourceResponse>(
            "/spaces",
            request,
        );
        return response.data;
    },

    async getChannels(spaceId: string): Promise<ChannelSummary[]> {
        const response = await apiClient.get<ChannelSummary[]>(
            `/spaces/${spaceId}/channels`,
        );
        return response.data;
    },

    async createChannel(
        spaceId: string,
        request: CreateChannelRequest,
    ): Promise<CreateResourceResponse> {
        const response = await apiClient.post<CreateResourceResponse>(
            `/spaces/${spaceId}/channels`,
            request,
        );
        return response.data;
    },

    async getMembers(spaceId: string): Promise<SpaceMember[]> {
        const response = await apiClient.get<SpaceMember[]>(
            `/spaces/${spaceId}/members`,
        );

        return response.data;
    },

    async addMember(
        spaceId: string,
        request: AddSpaceMemberRequest,
    ): Promise<void> {
        await apiClient.post(`/spaces/${spaceId}/members`, request);
    },

    async removeMember(
        spaceId: string,
        userId: string,
    ): Promise<void> {
        await apiClient.delete(`/spaces/${spaceId}/members/${userId}`);
    },
};
