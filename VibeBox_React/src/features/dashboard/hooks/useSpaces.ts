
import { useCallback, useEffect, useState } from "react";
import { spacesApi, type SpaceSummary } from "@/api/spacesApi";
import { getErrorMessage } from "@/features/dashboard/utils/getErrorMessage";

export function useSpaces() {
    const [spaces, setSpaces] = useState<SpaceSummary[]>([]);
    const [spaceName, setSpaceName] = useState("");
    const [selectedSpaceId, setSelectedSpaceId] = useState("");
    const [isLoadingSpaces, setIsLoadingSpaces] = useState(true);
    const [isCreatingSpace, setIsCreatingSpace] = useState(false);
    const [spacesError, setSpacesError] = useState<string | null>(null);
    const [createSpaceError, setCreateSpaceError] = useState<string | null>(null);

    const loadSpaces = useCallback(async (preferredSpaceId?: string) => {
        setSpacesError(null);
        setIsLoadingSpaces(true);

        try {
            const result = await spacesApi.getMySpaces();

            setSpaces(result);

            setSelectedSpaceId((currentId) => {
                if (
                    preferredSpaceId &&
                    result.some((space) => space.id === preferredSpaceId)
                ) {
                    return preferredSpaceId;
                }

                if (result.some((space) => space.id === currentId)) {
                    return currentId;
                }

                return result[0]?.id ?? "";
            });
        } catch (error: unknown) {
            setSpacesError(
                getErrorMessage(error, "Unable to load spaces."),
            );
        } finally {
            setIsLoadingSpaces(false);
        }
    }, []);

    useEffect(() => {
        void loadSpaces();
    }, [loadSpaces]);

    async function handleCreateSpace(
        event: React.SyntheticEvent<HTMLFormElement>,
    ) {
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
        } catch (error: unknown) {
            setCreateSpaceError(
                getErrorMessage(error, "Unable to create the space."),
            );
        } finally {
            setIsCreatingSpace(false);
        }
    }

    return {
        spaces,
        spaceName,
        setSpaceName,
        selectedSpaceId,
        setSelectedSpaceId,
        isLoadingSpaces,
        isCreatingSpace,
        spacesError,
        createSpaceError,
        loadSpaces,
        handleCreateSpace,
    };
}
