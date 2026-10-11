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
};