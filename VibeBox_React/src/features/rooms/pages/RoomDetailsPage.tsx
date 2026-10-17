import { useCallback, useEffect, useState, type SyntheticEvent } from "react";
import axios from "axios";
import { Link, useParams } from "react-router-dom";
import {
    roomsApi,
    type RoomDetails,
    type RoomParticipant,
} from "@/api/roomsApi";
import { LiveKitCall } from "@/features/rooms/components/LiveKitCall";

function getErrorMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        return error.response?.data?.message ?? fallback;
    }

    return fallback;
}

export function RoomDetailsPage() {
    const { id } = useParams<{ id: string }>();

    const [room, setRoom] = useState<RoomDetails | null>(null);
    const [participants, setParticipants] = useState<RoomParticipant[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [participantUserId, setParticipantUserId] = useState("");
    const [isAddingParticipant, setIsAddingParticipant] = useState(false);
    const [addParticipantError, setAddParticipantError] = useState<string | null>(null);
    const [addParticipantMessage, setAddParticipantMessage] = useState<string | null>(null);
    const [removingParticipantId, setRemovingParticipantId] = useState<string | null>(null);
    const [removeParticipantError, setRemoveParticipantError] = useState<string | null>(null);
    const [removeParticipantMessage, setRemoveParticipantMessage] = useState<string | null>(null);

    const loadRoom = useCallback(async () => {
        if (!id) {
            setError("Room ID is missing.");
            setIsLoading(false);
            return;
        }

        setIsLoading(true);
        setError(null);

        try {
            const [roomResult, participantsResult] = await Promise.all([
                roomsApi.getRoomById(id),
                roomsApi.getRoomParticipants(id),
            ]);

            setRoom(roomResult);
            setParticipants(participantsResult);
        } catch (requestError: unknown) {
            setError(
                getErrorMessage(requestError, "Unable to load room details."),
            );
        } finally {
            setIsLoading(false);
        }
    }, [id]);

    async function handleAddParticipant(
        event: SyntheticEvent<HTMLFormElement>,
    ) {
        event.preventDefault();

        if (!id) return;

        setAddParticipantError(null);
        setAddParticipantMessage(null);
        setIsAddingParticipant(true);

        try {
            await roomsApi.addParticipant(id, {
                userId: participantUserId.trim(),
            });

            setParticipantUserId("");
            setAddParticipantMessage("Participant added.");
            await loadRoom();
        } catch (requestError: unknown) {
            setAddParticipantError(
                getErrorMessage(requestError, "Unable to add participant."),
            );
        } finally {
            setIsAddingParticipant(false);
        }
    }

    async function handleRemoveParticipant(userId: string) {
        if (!id || removingParticipantId) return;

        const confirmed = window.confirm(
            "Are you sure you want to remove this participant?",
        );

        if (!confirmed) return;

        setRemovingParticipantId(userId);
        setRemoveParticipantError(null);
        setRemoveParticipantMessage(null);
        setAddParticipantError(null);
        setAddParticipantMessage(null);

        try {
            await roomsApi.removeParticipant(id, userId);
            setRemoveParticipantMessage("Participant removed.");
            await loadRoom();
        } catch (requestError: unknown) {
            setRemoveParticipantError(
                getErrorMessage(requestError, "Unable to remove participant."),
            );
        } finally {
            setRemovingParticipantId(null);
        }
    }

    useEffect(() => {
        void loadRoom();
    }, [loadRoom]);

    return (
        <main className="dashboard-page">
            <header className="dashboard-header">
                <Link to="/dashboard" className="brand">
                    VibeBox
                </Link>

                <Link to="/dashboard" className="back-link">
                    Back to rooms
                </Link>
            </header>

            <section className="dashboard-content">
                {isLoading ? (
                    <p className="rooms-message">Loading room...</p>
                ) : error ? (
                    <section className="rooms-section">
                        <p className="form-error" role="alert">{error}</p>

                        <button
                            type="button"
                            className="secondary-button"
                            onClick={() => void loadRoom()}
                        >
                            Try again
                        </button>
                    </section>
                ) : room ? (
                    <>
                        <section className="rooms-section">
                            <p className="room-eyebrow">ROOM</p>
                            <h1>{room.name}</h1>
                            <p className="auth-description room-created-date">
                                Created {new Date(room.createdAtUtc).toLocaleString()}
                            </p>
                        </section>

                        <section className="rooms-section">
                            <div className="rooms-heading">
                                <h2>Video call</h2>
                            </div>

                            <LiveKitCall roomId={room.id} />
                        </section>

                        <section className="rooms-section">
                            <div className="rooms-heading">
                                <h2>Participants</h2>
                                <span className="participant-count">
                                    {participants.length}
                                </span>
                            </div>

                            <form className="room-form" onSubmit={handleAddParticipant}>
                                <label className="room-name-field">
                                    Participant user ID
                                    <input
                                        type="text"
                                        value={participantUserId}
                                        onChange={(event) => setParticipantUserId(event.target.value)}
                                        placeholder="Enter user ID"
                                        required
                                    />
                                </label>

                                <button type="submit" disabled={isAddingParticipant}>
                                    {isAddingParticipant ? "Adding..." : "Add participant"}
                                </button>
                            </form>

                            {addParticipantError && (
                                <p className="form-error" role="alert">
                                    {addParticipantError}
                                </p>
                            )}

                            {removeParticipantError && (
                                <p className="form-error" role="alert">
                                    {removeParticipantError}
                                </p>
                            )}

                            {addParticipantMessage && (
                                <p className="success-message" role="status">
                                    {addParticipantMessage}
                                </p>
                            )}

                            {removeParticipantMessage && (
                                <p className="success-message" role="status">
                                    {removeParticipantMessage}
                                </p>
                            )}

                            {participants.length === 0 ? (
                                <p className="rooms-message">
                                    No participants found.
                                </p>
                            ) : (
                                <div className="rooms-list">
                                    {participants.map((participant) => (
                                        <article
                                            className="room-card participant-card"
                                            key={participant.userId}
                                        >
                                            <div className="participant-avatar" aria-hidden="true">
                                                {participant.email.charAt(0).toUpperCase()}
                                            </div>

                                            <div className="participant-info">
                                                <h3>{participant.email}</h3>
                                                <p>
                                                    Joined{" "}
                                                    {new Date(
                                                        participant.joinedAtUtc,
                                                    ).toLocaleDateString()}
                                                </p>
                                            </div>

                                            <button
                                                type="button"
                                                className="remove-participant-button"
                                                disabled={removingParticipantId !== null}
                                                onClick={() => void handleRemoveParticipant(participant.userId)}
                                            >
                                                {removingParticipantId === participant.userId
                                                    ? "Removing..."
                                                    : "Remove"}
                                            </button>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </section>
                    </>
                ) : null}
            </section>
        </main>
    );
}