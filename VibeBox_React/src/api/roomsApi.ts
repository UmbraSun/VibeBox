import { apiClient } from "@/api/apiClient";

export interface RoomSummary {
  id: string;
  name: string;
  createdAtUtc: string;
}

export interface CreateRoomRequest {
  name: string;
}

export interface CreateRoomResponse {
  id: string;
  ownerId: string;
  name: string;
  createdAtUtc: string;
}

export interface RoomDetails {
  id: string;
  name: string;
  createdAtUtc: string;
}

export interface RoomParticipant {
  userId: string;
  email: string;
  joinedAtUtc: string;
}

export interface AddRoomParticipantRequest {
  userId: string;
}

export interface LiveKitTokenResponse {
  serverUrl: string;
  participantToken: string;
  roomName: string;
}

export const roomsApi = {
  async getMyRooms(): Promise<RoomSummary[]> {
    const response = await apiClient.get<RoomSummary[]>("/rooms");
    return response.data;
  },

  async createRoom(
    request: CreateRoomRequest,
  ): Promise<CreateRoomResponse> {
    const response = await apiClient.post<CreateRoomResponse>(
      "/rooms",
      request,
    );

    return response.data;
  },

  async getRoomById(id: string): Promise<RoomDetails> {
    const response = await apiClient.get<RoomDetails>(
      `/rooms/${id}`,
    );

    return response.data;
  },

  async addParticipant(
    id: string,
    request: AddRoomParticipantRequest,
  ): Promise<void> {
    await apiClient.post(`/rooms/${id}/participants`, request);
  },

  async removeParticipant(
    id: string,
    userId: string,
  ): Promise<void> {
    await apiClient.delete(`/rooms/${id}/participants/${userId}`);
  },

  async getRoomParticipants(id: string): Promise<RoomParticipant[]> {
    const response = await apiClient.get<RoomParticipant[]>(
      `/rooms/${id}/participants`,
    );

    return response.data;
  },

  async getLiveKitToken(id: string): Promise<LiveKitTokenResponse> {
    const response = await apiClient.post<LiveKitTokenResponse>(
      `/rooms/${id}/livekit-token`,
    );

    return response.data;
  },
};