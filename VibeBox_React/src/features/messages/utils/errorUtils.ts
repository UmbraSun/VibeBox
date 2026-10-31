import axios from "axios";

export function getErrorMessage(
    error: unknown,
    fallback: string,
): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data;

        if (data instanceof Blob) {
            return fallback;
        }

        return data?.message ?? data?.error ?? fallback;
    }

    if (error instanceof Error) {
        return error.message;
    }

    return fallback;
}