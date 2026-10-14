import { useEffect, useRef, useState } from "react";
import type { HubConnection } from "@microsoft/signalr";

const RTC_CONFIGURATION: RTCConfiguration = {
    iceServers: [
        { urls: "stun:stun.l.google.com:19302" },
    ],
};

type RemoteStreams = Record<string, MediaStream>;

export function useWebRtcCall(
    roomId: string | undefined,
    connection: HubConnection | null,
    localStream: MediaStream | null,
) {
    const [remoteStreams, setRemoteStreams] = useState<RemoteStreams>({});

    const localStreamRef = useRef<MediaStream | null>(localStream);
    const pendingOffers = useRef(new Map<string, string>());
    const pendingCandidates = useRef(
        new Map<string, RTCIceCandidateInit[]>(),
    );
    const pendingPeers = useRef(new Set<string>());
    const processPendingRef = useRef<() => void>(() => { });

    localStreamRef.current = localStream;

    useEffect(() => {
        if (!roomId || !connection) return;

        const hubConnection = connection;
        let active = true;

        const peers = new Map<string, RTCPeerConnection>();

        function addLocalTracks(peer: RTCPeerConnection) {
            const stream = localStreamRef.current;

            if (!stream) return;

            const existingTrackIds = new Set(
                peer.getSenders()
                    .map((sender) => sender.track?.id)
                    .filter((trackId): trackId is string => Boolean(trackId)),
            );

            for (const track of stream.getTracks()) {
                if (!existingTrackIds.has(track.id)) {
                    peer.addTrack(track, stream);
                }
            }
        }

        function createPeer(userId: string): RTCPeerConnection {
            const existingPeer = peers.get(userId);

            if (existingPeer) return existingPeer;

            const peer = new RTCPeerConnection(RTC_CONFIGURATION);
            peers.set(userId, peer);

            addLocalTracks(peer);

            peer.ontrack = (event) => {
                if (!active) return;

                const [stream] = event.streams;

                if (stream) {
                    setRemoteStreams((current) => ({
                        ...current,
                        [userId]: stream,
                    }));
                }
            };

            peer.onicecandidate = (event) => {
                if (!event.candidate || !active) return;

                const candidate = event.candidate;

                void hubConnection.invoke(
                    "SendIceCandidate",
                    roomId,
                    userId,
                    candidate.candidate,
                    candidate.sdpMid,
                    candidate.sdpMLineIndex,
                ).catch((error: unknown) => {
                    console.error("Failed to send ICE candidate.", error);
                });
            };

            return peer;
        }

        async function flushCandidates(
            userId: string,
            peer: RTCPeerConnection,
        ) {
            const candidates = pendingCandidates.current.get(userId) ?? [];
            pendingCandidates.current.delete(userId);

            for (const candidate of candidates) {
                await peer.addIceCandidate(
                    new RTCIceCandidate(candidate),
                );
            }
        }

        async function createOffer(userId: string) {
            if (!active || !localStreamRef.current) {
                pendingPeers.current.add(userId);
                return;
            }

            pendingPeers.current.delete(userId);

            const peer = createPeer(userId);

            if (peer.signalingState !== "stable") return;

            const offer = await peer.createOffer();
            await peer.setLocalDescription(offer);

            if (!active || !peer.localDescription) return;

            await hubConnection.invoke(
                "SendOffer",
                roomId,
                userId,
                peer.localDescription.sdp,
            );
        }

        async function handleOffer(userId: string, sdp: string) {
            if (!active || !localStreamRef.current) {
                pendingOffers.current.set(userId, sdp);
                return;
            }

            pendingOffers.current.delete(userId);

            const peer = createPeer(userId);

            await peer.setRemoteDescription(
                new RTCSessionDescription({
                    type: "offer",
                    sdp,
                }),
            );

            await flushCandidates(userId, peer);

            const answer = await peer.createAnswer();
            await peer.setLocalDescription(answer);

            if (!active || !peer.localDescription) return;

            await hubConnection.invoke(
                "SendAnswer",
                roomId,
                userId,
                peer.localDescription.sdp,
            );
        }

        async function handleAnswer(userId: string, sdp: string) {
            const peer = peers.get(userId);

            if (!peer || !active) return;

            await peer.setRemoteDescription(
                new RTCSessionDescription({
                    type: "answer",
                    sdp,
                }),
            );

            await flushCandidates(userId, peer);
        }

        async function handleIceCandidate(
            userId: string,
            candidate: string,
            sdpMid: string | null,
            sdpMLineIndex: number | null,
        ) {
            const iceCandidate: RTCIceCandidateInit = {
                candidate,
                sdpMid,
                sdpMLineIndex,
            };

            const peer = peers.get(userId);

            if (!peer?.remoteDescription) {
                const candidates =
                    pendingCandidates.current.get(userId) ?? [];

                candidates.push(iceCandidate);
                pendingCandidates.current.set(userId, candidates);
                return;
            }

            await peer.addIceCandidate(
                new RTCIceCandidate(iceCandidate),
            );
        }

        function removePeer(userId: string) {
            const peer = peers.get(userId);

            peer?.close();
            peers.delete(userId);

            pendingOffers.current.delete(userId);
            pendingCandidates.current.delete(userId);
            pendingPeers.current.delete(userId);

            setRemoteStreams((current) => {
                const next = { ...current };
                delete next[userId];
                return next;
            });
        }

        function runSafely(
            operation: () => Promise<void>,
        ) {
            void operation().catch((error: unknown) => {
                console.error("WebRTC signaling operation failed.", error);
            });
        }

        const onParticipantJoined = (
            eventRoomId: string,
            userId: string,
        ) => {
            if (eventRoomId !== roomId) return;
            runSafely(() => createOffer(userId));
        };

        const onParticipantLeft = (
            eventRoomId: string,
            userId: string,
        ) => {
            if (eventRoomId !== roomId) return;
            removePeer(userId);
        };

        const onReceiveOffer = (
            eventRoomId: string,
            userId: string,
            sdp: string,
        ) => {
            if (eventRoomId !== roomId) return;
            runSafely(() => handleOffer(userId, sdp));
        };

        const onReceiveAnswer = (
            eventRoomId: string,
            userId: string,
            sdp: string,
        ) => {
            if (eventRoomId !== roomId) return;
            runSafely(() => handleAnswer(userId, sdp));
        };

        const onReceiveIceCandidate = (
            eventRoomId: string,
            userId: string,
            candidate: string,
            sdpMid: string | null,
            sdpMLineIndex: number | null,
        ) => {
            if (eventRoomId !== roomId) return;

            runSafely(() =>
                handleIceCandidate(
                    userId,
                    candidate,
                    sdpMid,
                    sdpMLineIndex,
                ),
            );
        };

        processPendingRef.current = () => {
            if (!localStreamRef.current || !active) return;

            for (const [userId, sdp] of pendingOffers.current) {
                runSafely(() => handleOffer(userId, sdp));
            }

            for (const userId of pendingPeers.current) {
                runSafely(() => createOffer(userId));
            }
        };

        hubConnection.on("ParticipantJoined", onParticipantJoined);
        hubConnection.on("ParticipantLeft", onParticipantLeft);
        hubConnection.on("ReceiveOffer", onReceiveOffer);
        hubConnection.on("ReceiveAnswer", onReceiveAnswer);
        hubConnection.on("ReceiveIceCandidate", onReceiveIceCandidate);

        processPendingRef.current();

        return () => {
            active = false;
            processPendingRef.current = () => { };

            hubConnection.off("ParticipantJoined", onParticipantJoined);
            hubConnection.off("ParticipantLeft", onParticipantLeft);
            hubConnection.off("ReceiveOffer", onReceiveOffer);
            hubConnection.off("ReceiveAnswer", onReceiveAnswer);
            hubConnection.off("ReceiveIceCandidate", onReceiveIceCandidate);

            for (const peer of peers.values()) {
                peer.close();
            }

            peers.clear();
            pendingOffers.current.clear();
            pendingCandidates.current.clear();
            pendingPeers.current.clear();

            setRemoteStreams({});
        };
    }, [roomId, connection]);

    useEffect(() => {
        if (!localStream) return;

        processPendingRef.current();
    }, [localStream]);

    return { remoteStreams };
}