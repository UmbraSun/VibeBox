
import { useEffect, useState, type SyntheticEvent } from "react";
import {
    spacesApi,
    type ChannelSummary,
    type ChannelType,
} from "@/api/spacesApi";
import { getErrorMessage } from "@/features/dashboard/utils/getErrorMessage";

export function useChannels(selectedSpaceId: string) {
    const [channels, setChannels] = useState<ChannelSummary[]>([]);
    const [channelName, setChannelName] = useState("");
    const [channelType, setChannelType] = useState<ChannelType>(0);
    const [selectedChannelId, setSelectedChannelId] = useState("");
    const [isLoadingChannels, setIsLoadingChannels] = useState(false);
    const [isCreatingChannel, setIsCreatingChannel] = useState(false);
    const [channelsError, setChannelsError] = useState<string | null>(null);
    const [createChannelError, setCreateChannelError] = useState<string | null>(null);

    useEffect(() => {
        let active = true;

        setChannels([]);
        setSelectedChannelId("");
        setChannelsError(null);

        if (!selectedSpaceId) {
            setIsLoadingChannels(false);
            return;
        }

        async function loadChannels() {
            setIsLoadingChannels(true);

            try {
                const result = await spacesApi.getChannels(selectedSpaceId);

                if (!active) return;

                setChannels(result);

                const firstTextChannel = result.find(
                    (channel) => channel.type === 0,
                );

                setSelectedChannelId(
                    firstTextChannel?.id ?? result[0]?.id ?? "",
                );
            } catch (error: unknown) {
                if (active) {
                    setChannelsError(
                        getErrorMessage(error, "Unable to load channels."),
                    );
                }
            } finally {
                if (active) {
                    setIsLoadingChannels(false);
                }
            }
        }

        void loadChannels();

        return () => {
            active = false;
        };
    }, [selectedSpaceId]);

    async function handleCreateChannel(
        event: SyntheticEvent<HTMLFormElement>,
    ) {
        event.preventDefault();

        const name = channelName.trim();

        if (!selectedSpaceId) {
            setCreateChannelError("Select a space first.");
            return;
        }

        if (!name) {
            setCreateChannelError("Enter a channel name.");
            return;
        }

        setCreateChannelError(null);
        setIsCreatingChannel(true);

        try {
            const created = await spacesApi.createChannel(selectedSpaceId, {
                name,
                type: channelType,
            });

            const updatedChannels = await spacesApi.getChannels(selectedSpaceId);

            setChannels(updatedChannels);
            setSelectedChannelId(created.id);
            setChannelName("");
        } catch (error: unknown) {
            setCreateChannelError(
                getErrorMessage(error, "Unable to create the channel."),
            );
        } finally {
            setIsCreatingChannel(false);
        }
    }

    const selectedChannel = channels.find(
        (channel) => channel.id === selectedChannelId,
    );

    return {
        channels,
        channelName,
        setChannelName,
        channelType,
        setChannelType,
        selectedChannelId,
        setSelectedChannelId,
        selectedChannel,
        isLoadingChannels,
        isCreatingChannel,
        channelsError,
        createChannelError,
        handleCreateChannel,
    };
}
