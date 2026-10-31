export function getCurrentUserId(token: string | null): string | null {
    if (!token) return null;

    try {
        const payload = token.split(".")[1];
        if (!payload) return null;

        const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
        const decoded = atob(
            normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "="),
        );

        const claims = JSON.parse(decoded) as Record<string, unknown>;

        const userId =
            claims.nameid ??
            claims.sub ??
            claims.userId ??
            claims[
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            ];

        return typeof userId === "string" ? userId : null;
    } catch {
        return null;
    }
}