
import { useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";
import { useAuthStore } from "@/features/auth/store/authStore";

const RTC_CONFIGURATION: RTCConfiguration = {
    iceServers: [{ urls: "stun:stun.l.google.com:19302" }],
};

type RemoteStreams = Record<string, MediaStream>;

function getUserIdFromAccessToken(token: string | null): string | null {
    if (!token) return null;

    try {
        const payload = token.split(".")[1];
        if (!payload) return null;

        const base64 = payload
            .replace(/-/g, "+")
            .replace(/_/g, "/");

        const decoded = atob(base64);
        const bytes = Uint8Array.from(
            decoded,
            (character) => character.charCodeAt(0),
        );

        const claims = JSON.parse(
            new TextDecoder().decode(bytes),
        ) as { sub?: string };

        return claims.sub ?? null;
    } catch {
        return null;
    }
}

export function useWebRtcCall(
    roomId: string | undefined,
    connection: HubConnection | null,
    localStream: MediaStream | null,
) {
    const [remoteStreams, setRemoteStreams] =
        useState<RemoteStreams>({});

    const localStreamRef = useRef<MediaStream | null>(localStream);
    const peersRef = useRef(new Map<string, RTCPeerConnection>());
    const pendingOffersRef = useRef(new Map<string, string>());
    const pendingCandidatesRef = useRef(
        new Map<string, RTCIceCandidateInit[]>(),
    );
    const pendingPeersRef = useRef(new Set<string>());
    const operationQueuesRef = useRef(
        new Map<string, Promise<void>>(),
    );

    const processPendingRef = useRef<
        (userId: string) => Promise<void>
    >(async () => {});

    const syncTracksRef = useRef<
        (userId: string, peer: RTCPeerConnection) => Promise<void>
    >(async () => {});

    localStreamRef.current = localStream;

    useEffect(() => {
        if (!roomId || !connection) return;

        const hub = connection;
        let active = true;

        const peers = peersRef.current;
        const pendingOffers = pendingOffersRef.current;
        const pendingCandidates = pendingCandidatesRef.current;
        const pendingPeers = pendingPeersRef.current;
        const operationQueues = operationQueuesRef.current;

        operationQueues.clear();

        const currentUserId = getUserIdFromAccessToken(
            useAuthStore.getState().accessToken,
        );

        function enqueuePeerOperation(
            userId: string,
            operation: () => Promise<void>,
        ): Promise<void> {
            const previous =
                operationQueues.get(userId) ?? Promise.resolve();

            const task = previous
                .catch(() => undefined)
                .then(async () => {
                    if (!active) return;
                    await operation();
                });

            operationQueues.set(userId, task);

            return task.finally(() => {
                if (operationQueues.get(userId) === task) {
                    operationQueues.delete(userId);
                }
            });
        }

        function runSafely(
            userId: string,
            operation: () => Promise<void>,
        ) {
            void enqueuePeerOperation(userId, operation).catch(
                (error: unknown) => {
                    console.error(
                        `[WebRTC] Operation failed for ${userId}`,
                        error,
                    );
                },
            );
        }

        function createPeer(userId: string): RTCPeerConnection {
            const existing = peers.get(userId);
            if (existing) return existing;

            const peer = new RTCPeerConnection(RTC_CONFIGURATION);

            peers.set(userId, peer);

            console.info("[WebRTC] Peer created", { userId });

            peer.ontrack = (event) => {
                if (!active) return;

                console.info("[WebRTC] Remote track received", {
                    userId,
                    kind: event.track.kind,
                    readyState: event.track.readyState,
                    streams: event.streams.length,
                });

                const remoteStream = event.streams[0];
                if (!remoteStream) return;

                setRemoteStreams((current) => ({
                    ...current,
                    [userId]: remoteStream,
                }));
            };

            peer.onicecandidate = (event) => {
                if (!active || !event.candidate) return;

                const candidate = event.candidate;

                void hub.invoke(
                    "SendIceCandidate",
                    roomId,
                    userId,
                    candidate.candidate,
                    candidate.sdpMid,
                    candidate.sdpMLineIndex,
                ).catch((error: unknown) => {
                    console.error(
                        "[WebRTC] Failed to send ICE candidate",
                        error,
                    );
                });
            };

            peer.onconnectionstatechange = () => {
                console.info("[WebRTC] Connection state", {
                    userId,
                    state: peer.connectionState,
                    signalingState: peer.signalingState,
                    iceConnectionState: peer.iceConnectionState,
                });
            };

            peer.oniceconnectionstatechange = () => {
                console.info("[WebRTC] ICE state", {
                    userId,
                    state: peer.iceConnectionState,
                });
            };

            return peer;
        }

        async function flushCandidates(
            userId: string,
            peer: RTCPeerConnection,
        ) {
            if (!peer.remoteDescription) return;

            const candidates = pendingCandidates.get(userId) ?? [];

            for (const candidate of candidates) {
                if (!active) return;

                await peer.addIceCandidate(candidate);
            }

            pendingCandidates.delete(userId);
        }

        async function syncLocalTracksCore(
            userId: string,
            peer: RTCPeerConnection,
            allowNegotiation: boolean,
        ) {
            const stream = localStreamRef.current;

            if (!stream || !active || peer.signalingState === "closed") {
                return;
            }

            let needsNegotiation = false;

            for (const track of stream.getTracks()) {
                if (track.readyState !== "live") continue;

                const transceiver = peer
                    .getTransceivers()
                    .find(
                        (item) =>
                            item.receiver.track.kind === track.kind,
                    );

                const sender = transceiver?.sender;

                if (sender) {
                    if (sender.track?.id !== track.id) {
                        await sender.replaceTrack(track);
                    }
                } else {
                    peer.addTrack(track, stream);
                    needsNegotiation = true;
                }
            }

            console.info("[WebRTC] Local tracks synchronized", {
                userId,
                signalingState: peer.signalingState,
                senders: peer.getSenders().map((sender) => ({
                    kind: sender.track?.kind ?? null,
                    readyState: sender.track?.readyState ?? null,
                    enabled: sender.track?.enabled ?? null,
                })),
            });

            if (needsNegotiation) {
                pendingPeers.add(userId);
            }

            if (
                allowNegotiation &&
                needsNegotiation &&
                peer.signalingState === "stable"
            ) {
                await negotiateCore(userId);
            }
        }

        async function negotiateCore(userId: string) {
            if (!active) return;

            const stream = localStreamRef.current;

            if (!stream) {
                pendingPeers.add(userId);
                return;
            }

            const peer = peers.get(userId);

            if (!peer || peer.signalingState === "closed") return;

            // Only one participant initiates offers for a given peer.
            if (
                !currentUserId ||
                currentUserId.localeCompare(userId) >= 0
            ) {
                pendingPeers.delete(userId);
                return;
            }

            if (peer.signalingState !== "stable") {
                // Keep the request pending until the current negotiation finishes.
                pendingPeers.add(userId);
                return;
            }

            pendingPeers.delete(userId);

            const offer = await peer.createOffer();

            if (!active || peer.signalingState !== "stable") return;

            await peer.setLocalDescription(offer);

            if (!active || !peer.localDescription) return;

            await hub.invoke(
                "SendOffer",
                roomId,
                userId,
                peer.localDescription.sdp,
            );

            console.info("[WebRTC] Offer sent", { userId });
        }

        async function handleOfferCore(
            userId: string,
            sdp: string,
        ) {
            if (!active) return;

            if (!localStreamRef.current) {
                pendingOffers.set(userId, sdp);
                pendingPeers.add(userId);
                return;
            }

            const peer = createPeer(userId);

            if (peer.signalingState !== "stable") {
                pendingOffers.set(userId, sdp);

                console.warn("[WebRTC] Offer queued", {
                    userId,
                    signalingState: peer.signalingState,
                });

                return;
            }

            // Add local tracks before answering, without creating another offer.
            await syncLocalTracksCore(userId, peer, false);

            await peer.setRemoteDescription({
                type: "offer",
                sdp,
            });

            pendingOffers.delete(userId);

            await flushCandidates(userId, peer);

            const answer = await peer.createAnswer();

            await peer.setLocalDescription(answer);

            if (!active || !peer.localDescription) return;

            await hub.invoke(
                "SendAnswer",
                roomId,
                userId,
                peer.localDescription.sdp,
            );

            console.info("[WebRTC] Answer sent", { userId });
        }

        async function handleAnswerCore(
            userId: string,
            sdp: string,
        ) {
            if (!active) return;

            const peer = peers.get(userId);

            if (!peer || peer.signalingState !== "have-local-offer") {
                console.warn("[WebRTC] Answer ignored", {
                    userId,
                    signalingState: peer?.signalingState ?? "no peer",
                });
                return;
            }

            await peer.setRemoteDescription({
                type: "answer",
                sdp,
            });

            await flushCandidates(userId, peer);

            console.info("[WebRTC] Answer applied", { userId });

            await processPendingCore(userId);
        }

        async function handleCandidateCore(
            userId: string,
            candidate: string,
            sdpMid: string | null,
            sdpMLineIndex: number | null,
        ) {
            if (!active) return;

            const peer = peers.get(userId);

            const iceCandidate: RTCIceCandidateInit = {
                candidate,
                sdpMid,
                sdpMLineIndex,
            };

            if (!peer || !peer.remoteDescription) {
                const queued = pendingCandidates.get(userId) ?? [];

                queued.push(iceCandidate);
                pendingCandidates.set(userId, queued);
                return;
            }

            await peer.addIceCandidate(iceCandidate);
        }

        async function processPendingCore(userId: string) {
            if (!active || !localStreamRef.current) return;

            const peer = peers.get(userId);

            if (!peer) {
                if (pendingOffers.has(userId) || pendingPeers.has(userId)) {
                    createPeer(userId);
                } else {
                    return;
                }
            }

            const currentPeer = peers.get(userId);
            if (!currentPeer) return;

            if (currentPeer.signalingState !== "stable") return;

            const pendingOffer = pendingOffers.get(userId);

            if (pendingOffer) {
                await handleOfferCore(userId, pendingOffer);
                return;
            }

            await syncLocalTracksCore(userId, currentPeer, false);

            if (pendingPeers.has(userId)) {
                await negotiateCore(userId);
            }
        }

        syncTracksRef.current = (userId, peer) =>
            enqueuePeerOperation(userId, async () => {
                await syncLocalTracksCore(userId, peer, true);
            });

        processPendingRef.current = (userId) =>
            enqueuePeerOperation(userId, () => processPendingCore(userId));

        const onParticipantJoined = (
            eventRoomId: string,
            userId: string,
        ) => {
            if (!active || eventRoomId !== roomId) return;

            console.info("[WebRTC] ParticipantJoined", {
                userId,
                hasLocalStream: Boolean(localStreamRef.current),
                peerExists: peers.has(userId),
            });

            runSafely(userId, async () => {
                const peer = createPeer(userId);

                if (localStreamRef.current) {
                    await syncLocalTracksCore(userId, peer, true);
                } else {
                    pendingPeers.add(userId);
                }
            });
        };

        const onParticipantLeft = (
            eventRoomId: string,
            userId: string,
        ) => {
            if (eventRoomId !== roomId) return;

            const peer = peers.get(userId);

            if (peer) {
                peer.ontrack = null;
                peer.onicecandidate = null;
                peer.close();
            }

            peers.delete(userId);
            pendingOffers.delete(userId);
            pendingCandidates.delete(userId);
            pendingPeers.delete(userId);
            operationQueues.delete(userId);

            setRemoteStreams((current) => {
                const next = { ...current };
                delete next[userId];
                return next;
            });

            console.info("[WebRTC] ParticipantLeft", { userId });
        };

        const onReceiveOffer = (
            eventRoomId: string,
            userId: string,
            sdp: string,
        ) => {
            if (eventRoomId !== roomId) return;

            runSafely(userId, () => handleOfferCore(userId, sdp));
        };

        const onReceiveAnswer = (
            eventRoomId: string,
            userId: string,
            sdp: string,
        ) => {
            if (eventRoomId !== roomId) return;

            runSafely(userId, () => handleAnswerCore(userId, sdp));
        };

        const onReceiveIceCandidate = (
            eventRoomId: string,
            userId: string,
            candidate: string,
            sdpMid: string | null,
            sdpMLineIndex: number | null,
        ) => {
            if (eventRoomId !== roomId) return;

            runSafely(userId, () =>
                handleCandidateCore(
                    userId,
                    candidate,
                    sdpMid,
                    sdpMLineIndex,
                ),
            );
        };

        hub.on("ParticipantJoined", onParticipantJoined);
        hub.on("ParticipantLeft", onParticipantLeft);
        hub.on("ReceiveOffer", onReceiveOffer);
        hub.on("ReceiveAnswer", onReceiveAnswer);
        hub.on("ReceiveIceCandidate", onReceiveIceCandidate);

        return () => {
            active = false;

            hub.off("ParticipantJoined", onParticipantJoined);
            hub.off("ParticipantLeft", onParticipantLeft);
            hub.off("ReceiveOffer", onReceiveOffer);
            hub.off("ReceiveAnswer", onReceiveAnswer);
            hub.off("ReceiveIceCandidate", onReceiveIceCandidate);

            for (const peer of peers.values()) {
                peer.ontrack = null;
                peer.onicecandidate = null;
                peer.close();
            }

            peers.clear();
            pendingOffers.clear();
            pendingCandidates.clear();
            pendingPeers.clear();
            operationQueues.clear();

            processPendingRef.current = async () => {};
            syncTracksRef.current = async () => {};

            setRemoteStreams({});
        };
    }, [roomId, connection]);

    useEffect(() => {
        if (!localStream || !roomId || !connection) return;

        const userIds = new Set([
            ...pendingOffersRef.current.keys(),
            ...pendingPeersRef.current,
            ...peersRef.current.keys(),
        ]);

        for (const userId of userIds) {
            void processPendingRef.current(userId).catch(
                (error: unknown) => {
                    console.error(
                        "[WebRTC] Failed to process pending negotiation",
                        { userId, error },
                    );
                },
            );
        }
    }, [localStream, roomId, connection]);

    useEffect(() => {
        if (localStream || !roomId || !connection) return;

        async function detachLocalTracks() {
            for (const peer of peersRef.current.values()) {
                await Promise.all(
                    peer
                        .getSenders()
                        .filter((sender) => sender.track !== null)
                        .map((sender) => sender.replaceTrack(null)),
                );
            }
        }

        void detachLocalTracks().catch((error: unknown) => {
            console.error(
                "[WebRTC] Failed to detach local tracks",
                error,
            );
        });
    }, [localStream, roomId, connection]);

    return { remoteStreams };
}
