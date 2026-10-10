using System.IO.Compression;
using Nexus.PLM.Addin.Sdk.Translators;

namespace Nexus.PLM.PrusaSlicer.Translators;

/// <summary>
/// Extracts the preview thumbnail a slicer saved inside a 3MF project — PrusaSlicer writes
/// <c>Metadata/thumbnail.png</c>, the Bambu/Orca lineage writes plate PNGs under the same
/// folder. The "conversion" is an unzip, so it is instant and pure .NET.
/// </summary>
public sealed class ThreeMfToPngTranslator : ITranslator
{
    /// <inheritdoc/>
    public string Key => "3mf_to_png";

    /// <inheritdoc/>
    public string Label => "3MF → PNG preview";

    /// <inheritdoc/>
    public string Description =>
        "Extracts the largest PNG preview the slicer embedded in a 3MF project (Metadata folder).";

    /// <inheritdoc/>
    public string InputMimeType => MimeTypes.ThreeMf;

    /// <inheritdoc/>
    public string OutputMimeType => MimeTypes.Png;

    /// <inheritdoc/>
    public string OutputFileExtension => ".png";

    /// <inheritdoc/>
    public Task<Stream> TranslateAsync(Stream input, CancellationToken cancellationToken = default)
    {
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException(
                "The input is not a 3MF file (not a zip archive).", ex);
        }

        using (zip)
        {
            var candidate = zip.Entries
                .Where(e => e.FullName.StartsWith("Metadata/", StringComparison.OrdinalIgnoreCase)
                            && e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Length)
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "The 3MF carries no embedded preview PNG. Save the project from the " +
                    "slicer with thumbnails enabled, or use 3mf_to_glb/3mf_to_stl for geometry.");

            using var entry = candidate.Open();
            var ms = new MemoryStream();
            entry.CopyTo(ms);
            ms.Position = 0;
            return Task.FromResult<Stream>(ms);
        }
    }
}
