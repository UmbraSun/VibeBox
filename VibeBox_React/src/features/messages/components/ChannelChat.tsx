
import { useState, type SyntheticEvent } from "react";
import {
    useChannelMessages,
} from "@/features/messages/hooks/useChannelMessages";

interface ChannelChatProps {
    channelId: string;
    channelName: string;
}

export function ChannelChat({
    channelId,
    channelName,
}: ChannelChatProps) {
    const [content, setContent] = useState("");

    const {
        messages,
        connectionStatus,
        isLoading,
        isSending,
        error,
        sendMessage,
    } = useChannelMessages(channelId);

    async function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const message = content.trim();

        if (!message || isSending) {
            return;
        }

        try {
            await sendMessage(message);
            setContent("");
        } catch {
            // The hook exposes the request error to the UI.
        }
    }

    return (
        <section className="rooms-section channel-chat">
            <div className="rooms-heading">
                <div>
                    <p className="room-eyebrow">TEXT CHANNEL</p>
                    <h2>{channelName}</h2>
                </div>

                <span className="participant-count">
                    {connectionStatus === "connected"
                        ? "Connected"
                        : connectionStatus === "connecting"
                          ? "Connecting..."
                          : connectionStatus === "error"
                            ? "Connection error"
                            : "Disconnected"}
                </span>
            </div>

            {error && (
                <p className="form-error" role="alert">
                    {error}
                </p>
            )}

            <div className="channel-chat-messages" aria-live="polite">
                {isLoading ? (
                    <p className="rooms-message">Loading messages...</p>
                ) : messages.length === 0 ? (
                    <p className="rooms-message">
                        No messages yet. Start the conversation.
                    </p>
                ) : (
                    messages.map((message) => (
                        <article className="channel-chat-message" key={message.id}>
                            <div className="channel-chat-message-header">
                                <strong>{message.senderEmail}</strong>
                                <time dateTime={message.createdAtUtc}>
                                    {new Date(
                                        message.createdAtUtc,
                                    ).toLocaleString()}
                                </time>
                            </div>

                            <p>{message.content}</p>
                        </article>
                    ))
                )}
            </div>

            <form className="room-form channel-chat-form" onSubmit={handleSubmit}>
                <label className="room-name-field">
                    Message
                    <textarea
                        value={content}
                        onChange={(event) => setContent(event.target.value)}
                        placeholder="Write a message..."
                        maxLength={4000}
                        rows={3}
                        required
                    />
                </label>

                <button
                    type="submit"
                    disabled={!content.trim() || isSending}
                >
                    {isSending ? "Sending..." : "Send"}
                </button>
            </form>
        </section>
    );
}
