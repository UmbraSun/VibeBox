import { useCallback, useEffect, useState } from "react";

interface LocalMediaState {
    stream: MediaStream | null;
    error: string | null;
    isLoading: boolean;
}

export function useLocalMedia() {
    const [state, setState] = useState<LocalMediaState>({
        stream: null,
        error: null,
        isLoading: false,
    });

    const startMedia = useCallback(async () => {
        setState((current) => ({
            ...current,
            error: null,
            isLoading: true,
        }));

        try {
            const stream = await navigator.mediaDevices.getUserMedia({
                audio: true,
                video: true,
            });

            setState((current) => {
                current.stream?.getTracks().forEach((track) => track.stop());

                return {
                    stream,
                    error: null,
                    isLoading: false,
                };
            });
        } catch (error: unknown) {
            const message =
                error instanceof Error
                    ? error.message
                    : "Unable to access camera or microphone.";

            setState((current) => ({
                ...current,
                error: message,
                isLoading: false,
            }));
        }
    }, []);

    const stopMedia = useCallback(() => {
        setState((current) => {
            current.stream?.getTracks().forEach((track) => track.stop());

            return {
                stream: null,
                error: null,
                isLoading: false,
            };
        });
    }, []);

    useEffect(() => {
        return () => {
            setState((current) => {
                current.stream?.getTracks().forEach((track) => track.stop());
                return current;
            });
        };
    }, []);

    return {
        stream: state.stream,
        error: state.error,
        isLoading: state.isLoading,
        startMedia,
        stopMedia,
    };
}
