const MAX_FILES_COUNT = 5;
const MAX_IMAGE_SIZE = 10 * 1024 * 1024;
const MAX_VIDEO_SIZE = 100 * 1024 * 1024;
const MAX_TOTAL_SIZE = 150 * 1024 * 1024;

export function formatFileSize(sizeBytes: number): string {
    if (sizeBytes < 1024 * 1024) {
        return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
    }

    return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function validateFiles(files: File[]): string | null {
    if (files.length > MAX_FILES_COUNT) {
        return `You can select a maximum of ${MAX_FILES_COUNT} files.`;
    }

    let totalSize = 0;

    for (const file of files) {
        const isVideo = file.type.startsWith("video/");
        const isImage = file.type.startsWith("image/");

        if (!isVideo && !isImage) {
            return `Unsupported file type: ${file.name}.`;
        }

        const maxSize = isVideo ? MAX_VIDEO_SIZE : MAX_IMAGE_SIZE;

        if (file.size === 0 || file.size > maxSize) {
            return isVideo
                ? `Video "${file.name}" must not exceed 100 MB.`
                : `Image "${file.name}" must not exceed 10 MB.`;
        }

        totalSize += file.size;

        if (totalSize > MAX_TOTAL_SIZE) {
            return "Total attachment size must not exceed 150 MB.";
        }
    }

    return null;
}