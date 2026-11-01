import type { SyntheticEvent } from "react";
import type { SpaceMember } from "@/api/spacesApi";

interface SpaceMembersSectionProps {
    selectedSpaceId: string;
    ownerId?: string;
    members: SpaceMember[];
    memberEmail: string;
    isLoading: boolean;
    isAdding: boolean;
    error: string | null;
    success: string | null;
    onMemberEmailChange: (value: string) => void;
    onAdd: (event: SyntheticEvent<HTMLFormElement>) => void;
    onRemove: (userId: string) => void;
}

export function SpaceMembersSection({
    selectedSpaceId,
    ownerId,
    members,
    memberEmail,
    isLoading,
    isAdding,
    error,
    success,
    onMemberEmailChange,
    onAdd,
    onRemove,
}: SpaceMembersSectionProps) {
    return (
        <section className="rooms-section">
            <h2>Space members</h2>

            {!selectedSpaceId ? (
                <p className="rooms-message">
                    Select a space to manage its members.
                </p>
            ) : (
                <>
                    <form className="room-form" onSubmit={onAdd}>
                        <label className="room-name-field">
                            Member email
                            <input
                                type="email"
                                value={memberEmail}
                                onChange={(event) =>
                                    onMemberEmailChange(event.target.value)
                                }
                                placeholder="friend@example.com"
                                required
                            />
                        </label>

                        <button type="submit" disabled={isAdding || isLoading}>
                            {isAdding ? "Adding..." : "Add member"}
                        </button>
                    </form>

                    {error && (
                        <p className="form-error" role="alert">{error}</p>
                    )}

                    {success && (
                        <p className="rooms-message" role="status">{success}</p>
                    )}

                    {isLoading ? (
                        <p className="rooms-message">Loading members...</p>
                    ) : members.length === 0 ? (
                        <p className="rooms-message">No members found.</p>
                    ) : (
                        <div className="workspace-channel-list">
                            {members.map((member) => {
                                const isOwner = ownerId === member.userId;

                                return (
                                    <div
                                        className="workspace-member-row"
                                        key={member.userId}
                                    >
                                        <div>
                                            <strong>{member.email}</strong>
                                            <p className="rooms-message">
                                                {member.role}
                                                {isOwner ? " · Space owner" : ""}
                                            </p>
                                        </div>

                                        {!isOwner && (
                                            <button
                                                type="button"
                                                className="secondary-button"
                                                onClick={() => onRemove(member.userId)}
                                            >
                                                Remove
                                            </button>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </>
            )}
        </section>
    );
}