    
import { useEffect, useState, type SyntheticEvent } from "react";
import {
    spacesApi,
    type SpaceMember,
} from "@/api/spacesApi";
import { getErrorMessage } from "@/features/dashboard/utils/getErrorMessage";

export function useSpaceMembers(selectedSpaceId: string) {
    const [members, setMembers] = useState<SpaceMember[]>([]);
    const [memberEmail, setMemberEmail] = useState("");
    const [isLoadingMembers, setIsLoadingMembers] = useState(false);
    const [isAddingMember, setIsAddingMember] = useState(false);
    const [memberError, setMemberError] = useState<string | null>(null);
    const [memberSuccess, setMemberSuccess] = useState<string | null>(null);

    useEffect(() => {
        let active = true;

        setMembers([]);
        setMemberError(null);
        setMemberSuccess(null);

        if (!selectedSpaceId) {
            setIsLoadingMembers(false);
            return;
        }

        async function loadMembers() {
            setIsLoadingMembers(true);

            try {
                const result = await spacesApi.getMembers(selectedSpaceId);

                if (active) {
                    setMembers(result);
                }
            } catch (error: unknown) {
                if (active) {
                    setMemberError(
                        getErrorMessage(
                            error,
                            "Unable to load space members.",
                        ),
                    );
                }
            } finally {
                if (active) {
                    setIsLoadingMembers(false);
                }
            }
        }

        void loadMembers();

        return () => {
            active = false;
        };
    }, [selectedSpaceId]);

    async function handleAddMember(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const email = memberEmail.trim();

        if (!selectedSpaceId || !email) return;

        setMemberError(null);
        setMemberSuccess(null);
        setIsAddingMember(true);

        try {
            await spacesApi.addMember(selectedSpaceId, { email });

            setMemberEmail("");
            setMemberSuccess("Member added successfully.");

            setMembers(await spacesApi.getMembers(selectedSpaceId));
        } catch (error: unknown) {
            setMemberError(
                getErrorMessage(error, "Unable to add member."),
            );
        } finally {
            setIsAddingMember(false);
        }
    }

    async function handleRemoveMember(userId: string) {
        if (!selectedSpaceId) return;

        setMemberError(null);
        setMemberSuccess(null);

        try {
            await spacesApi.removeMember(selectedSpaceId, userId);

            setMembers((currentMembers) =>
                currentMembers.filter(
                    (member) => member.userId !== userId,
                ),
            );

            setMemberSuccess("Member removed successfully.");
        } catch (error: unknown) {
            setMemberError(
                getErrorMessage(error, "Unable to remove member."),
            );
        }
    }

    return {
        members,
        memberEmail,
        setMemberEmail,
        isLoadingMembers,
        isAddingMember,
        memberError,
        memberSuccess,
        handleAddMember,
        handleRemoveMember,
    };
}
