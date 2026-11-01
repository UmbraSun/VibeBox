
import { DashboardHeader } from "@/features/dashboard/components/DashboardHeader";
import { SpacesSection } from "@/features/dashboard/components/SpacesSection";
import { ChannelsSection } from "@/features/dashboard/components/ChannelsSection";
import { SpaceMembersSection } from "@/features/dashboard/components/SpaceMembersSection";
import { useDashboard } from "@/features/dashboard/hooks/useDashboard";
import { ChannelChat } from "@/features/messages/components/ChannelChat";
import { LiveKitCall } from "@/features/rooms/components/LiveKitCall";

export function DashboardPage() {
    const dashboard = useDashboard();

    return (
        <main className="dashboard-page">
            <DashboardHeader
                onLogout={() => void dashboard.handleLogout()}
            />

            <section className="dashboard-content">
                <h1>Your workspace</h1>

                <p className="auth-description">
                    Create spaces, chat in text channels, or join voice channels.
                </p>

                <SpacesSection
                    spaces={dashboard.spaces}
                    selectedSpaceId={dashboard.selectedSpaceId}
                    spaceName={dashboard.spaceName}
                    isLoading={dashboard.isLoadingSpaces}
                    isCreating={dashboard.isCreatingSpace}
                    error={dashboard.spacesError}
                    createError={dashboard.createSpaceError}
                    onSpaceNameChange={dashboard.setSpaceName}
                    onSelect={dashboard.setSelectedSpaceId}
                    onCreate={dashboard.handleCreateSpace}
                />

                <ChannelsSection
                    channels={dashboard.channels}
                    selectedSpaceId={dashboard.selectedSpaceId}
                    selectedChannelId={dashboard.selectedChannelId}
                    channelName={dashboard.channelName}
                    channelType={dashboard.channelType}
                    isLoading={dashboard.isLoadingChannels}
                    isCreating={dashboard.isCreatingChannel}
                    error={dashboard.channelsError}
                    createError={dashboard.createChannelError}
                    onChannelNameChange={dashboard.setChannelName}
                    onChannelTypeChange={dashboard.setChannelType}
                    onSelect={dashboard.setSelectedChannelId}
                    onCreate={dashboard.handleCreateChannel}
                />

                <SpaceMembersSection
                    selectedSpaceId={dashboard.selectedSpaceId}
                    ownerId={dashboard.selectedSpace?.ownerId}
                    members={dashboard.members}
                    memberEmail={dashboard.memberEmail}
                    isLoading={dashboard.isLoadingMembers}
                    isAdding={dashboard.isAddingMember}
                    error={dashboard.memberError}
                    success={dashboard.memberSuccess}
                    onMemberEmailChange={dashboard.setMemberEmail}
                    onAdd={dashboard.handleAddMember}
                    onRemove={(userId) =>
                        void dashboard.handleRemoveMember(userId)
                    }
                />

                {dashboard.selectedChannel?.type === 0 && (
                    <ChannelChat
                        channelId={dashboard.selectedChannel.id}
                        channelName={dashboard.selectedChannel.name}
                    />
                )}

                {dashboard.selectedChannel?.type === 1 && (
                    <section className="rooms-section">
                        <h2>
                            Voice · {dashboard.selectedChannel.name}
                        </h2>

                        <LiveKitCall
                            channelId={dashboard.selectedChannel.id}
                        />
                    </section>
                )}
            </section>
        </main>
    );
}
