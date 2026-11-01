
import { useNavigate } from "react-router-dom";
import { apiClient } from "@/api/apiClient";
import { useAuthStore } from "@/features/auth/store/authStore";
import { useSpaces } from "@/features/dashboard/hooks/useSpaces";
import { useChannels } from "@/features/dashboard/hooks/useChannels";
import { useSpaceMembers } from "@/features/dashboard/hooks/useSpaceMembers";

export function useDashboard() {
    const navigate = useNavigate();
    const clearTokens = useAuthStore((state) => state.clearTokens);

    const spaces = useSpaces();
    const channels = useChannels(spaces.selectedSpaceId);
    const members = useSpaceMembers(spaces.selectedSpaceId);

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

    const selectedSpace = spaces.spaces.find(
        (space) => space.id === spaces.selectedSpaceId,
    );

    return {
        ...spaces,
        ...channels,
        ...members,
        selectedSpace,
        handleLogout,
    };
}
