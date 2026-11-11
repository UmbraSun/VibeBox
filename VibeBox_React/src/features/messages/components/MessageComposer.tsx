
import { useState, type ChangeEvent } from "react";
import {
    formatFileSize,
    validateFiles,
} from "@/features/messages/utils/fileValidation";

interface MessageComposerProps {
    isSending: boolean;
    onSend: (content: string, files: File[]) => Promise<void>;
}

export function MessageComposer({
    isSending,
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
            await onSend(message, selectedFiles);

            setContent("");
            setSelectedFiles([]);
            setFileError(null);
        } catch {
            // Error is handled by the parent hook.
        }
    }

    return (
        <form className="room-form channel-chat-form" onSubmit={handleSubmit}>
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
