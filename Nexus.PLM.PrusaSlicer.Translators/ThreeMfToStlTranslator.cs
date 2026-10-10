using Nexus.PLM.Addin.Sdk.Translators;

namespace Nexus.PLM.PrusaSlicer.Translators;

/// <summary>Re-encodes a 3MF's mesh as binary STL — pure .NET, no slicer needed.</summary>
public sealed class ThreeMfToStlTranslator : ITranslator
{
    /// <inheritdoc/>
    public string Key => "3mf_to_stl";

    /// <inheritdoc/>
    public string Label => "3MF → STL";

    /// <inheritdoc/>
    public string Description =>
        "Re-encodes a 3MF project's mesh (all build items flattened) as binary STL for printing and interchange. Pure .NET — no slicer needed on the server.";

    /// <inheritdoc/>
    public string InputMimeType => MimeTypes.ThreeMf;

    /// <inheritdoc/>
    public string OutputMimeType => MimeTypes.Stl;

    /// <inheritdoc/>
    public string OutputFileExtension => ".stl";

    /// <inheritdoc/>
    public Task<Stream> TranslateAsync(Stream input, CancellationToken cancellationToken = default)
    {
        var mesh = ThreeMfMesh.Load(input);

        // Binary STL: 80-byte header, uint32 triangle count, then per triangle a float3
        // normal (zeros are legal — readers recompute), three float3 vertices, uint16 attrs.
        var ms = new MemoryStream(84 + mesh.Triangles.Count * 50);
        using var w = new BinaryWriter(ms, System.Text.Encoding.ASCII, leaveOpen: true);
        var header = new byte[80];
        System.Text.Encoding.ASCII.GetBytes("Converted from 3MF by Nexus PLM").CopyTo(header, 0);
        w.Write(header);
        w.Write((uint)mesh.Triangles.Count);

        foreach (var (a, b, c) in mesh.Triangles)
        {
            w.Write(0f); w.Write(0f); w.Write(0f);
            foreach (var index in (ReadOnlySpan<int>)[a, b, c])
            {
                var (x, y, z) = mesh.Vertices[index];
                w.Write(x); w.Write(y); w.Write(z);
            }
            w.Write((ushort)0);
        }

        w.Flush();
        ms.Position = 0;
        return Task.FromResult<Stream>(ms);
    }
}
