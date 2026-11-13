
import { useState, type ChangeEvent } from "react";
import type { MessageResponse } from "@/api/messagesApi";
import {
    formatFileSize,
    validateFiles,
} from "@/features/messages/utils/fileValidation";

interface MessageComposerProps {
    isSending: boolean;
    replyToMessage: MessageResponse | null;
    onCancelReply: () => void;
    onSend: (
        content: string,
        files: File[],
        replyToMessageId?: string,
    ) => Promise<void>;
}

export function MessageComposer({
    isSending,
    replyToMessage,
    onCancelReply,
    onSend,
}: MessageComposerProps) {
    const [content, setContent] = useState("");
    const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
    const [fileError, setFileError] = useState<string | null>(null);

    function handleFilesChange(event: ChangeEvent<HTMLInputElement>) {
        const files = Array.from(event.currentTarget.files ?? []);
        event.currentTarget.value = "";

        if (files.length === 0) return;

        const mergedFiles = [...selectedFiles];

        for (const file of files) {
            const alreadySelected = mergedFiles.some(
                (existingFile) =>
                    existingFile.name === file.name &&
                    existingFile.size === file.size &&
                    existingFile.lastModified === file.lastModified,
            );

            if (!alreadySelected) {
                mergedFiles.push(file);
            }
        }

        const validationError = validateFiles(mergedFiles);

        if (validationError) {
            setFileError(validationError);
            return;
        }

        setSelectedFiles(mergedFiles);
        setFileError(null);
    }

    function removeSelectedFile(indexToRemove: number) {
        const updatedFiles = selectedFiles.filter(
            (_, index) => index !== indexToRemove,
        );

        setSelectedFiles(updatedFiles);
        setFileError(validateFiles(updatedFiles));
    }

    async function handleSubmit(event: ChangeEvent<HTMLFormElement>) {
        event.preventDefault();

        const message = content.trim();

        if (isSending || (!message && selectedFiles.length === 0)) {
            return;
        }

        const validationError = validateFiles(selectedFiles);

        if (validationError) {
            setFileError(validationError);
            return;
        }

        try {
            await onSend(
                message,
                selectedFiles,
                replyToMessage?.id,
            );

            setContent("");
            setSelectedFiles([]);
            setFileError(null);
        } catch {
            // Error is handled by the parent hook.
        }
    }

    return (
        <form className="room-form channel-chat-form" onSubmit={handleSubmit}>
            {replyToMessage && (
                <div className="channel-chat-replying">
                    <div>
                        <strong>
                            Replying to {replyToMessage.senderEmail}
                        </strong>

                        <p>
                            {replyToMessage.content || "Attachment"}
                        </p>
                    </div>

                    <button
                        type="button"
                        onClick={onCancelReply}
                        disabled={isSending}
                        aria-label="Cancel reply"
                    >
                        Cancel
                    </button>
                </div>
            )}

            {fileError && (
                <p className="form-error" role="alert">
                    {fileError}
                </p>
            )}

            <label className="room-name-field">
                Message
                <textarea
                    value={content}
                    onChange={(event) => setContent(event.target.value)}
                    placeholder="Write a message..."
                    maxLength={4000}
                    rows={3}
                    disabled={isSending}
                />
            </label>

            <label className="room-name-field">
                Attach images, GIFs or videos (max. 5 files)
                <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm"
                    multiple
                    onChange={handleFilesChange}
                    disabled={isSending}
                />
            </label>

            {selectedFiles.length > 0 && (
                <ul className="channel-chat-selected-files">
                    {selectedFiles.map((file, index) => (
                        <li key={`${file.name}-${file.size}-${index}`}>
                            <span>
                                {file.name}
                                {" · "}
                                {formatFileSize(file.size)}
                            </span>

                            <button
                                type="button"
                                onClick={() => removeSelectedFile(index)}
                                disabled={isSending}
                            >
                                Remove
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            <button
                type="submit"
                disabled={
                    isSending ||
                    (!content.trim() && selectedFiles.length === 0)
                }
            >
                {isSending
                    ? "Sending..."
                    : selectedFiles.length > 0
                      ? `Send (${selectedFiles.length} file${selectedFiles.length === 1 ? "" : "s"})`
                      : "Send"}
            </button>
        </form>
    );
}
