public interface IVaultFileAdapter
{
    // --- Core Import/Export ---
    Task<string> ExportVaultAsync(string outputPath, CancellationToken ct = default);
    Task ImportVaultAsync(string vaultFilePath, CancellationToken ct = default);

    // --- Attachments ---
    Task<string> SaveAttachmentAsync(string noteId, Stream file, string fileName, CancellationToken ct = default);
    Task<Stream?> GetAttachmentAsync(string noteId, string fileName, CancellationToken ct = default);
    Task DeleteAttachmentAsync(string noteId, string fileName, CancellationToken ct = default);

    // --- File Management ---
    Task<string> MoveVaultAsync(string newPath, CancellationToken ct = default);
    Task BackupVaultAsync(string targetDir, CancellationToken ct = default);
    Task PurgeTempFilesAsync(CancellationToken ct = default);
    Task<bool> ValidateVaultStructureAsync(CancellationToken ct = default);

    // --- Validation / Hashing ---
    Task<string> ComputeFileHashAsync(string path, CancellationToken ct = default);
    Task<bool> VerifyFileIntegrityAsync(string path, string expectedHash, CancellationToken ct = default);
}
