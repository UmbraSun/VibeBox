import {
    useCallback,
    useEffect,
    useMemo,
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
    } = useConversations();

    const [hubError, setHubError] = useState<string | null>(null);

    const conversationIdsKey = conversations
        .map((conversation) => conversation.id)
        .sort()
        .join("|");

    const conversationIds = useMemo(
        () => conversationIdsKey ? conversationIdsKey.split("|") : [],
        [conversationIdsKey],
    );

    const refreshTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

    const handleConversationMessageChanged = useCallback(
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
    const handleHubMessageCreated = useCallback(() => { }, []);
    const handleHubMessageDeleted = useCallback((_messageId: string) => { }, []);

    useMessageHub({
        conversationIds,
        onMessageCreated: handleHubMessageCreated,
        onMessageDeleted: handleHubMessageDeleted,
        onConnected: handleHubConnected,
        onStatusChange: handleHubStatusChange,
        onError: handleHubError,
        onConversationMessageChanged: handleConversationMessageChanged,
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

    async function handleSearchChange(value: string) {
        setSearch(value);
        await searchUsers(value);
    }

    async function handleSelectUser(userId: string, email: string) {
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
                    <div className="direct-messages-search">
                        <input
                            type="text"
                            value={search}
                            onChange={(event) =>
                                void handleSearchChange(event.target.value)
                            }
                            placeholder="Search users by email..."
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
                                        {user.email}
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
                        <h3>Conversations</h3>

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

                        {!isLoading && conversations.length === 0 && (
                            <p className="direct-messages-hint">
                                No conversations yet.
                            </p>
                        )}

                        {conversations.map((conversation) => {
                            const name =
                                conversation.otherUserEmail ??
                                conversation.name ??
                                "Conversation";

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
                </aside>

                <div className="direct-messages-chat">
                    {selectedConversationId ? (
                        <ConversationChat
                            conversationId={selectedConversationId}
                            conversationName={selectedConversationName}
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