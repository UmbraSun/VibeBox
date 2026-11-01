import type { SyntheticEvent } from "react";
import type { SpaceSummary } from "@/api/spacesApi";

interface SpacesSectionProps {
    spaces: SpaceSummary[];
    selectedSpaceId: string;
    spaceName: string;
    isLoading: boolean;
    isCreating: boolean;
    error: string | null;
    createError: string | null;
    onSpaceNameChange: (value: string) => void;
    onSelect: (id: string) => void;
    onCreate: (event: SyntheticEvent<HTMLFormElement>) => void;
}

export function SpacesSection({
    spaces,
    selectedSpaceId,
    spaceName,
    isLoading,
    isCreating,
    error,
    createError,
    onSpaceNameChange,
    onSelect,
    onCreate,
}: SpacesSectionProps) {
    return (
        <section className="rooms-section">
            <h2>Spaces</h2>

            <form className="room-form" onSubmit={onCreate}>
                <label className="room-name-field">
                    Space name
                    <input
                        type="text"
                        value={spaceName}
                        onChange={(event) => onSpaceNameChange(event.target.value)}
                        placeholder="e.g. Friends"
                        maxLength={100}
                        required
                    />
                </label>

                <button type="submit" disabled={isCreating}>
                    {isCreating ? "Creating..." : "Create space"}
                </button>
            </form>

            {createError && (
                <p className="form-error" role="alert">{createError}</p>
            )}

            {error && (
                <p className="form-error" role="alert">{error}</p>
            )}

            {isLoading ? (
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
                            onClick={() => onSelect(space.id)}
                        >
                            {space.name}
                        </button>
                    ))}
                </div>
            )}
        </section>
    );
}