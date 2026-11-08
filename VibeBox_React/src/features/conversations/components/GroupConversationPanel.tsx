
import { useCallback, useEffect, useState } from "react";
import {
    conversationsApi,
    type ConversationDetailsResponse,
} from "@/api/conversationsApi";
import { useUserSearch } from "@/features/conversations/hooks/useUserSearch";
import { getErrorMessage } from "@/features/messages/utils/errorUtils";

interface GroupConversationPanelProps {
    conversationId: string;
    currentUserId: string | null;
    onLeave: () => void;
}

export function GroupConversationPanel({
    conversationId,
    currentUserId,
    onLeave,
}: GroupConversationPanelProps) {
    const [details, setDetails] =
        useState<ConversationDetailsResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isUpdating, setIsUpdating] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [name, setName] = useState("");
    const [search, setSearch] = useState("");
    const [selectedUserIds, setSelectedUserIds] = useState<string[]>([]);

    const {
        users,
        isSearching,
        error: searchError,
        searchUsers,
        clearSearch,
    } = useUserSearch();

    const loadDetails = useCallback(async () => {
        try {
            const result = await conversationsApi.getDetails(conversationId);

            setDetails(result);
            setName(result.name ?? "");
            setError(null);
        } catch (requestError: unknown) {
            setError(
                getErrorMessage(
                    requestError,
                    "Unable to load group details.",
                ),
            );
        } finally {
            setIsLoading(false);
        }
    }, [conversationId]);

    useEffect(() => {
        void loadDetails();
    }, [loadDetails]);

    async function executeUpdate(action: () => Promise<void>) {
        if (isUpdating) return;

        setIsUpdating(true);
        setError(null);

        try {
            await action();
            await loadDetails();
        } catch (requestError: unknown) {
            setError(
                getErrorMessage(
                    requestError,
                    "Unable to update group.",
                ),
            );
        } finally {
            setIsUpdating(false);
        }
    }

    async function handleSearchChange(value: string) {
        setSearch(value);
        await searchUsers(value);
    }

    function toggleUser(userId: string) {
        setSelectedUserIds((current) =>
            current.includes(userId)
                ? current.filter((id) => id !== userId)
                : [...current, userId],
        );
    }

    async function handleAddMembers() {
        if (selectedUserIds.length === 0) return;

        await executeUpdate(async () => {
            await conversationsApi.addMembers(
                conversationId,
                selectedUserIds,
            );

            setSelectedUserIds([]);
            setSearch("");
            clearSearch();
        });
    }

    async function handleRemoveMember(userId: string) {
        const confirmed = window.confirm(
            "Are you sure you want to remove this member from the group?",
        );

        if (!confirmed) return;

        await executeUpdate(() =>
            conversationsApi.removeMember(conversationId, userId),
        );
    }

    async function handleRename() {
        const trimmedName = name.trim();

        if (!trimmedName || trimmedName.length > 200) return;

        await executeUpdate(() =>
            conversationsApi.renameConversation(
                conversationId,
                trimmedName,
            ),
        );
    }

    async function handleLeave() {
        const confirmed = window.confirm(
            "Are you sure you want to leave this group?",
        );

        if (!confirmed) return;

        setIsUpdating(true);
        setError(null);

        try {
            await conversationsApi.leaveConversation(conversationId);
            onLeave();
        } catch (requestError: unknown) {
            setError(
                getErrorMessage(
                    requestError,
                    "Unable to leave group.",
                ),
            );
        } finally {
            setIsUpdating(false);
        }
    }

    if (isLoading) {
        return <div className="group-conversation-panel">
            <p className="direct-messages-hint">Loading group details...</p>
        </div>;
    }

    if (!details) {
        return (
            <div className="group-conversation-panel">
                {error && <p className="form-error">{error}</p>}
            </div>
        );
    }

    const availableUsers = users.filter(
        (user) =>
            !details.members.some((member) => member.userId === user.id),
    );

    return (
        <div className="group-conversation-panel">
            <div className="group-conversation-panel-heading">
                <h3>Group settings</h3>

                <button
                    type="button"
                    onClick={() => void loadDetails()}
                    disabled={isUpdating}
                >
                    Refresh
                </button>
            </div>

            {error && (
                <p className="form-error" role="alert">
                    {error}
                </p>
            )}

            <div className="group-conversation-rename">
                <label htmlFor="group-name">Group name</label>

                <div>
                    <input
                        id="group-name"
                        type="text"
                        value={name}
                        maxLength={200}
                        onChange={(event) => setName(event.target.value)}
                    />

                    <button
                        type="button"
                        disabled={
                            isUpdating ||
                            !name.trim() ||
                            name.trim() === (details.name ?? "")
                        }
                        onClick={() => void handleRename()}
                    >
                        Rename
                    </button>
                </div>
            </div>

            <div className="group-conversation-members">
                <h4>Members ({details.members.length})</h4>

                {details.members.map((member) => (
                    <div
                        className="group-conversation-member"
                        key={member.userId}
                    >
                        <div>
                            <span>{member.email}</span>

                            {member.userId === currentUserId && (
                                <small>You</small>
                            )}
                        </div>

                        {member.userId !== currentUserId && (
                            <button
                                type="button"
                                disabled={isUpdating}
                                onClick={() =>
                                    void handleRemoveMember(member.userId)
                                }
                            >
                                Remove
                            </button>
                        )}
                    </div>
                ))}
            </div>

            <div className="group-conversation-add-members">
                <h4>Add members</h4>

                <input
                    type="text"
                    value={search}
                    onChange={(event) =>
                        void handleSearchChange(event.target.value)
                    }
                    placeholder="Search users by email..."
                />

                {isSearching && (
                    <p className="direct-messages-hint">Searching...</p>
                )}

                {searchError && (
                    <p className="form-error">{searchError}</p>
                )}

                {availableUsers.length > 0 && (
                    <div className="group-conversation-user-results">
                        {availableUsers.map((user) => (
                            <button
                                key={user.id}
                                type="button"
                                className={
                                    selectedUserIds.includes(user.id)
                                        ? "selected"
                                        : ""
                                }
                                onClick={() => toggleUser(user.id)}
                            >
                                {selectedUserIds.includes(user.id) ? "✓ " : ""}
                                {user.email}
                            </button>
                        ))}
                    </div>
                )}

                {selectedUserIds.length > 0 && (
                    <button
                        type="button"
                        disabled={isUpdating}
                        onClick={() => void handleAddMembers()}
                    >
                        Add selected ({selectedUserIds.length})
                    </button>
                )}
            </div>

            <div className="group-conversation-actions">
                <button
                    type="button"
                    className="danger-button"
                    disabled={isUpdating}
                    onClick={() => void handleLeave()}
                >
                    Leave group
                </button>
            </div>
        </div>
    );
}
