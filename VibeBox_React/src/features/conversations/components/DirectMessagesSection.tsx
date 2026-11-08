import {
    useCallback,
    useEffect,
    useRef,
    useState,
} from "react";
import { ConversationChat } from "@/features/messages/components/ConversationChat";
import { useConversations } from "@/features/conversations/hooks/useConversations";
import { useUserSearch } from "@/features/conversations/hooks/useUserSearch";
import { useMessageHub } from "@/features/messages/hooks/useMessageHub";

export function DirectMessagesSection() {
    const {
        conversations,
        isLoading,
        isCreating,
        error,
        loadConversations,
        createDirect,
        createGroup,
    } = useConversations();

    const [hubError, setHubError] = useState<string | null>(null);

    const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

    const handleConversationUpdated = useCallback(
        (_conversationId: string) => {
            if (refreshTimer.current) {
                clearTimeout(refreshTimer.current);
            }

            refreshTimer.current = setTimeout(() => {
                void loadConversations();
                refreshTimer.current = null;
            }, 300);
        },
        [loadConversations],
    );

    useEffect(() => {
        return () => {
            if (refreshTimer.current) {
                clearTimeout(refreshTimer.current);
            }
        };
    }, []);

    const handleHubError = useCallback((message: string) => {
        setHubError(message);
    }, []);

    const handleHubStatusChange = useCallback(() => { }, []);
    const handleHubConnected = useCallback(() => { }, []);

    useMessageHub({
        conversationIds: conversations.map((conversation) => conversation.id),
        onConnected: handleHubConnected,
        onStatusChange: handleHubStatusChange,
        onError: handleHubError,
        onConversationUpdated: handleConversationUpdated,
        onConversationMessageChanged: handleConversationUpdated,
    });

    const {
        users,
        isSearching,
        error: searchError,
        searchUsers,
        clearSearch,
    } = useUserSearch();

    const [search, setSearch] = useState("");
    const [selectedConversationId, setSelectedConversationId] =
        useState<string | null>(null);
    const [selectedConversationName, setSelectedConversationName] =
        useState("");
    const [isGroupCreationOpen, setIsGroupCreationOpen] = useState(false);
    const [groupName, setGroupName] = useState("");
    const [selectedGroupUsers, setSelectedGroupUsers] = useState<
        { id: string; email: string }[]
    >([]);

    const previousConversationIdsRef = useRef<Set<string>>(new Set());

    useEffect(() => {
        const currentIds = new Set(
            conversations.map((conversation) => conversation.id.toLowerCase()),
        );

        const selectedId = selectedConversationId?.toLowerCase();

        if (
            selectedId &&
            previousConversationIdsRef.current.has(selectedId) &&
            !currentIds.has(selectedId)
        ) {
            setSelectedConversationId(null);
            setSelectedConversationName("");
        }

        previousConversationIdsRef.current = currentIds;
    });

    useEffect(() => {
        if (
            !selectedConversationId &&
            conversations.length > 0
        ) {
            const first = conversations[0];

            setSelectedConversationId(first.id);
            setSelectedConversationName(
                first.otherUserEmail ?? first.name ?? "Conversation",
            );
        }
    }, [conversations, selectedConversationId]);

    useEffect(() => {
        if (!selectedConversationId) return;

        const selectedConversation = conversations.find(
            (conversation) => conversation.id === selectedConversationId,
        );

        if (!selectedConversation) return;

        const name =
            selectedConversation.type.toLowerCase() === "group"
                ? selectedConversation.name ?? "Group"
                : selectedConversation.otherUserEmail ?? "Conversation";

        setSelectedConversationName((current) =>
            current === name ? current : name,
        );
    }, [conversations, selectedConversationId]);

    async function handleSearchChange(value: string) {
        setSearch(value);
        await searchUsers(value);
    }

    async function handleSelectUser(userId: string, email: string) {
        if (isGroupCreationOpen) {
            setSelectedGroupUsers((current) => {
                if (current.some((user) => user.id === userId)) {
                    return current.filter((user) => user.id !== userId);
                }

                return [...current, { id: userId, email }];
            });

            return;
        }

        try {
            const conversationId = await createDirect(userId);

            setSelectedConversationId(conversationId);
            setSelectedConversationName(email);
            setSearch("");
            clearSearch();
        } catch {
            // The conversations hook exposes the creation error.
        }
    }

    async function handleCreateGroup() {
        if (
            !groupName.trim() ||
            selectedGroupUsers.length < 2 ||
            isCreating
        ) {
            return;
        }

        try {
            const conversationId = await createGroup(
                groupName,
                selectedGroupUsers.map((user) => user.id),
            );

            setSelectedConversationId(conversationId);
            setSelectedConversationName(groupName.trim());

            setGroupName("");
            setSelectedGroupUsers([]);
            setSearch("");
            setIsGroupCreationOpen(false);
            clearSearch();
        } catch {
            // The conversations hook exposes the creation error.
        }
    }

    function handleSelectConversation(
        conversationId: string,
        name: string,
    ) {
        setSelectedConversationId(conversationId);
        setSelectedConversationName(name);
    }

    return (
        <section className="rooms-section direct-messages-section">
            <h2>Direct Messages</h2>

            <div className="direct-messages-layout">
                <aside className="direct-messages-sidebar">
                    <button
                        type="button"
                        onClick={() => {
                            setIsGroupCreationOpen((current) => !current);
                            setSelectedGroupUsers([]);
                            setSearch("");
                            clearSearch();
                        }}
                    >
                        {isGroupCreationOpen ? "Cancel group creation" : "+ Create group"}
                    </button>

                    {isGroupCreationOpen && (
                        <div className="group-creation">
                            <input
                                type="text"
                                value={groupName}
                                onChange={(event) => setGroupName(event.target.value)}
                                placeholder="Group name"
                                maxLength={200}
                            />

                            <p className="direct-messages-hint">
                                Select at least two participants.
                            </p>

                            {selectedGroupUsers.length > 0 && (
                                <div className="group-selected-users">
                                    {selectedGroupUsers.map((user) => (
                                        <button
                                            key={user.id}
                                            type="button"
                                            onClick={() =>
                                                setSelectedGroupUsers((current) =>
                                                    current.filter((item) => item.id !== user.id),
                                                )
                                            }
                                        >
                                            {user.email} ×
                                        </button>
                                    ))}
                                </div>
                            )}

                            <button
                                type="button"
                                disabled={
                                    isCreating ||
                                    !groupName.trim() ||
                                    selectedGroupUsers.length < 2
                                }
                                onClick={() => void handleCreateGroup()}
                            >
                                {isCreating ? "Creating..." : "Create group"}
                            </button>
                        </div>
                    )}

                    <div className="direct-messages-search">
                        <input
                            type="text"
                            value={search}
                            onChange={(event) =>
                                void handleSearchChange(event.target.value)
                            }
                            placeholder={
                                isGroupCreationOpen
                                    ? "Search users to add to group..."
                                    : "Search users by email..."
                            }
                        />

                        {isSearching && (
                            <span className="direct-messages-hint">
                                Searching...
                            </span>
                        )}

                        {searchError && (
                            <p className="direct-messages-error">
                                {searchError}
                            </p>
                        )}

                        {users.length > 0 && (
                            <div className="direct-messages-search-results">
                                {users.map((user) => (
                                    <button
                                        key={user.id}
                                        type="button"
                                        disabled={isCreating}
                                        onClick={() =>
                                            void handleSelectUser(
                                                user.id,
                                                user.email,
                                            )
                                        }
                                    >
                                        {isGroupCreationOpen &&
                                            selectedGroupUsers.some((item) => item.id === user.id)
                                            ? `✓ ${user.email}`
                                            : user.email}
                                    </button>
                                ))}
                            </div>
                        )}

                        {search.trim().length >= 3 &&
                            !isSearching &&
                            users.length === 0 &&
                            !searchError && (
                                <span className="direct-messages-hint">
                                    No users found.
                                </span>
                            )}
                    </div>

                    <div className="direct-messages-list">
                        {isLoading && (
                            <p className="direct-messages-hint">
                                Loading conversations...
                            </p>
                        )}

                        {error && (
                            <p className="direct-messages-error">{error}</p>
                        )}

                        {hubError && (
                            <p className="direct-messages-error">{hubError}</p>
                        )}

                        {(["Direct", "Group"] as const).map((type) => {
                            const items = conversations.filter(
                                (conversation) =>
                                    conversation.type.toLowerCase() === type.toLowerCase(),
                            );

                            if (items.length === 0) {
                                return null;
                            }

                            return (
                                <div key={type}>
                                    <h3>
                                        {type === "Direct" ? "Direct Messages" : "Groups"}
                                    </h3>

                                    {items.map((conversation) => {
                                        const name =
                                            conversation.type.toLowerCase() === "group"
                                                ? conversation.name ?? "Group"
                                                : conversation.otherUserEmail ?? "Conversation";

                                        return (
                                            <button
                                                key={conversation.id}
                                                type="button"
                                                className={
                                                    selectedConversationId === conversation.id
                                                        ? "direct-message-item active"
                                                        : "direct-message-item"
                                                }
                                                onClick={() =>
                                                    handleSelectConversation(
                                                        conversation.id,
                                                        name,
                                                    )
                                                }
                                            >
                                                <div className="direct-message-item-info">
                                                    <span className="direct-message-item-name">
                                                        {name}
                                                    </span>

                                                    <span className="direct-message-item-preview">
                                                        {conversation.lastMessageContent?.trim() ||
                                                            (conversation.lastMessageHasAttachments
                                                                ? "Attachment"
                                                                : "No messages yet")}
                                                    </span>
                                                </div>

                                                {(conversation.lastMessageCreatedAtUtc ??
                                                    conversation.lastMessageAtUtc) && (
                                                        <small className="direct-message-item-date">
                                                            {new Date(
                                                                conversation.lastMessageCreatedAtUtc ??
                                                                conversation.lastMessageAtUtc!,
                                                            ).toLocaleDateString()}
                                                        </small>
                                                    )}
                                            </button>
                                        );
                                    })}
                                </div>
                            );
                        })}

                        {!isLoading && conversations.length === 0 && (
                            <p className="direct-messages-hint">
                                No conversations yet.
                            </p>
                        )}
                    </div>
                </aside>

                <div className="direct-messages-chat">
                    {selectedConversationId ? (
                        <ConversationChat
                            conversationId={selectedConversationId}
                            conversationName={selectedConversationName}
                            conversationType={
                                conversations.find(
                                    (conversation) => conversation.id === selectedConversationId,
                                )?.type ?? "Direct"
                            }
                            onLeaveGroup={() => {
                                setSelectedConversationId(null);
                                setSelectedConversationName("");
                                void loadConversations();
                            }}
                        />
                    ) : (
                        <div className="direct-messages-empty">
                            Select a conversation or search for a user.
                        </div>
                    )}
                </div>
            </div>
        </section>
    );
}