import type { SyntheticEvent } from "react";
import type {
    ChannelSummary,
    ChannelType,
} from "@/api/spacesApi";

interface ChannelsSectionProps {
    channels: ChannelSummary[];
    selectedSpaceId: string;
    selectedChannelId: string;
    channelName: string;
    channelType: ChannelType;
    isLoading: boolean;
    isCreating: boolean;
    error: string | null;
    createError: string | null;
    onChannelNameChange: (value: string) => void;
    onChannelTypeChange: (value: ChannelType) => void;
    onSelect: (id: string) => void;
    onCreate: (event: SyntheticEvent<HTMLFormElement>) => void;
}

export function ChannelsSection({
    channels,
    selectedSpaceId,
    selectedChannelId,
    channelName,
    channelType,
    isLoading,
    isCreating,
    error,
    createError,
    onChannelNameChange,
    onChannelTypeChange,
    onSelect,
    onCreate,
}: ChannelsSectionProps) {
    return (
        <section className="rooms-section">
            <h2>Channels</h2>

            <form className="room-form" onSubmit={onCreate}>
                <label className="room-name-field">
                    Channel name
                    <input
                        type="text"
                        value={channelName}
                        onChange={(event) => onChannelNameChange(event.target.value)}
                        placeholder="e.g. general"
                        maxLength={100}
                        required
                        disabled={!selectedSpaceId}
                    />
                </label>

                <label className="room-name-field">
                    Channel type
                    <select
                        value={channelType}
                        onChange={(event) =>
                            onChannelTypeChange(Number(event.target.value) as ChannelType)
                        }
                        disabled={!selectedSpaceId}
                    >
                        <option value={0}>Text</option>
                        <option value={1}>Voice</option>
                    </select>
                </label>

                <button
                    type="submit"
                    disabled={!selectedSpaceId || isCreating || isLoading}
                >
                    {isCreating ? "Creating..." : "Create channel"}
                </button>
            </form>

            {createError && (
                <p className="form-error" role="alert">{createError}</p>
            )}

            {error && (
                <p className="form-error" role="alert">{error}</p>
            )}

            {!selectedSpaceId ? (
                <p className="rooms-message">
                    Create or select a space to view its channels.
                </p>
            ) : isLoading ? (
                <p className="rooms-message">Loading channels...</p>
            ) : channels.length === 0 ? (
                <p className="rooms-message">
                    No channels yet. Create your first channel above.
                </p>
            ) : (
                <div className="workspace-channel-list">
                    {channels.map((channel) => (
                        <button
                            type="button"
                            key={channel.id}
                            className={
                                selectedChannelId === channel.id
                                    ? "workspace-select-button is-active"
                                    : "workspace-select-button"
                            }
                            aria-pressed={selectedChannelId === channel.id}
                            onClick={() => onSelect(channel.id)}
                        >
                            <span aria-hidden="true">
                                {channel.type === 0 ? "#" : "🔊"}
                            </span>{" "}
                            {channel.name}
                        </button>
                    ))}
                </div>
            )}
        </section>
    );
}