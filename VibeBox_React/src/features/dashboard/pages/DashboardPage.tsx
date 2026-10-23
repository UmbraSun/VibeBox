
import {
    useCallback,
    useEffect,
    useState,
    type SyntheticEvent,
} from "react";
import axios from "axios";
import { Link, useNavigate } from "react-router-dom";
import { apiClient } from "@/api/apiClient";
import {
    roomsApi,
    type RoomSummary,
} from "@/api/roomsApi";
import {
    spacesApi,
    type SpaceSummary,
    type ChannelSummary,
} from "@/api/spacesApi";
import { ChannelChat } from "@/features/messages/components/ChannelChat";
import { useAuthStore } from "@/features/auth/store/authStore";

function getErrorMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        return error.response?.data?.message ?? fallback;
    }

    if (error instanceof Error) {
        return error.message;
    }

    return fallback;
}

export function DashboardPage() {
    const navigate = useNavigate();
    const clearTokens = useAuthStore((state) => state.clearTokens);

    // Existing rooms
    const [rooms, setRooms] = useState<RoomSummary[]>([]);
    const [roomName, setRoomName] = useState("");
    const [isLoadingRooms, setIsLoadingRooms] = useState(true);
    const [isCreatingRoom, setIsCreatingRoom] = useState(false);
    const [roomsError, setRoomsError] = useState<string | null>(null);
    const [createRoomError, setCreateRoomError] = useState<string | null>(null);

    // Spaces
    const [spaces, setSpaces] = useState<SpaceSummary[]>([]);
    const [spaceName, setSpaceName] = useState("");
    const [selectedSpaceId, setSelectedSpaceId] = useState("");
    const [isLoadingSpaces, setIsLoadingSpaces] = useState(true);
    const [isCreatingSpace, setIsCreatingSpace] = useState(false);
    const [spacesError, setSpacesError] = useState<string | null>(null);
    const [createSpaceError, setCreateSpaceError] = useState<string | null>(null);

    // Channels
    const [channels, setChannels] = useState<ChannelSummary[]>([]);
    const [channelName, setChannelName] = useState("");
    const [selectedChannelId, setSelectedChannelId] = useState("");
    const [isLoadingChannels, setIsLoadingChannels] = useState(false);
    const [isCreatingChannel, setIsCreatingChannel] = useState(false);
    const [channelsError, setChannelsError] = useState<string | null>(null);
    const [createChannelError, setCreateChannelError] = useState<string | null>(null);

    const loadRooms = useCallback(async () => {
        setRoomsError(null);
        setIsLoadingRooms(true);

        try {
            const result = await roomsApi.getMyRooms();
            setRooms(result);
        } catch (requestError: unknown) {
            setRoomsError(
                getErrorMessage(requestError, "Unable to load your rooms."),
            );
        } finally {
            setIsLoadingRooms(false);
        }
    }, []);

    const loadSpaces = useCallback(async (preferredSpaceId?: string) => {
        setSpacesError(null);
        setIsLoadingSpaces(true);

        try {
            const result = await spacesApi.getMySpaces();
            setSpaces(result);

            setSelectedSpaceId((currentId) => {
                if (preferredSpaceId && result.some(
                    (space) => space.id === preferredSpaceId,
                )) {
                    return preferredSpaceId;
                }

                if (result.some((space) => space.id === currentId)) {
                    return currentId;
                }

                return result[0]?.id ?? "";
            });
        } catch (requestError: unknown) {
            setSpacesError(
                getErrorMessage(requestError, "Unable to load spaces."),
            );
        } finally {
            setIsLoadingSpaces(false);
        }
    }, []);

    useEffect(() => {
        void loadRooms();
        void loadSpaces();
    }, [loadRooms, loadSpaces]);

    useEffect(() => {
        let active = true;

        setChannels([]);
        setSelectedChannelId("");
        setChannelsError(null);

        if (!selectedSpaceId) {
            setIsLoadingChannels(false);
            return;
        }

        async function loadChannels() {
            setIsLoadingChannels(true);

            try {
                const result = await spacesApi.getChannels(selectedSpaceId);

                if (!active) {
                    return;
                }

                setChannels(result);

                const firstTextChannel = result.find(
                    (channel) => channel.type === 0,
                );

                setSelectedChannelId(firstTextChannel?.id ?? "");
            } catch (requestError: unknown) {
                if (active) {
                    setChannelsError(
                        getErrorMessage(
                            requestError,
                            "Unable to load channels.",
                        ),
                    );
                }
            } finally {
                if (active) {
                    setIsLoadingChannels(false);
                }
            }
        }

        void loadChannels();

        return () => {
            active = false;
        };
    }, [selectedSpaceId]);

    async function handleCreateRoom(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const name = roomName.trim();

        if (!name) {
            setCreateRoomError("Enter a room name.");
            return;
        }

        setCreateRoomError(null);
        setIsCreatingRoom(true);

        try {
            await roomsApi.createRoom({ name });
            setRoomName("");
            await loadRooms();
        } catch (requestError: unknown) {
            setCreateRoomError(
                getErrorMessage(requestError, "Unable to create the room."),
            );
        } finally {
            setIsCreatingRoom(false);
        }
    }

    async function handleCreateSpace(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const name = spaceName.trim();

        if (!name) {
            setCreateSpaceError("Enter a space name.");
            return;
        }

        setCreateSpaceError(null);
        setIsCreatingSpace(true);

        try {
            const created = await spacesApi.createSpace({ name });
            setSpaceName("");
            await loadSpaces(created.id);
        } catch (requestError: unknown) {
            setCreateSpaceError(
                getErrorMessage(requestError, "Unable to create the space."),
            );
        } finally {
            setIsCreatingSpace(false);
        }
    }

    async function handleCreateChannel(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const name = channelName.trim();

        if (!selectedSpaceId) {
            setCreateChannelError("Select a space first.");
            return;
        }

        if (!name) {
            setCreateChannelError("Enter a channel name.");
            return;
        }

        setCreateChannelError(null);
        setIsCreatingChannel(true);

        try {
            const created = await spacesApi.createChannel(selectedSpaceId, {
                name,
                type: 0,
            });

            const updatedChannels = await spacesApi.getChannels(selectedSpaceId);

            setChannels(updatedChannels);
            setSelectedChannelId(created.id);
            setChannelName("");
        } catch (requestError: unknown) {
            setCreateChannelError(
                getErrorMessage(requestError, "Unable to create the channel."),
            );
        } finally {
            setIsCreatingChannel(false);
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

    const selectedChannel = channels.find(
        (channel) => channel.id === selectedChannelId,
    );

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
                    Create spaces, chat in text channels, or open a room for a call.
                </p>

                <section className="rooms-section">
                    <h2>Spaces</h2>

                    <form className="room-form" onSubmit={handleCreateSpace}>
                        <label className="room-name-field">
                            Space name
                            <input
                                type="text"
                                value={spaceName}
                                onChange={(event) => setSpaceName(event.target.value)}
                                placeholder="e.g. Friends"
                                maxLength={100}
                                required
                            />
                        </label>

                        <button type="submit" disabled={isCreatingSpace}>
                            {isCreatingSpace ? "Creating..." : "Create space"}
                        </button>
                    </form>

                    {createSpaceError && (
                        <p className="form-error" role="alert">
                            {createSpaceError}
                        </p>
                    )}

                    {spacesError && (
                        <p className="form-error" role="alert">
                            {spacesError}
                        </p>
                    )}

                    {isLoadingSpaces ? (
                        <p className="rooms-message">Loading spaces...</p>
                    ) : spaces.length === 0 ? (
                        <p className="rooms-message">
                            No spaces yet. Create your first space above.
                        </p>
                    ) : (
                        <div className="workspace-space-list">
                            {spaces.map((space) => (
                                <button
                                    type="button"
                                    key={space.id}
                                    className={
                                        selectedSpaceId === space.id
                                            ? "workspace-select-button is-active"
                                            : "workspace-select-button"
                                    }
                                    aria-pressed={selectedSpaceId === space.id}
                                    onClick={() => setSelectedSpaceId(space.id)}
                                >
                                    {space.name}
                                </button>
                            ))}
                        </div>
                    )}
                </section>

                <section className="rooms-section">
                    <h2>Text channels</h2>

                    <form className="room-form" onSubmit={handleCreateChannel}>
                        <label className="room-name-field">
                            Channel name
                            <input
                                type="text"
                                value={channelName}
                                onChange={(event) => setChannelName(event.target.value)}
                                placeholder="e.g. general"
                                maxLength={100}
                                required
                                disabled={!selectedSpaceId}
                            />
                        </label>

                        <button
                            type="submit"
                            disabled={
                                !selectedSpaceId ||
                                isCreatingChannel ||
                                isLoadingSpaces
                            }
                        >
                            {isCreatingChannel ? "Creating..." : "Create channel"}
                        </button>
                    </form>

                    {createChannelError && (
                        <p className="form-error" role="alert">
                            {createChannelError}
                        </p>
                    )}

                    {channelsError && (
                        <p className="form-error" role="alert">
                            {channelsError}
                        </p>
                    )}

                    {!selectedSpaceId ? (
                        <p className="rooms-message">
                            Create or select a space to view its channels.
                        </p>
                    ) : isLoadingChannels ? (
                        <p className="rooms-message">Loading channels...</p>
                    ) : channels.length === 0 ? (
                        <p className="rooms-message">
                            No channels yet. Create your first text channel above.
                        </p>
                    ) : (
                        <div className="workspace-channel-list">
                            {channels.map((channel) => (
                                <button
                                    type="button"
                                    key={channel.id}
                                    className={
                                        channel.type === 0 &&
                                        selectedChannelId === channel.id
                                            ? "workspace-select-button is-active"
                                            : "workspace-select-button"
                                    }
                                    aria-pressed={selectedChannelId === channel.id}
                                    disabled={channel.type !== 0}
                                    onClick={() => setSelectedChannelId(channel.id)}
                                >
                                    <span aria-hidden="true">
                                        {channel.type === 0 ? "#" : "🔊"}
                                    </span>{" "}
                                    {channel.name}
                                    {channel.type === 1 ? " (Voice)" : ""}
                                </button>
                            ))}
                        </div>
                    )}
                </section>

                {selectedChannel?.type === 0 && (
                    <ChannelChat
                        channelId={selectedChannel.id}
                        channelName={selectedChannel.name}
                    />
                )}

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

                        <button type="submit" disabled={isCreatingRoom}>
                            {isCreatingRoom ? "Creating..." : "Create room"}
                        </button>
                    </form>

                    {createRoomError && (
                        <p className="form-error" role="alert">
                            {createRoomError}
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
                            disabled={isLoadingRooms}
                        >
                            Refresh
                        </button>
                    </div>

                    {isLoadingRooms ? (
                        <p className="rooms-message">Loading rooms...</p>
                    ) : roomsError ? (
                        <div>
                            <p className="form-error" role="alert">{roomsError}</p>
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
                                    <Link
                                        to={`/rooms/${room.id}`}
                                        className="room-card-link"
                                    >
                                        <div>
                                            <h3>{room.name}</h3>
                                            <p>
                                                Created{" "}
                                                {new Date(
                                                    room.createdAtUtc,
                                                ).toLocaleDateString()}
                                            </p>
                                        </div>
                                        <span
                                            className="room-card-arrow"
                                            aria-hidden="true"
                                        />
                                    </Link>
                                </article>
                            ))}
                        </div>
                    )}
                </section>
            </section>
        </main>
    );
}
