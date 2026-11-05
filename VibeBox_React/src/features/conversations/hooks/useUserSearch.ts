import { useState } from "react";
import { usersApi, type UserSearchResponse } from "@/api/usersApi";

export function useUserSearch() {
    const [users, setUsers] = useState<UserSearchResponse[]>([]);
    const [isSearching, setIsSearching] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function searchUsers(email: string) {
        const query = email.trim();

        if (query.length < 3) {
            setUsers([]);
            setError(null);
            return;
        }

        setIsSearching(true);
        setError(null);

        try {
            setUsers(await usersApi.search(query));
        } catch {
            setError("Failed to search users.");
            setUsers([]);
        } finally {
            setIsSearching(false);
        }
    }

    function clearSearch() {
        setUsers([]);
        setError(null);
    }

    return {
        users,
        isSearching,
        error,
        searchUsers,
        clearSearch,
    };
}