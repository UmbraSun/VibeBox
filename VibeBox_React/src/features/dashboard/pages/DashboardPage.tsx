import { useCallback, useEffect, useState, type FormEvent } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import { apiClient } from "@/api/apiClient";
import {
  roomsApi,
  type RoomSummary,
} from "@/api/roomsApi";
import { useAuthStore } from "@/features/auth/store/authStore";

function getErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    return error.response?.data?.message ?? fallback;
  }

  return fallback;
}

export function DashboardPage() {
  const navigate = useNavigate();
  const clearTokens = useAuthStore((state) => state.clearTokens);

  const [rooms, setRooms] = useState<RoomSummary[]>([]);
  const [roomName, setRoomName] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [createError, setCreateError] = useState<string | null>(null);

  const loadRooms = useCallback(async () => {
    setError(null);
    setIsLoading(true);

    try {
      const result = await roomsApi.getMyRooms();
      setRooms(result);
    } catch (requestError: unknown) {
      setError(
        getErrorMessage(requestError, "Unable to load your rooms."),
      );
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadRooms();
  }, [loadRooms]);

  async function handleCreateRoom(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const name = roomName.trim();

    if (!name) {
      setCreateError("Enter a room name.");
      return;
    }

    setCreateError(null);
    setIsCreating(true);

    try {
      await roomsApi.createRoom({ name });
      setRoomName("");
      await loadRooms();
    } catch (requestError: unknown) {
      setCreateError(
        getErrorMessage(requestError, "Unable to create the room."),
      );
    } finally {
      setIsCreating(false);
    }
  }

  async function handleLogout() {
    const refreshToken = useAuthStore.getState().refreshToken;

    clearTokens();

    if (refreshToken) {
      try {
        await apiClient.post("/auth/logout", { refreshToken });
      } catch {
        // Local sign-out remains effective if the server request fails.
      }
    }

    navigate("/login", { replace: true });
  }

  return (
    <main className="dashboard-page">
      <header className="dashboard-header">
        <span className="brand">VibeBox</span>

        <button
          type="button"
          className="secondary-button"
          onClick={() => void handleLogout()}
        >
          Sign out
        </button>
      </header>

      <section className="dashboard-content">
        <h1>Your workspace</h1>
        <p className="auth-description">
          Create a room or open one to continue.
        </p>

        <section className="rooms-section">
          <h2>New room</h2>

          <form className="room-form" onSubmit={handleCreateRoom}>
            <label className="room-name-field">
              Room name
              <input
                type="text"
                value={roomName}
                onChange={(event) => setRoomName(event.target.value)}
                placeholder="e.g. Weekend plans"
                maxLength={100}
                required
              />
            </label>

            <button type="submit" disabled={isCreating}>
              {isCreating ? "Creating..." : "Create room"}
            </button>
          </form>

          {createError && (
            <p className="form-error" role="alert">
              {createError}
            </p>
          )}
        </section>

        <section className="rooms-section">
          <div className="rooms-heading">
            <h2>Your rooms</h2>

            <button
              type="button"
              className="secondary-button refresh-button"
              onClick={() => void loadRooms()}
              disabled={isLoading}
            >
              Refresh
            </button>
          </div>

          {isLoading ? (
            <p className="rooms-message">Loading rooms...</p>
          ) : error ? (
            <div>
              <p className="form-error" role="alert">{error}</p>
              <button
                type="button"
                className="secondary-button"
                onClick={() => void loadRooms()}
              >
                Try again
              </button>
            </div>
          ) : rooms.length === 0 ? (
            <p className="rooms-message">
              You don't have any rooms yet. Create your first one above.
            </p>
          ) : (
            <div className="rooms-list">
              {rooms.map((room) => (
                <article className="room-card" key={room.id}>
                  <div>
                    <h3>{room.name}</h3>
                    <p>
                      Created{" "}
                      {new Date(room.createdAtUtc).toLocaleDateString()}
                    </p>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </section>
    </main>
  );
}