using System.IO.Compression;
using System.Text;
using Nexus.PLM.Addin.Sdk.Translators;
using Nexus.PLM.Addin.Sdk.Translators.Testing;
using Xunit;

namespace Nexus.PLM.PrusaSlicer.Translators.Tests;

/// <summary>Contract tests per C# translator.</summary>
public class ThreeMfToStlContractTests : TranslatorContractTests<ThreeMfToStlTranslator> { }

/// <summary>See <see cref="ThreeMfToStlContractTests"/>.</summary>
public class ThreeMfToPngContractTests : TranslatorContractTests<ThreeMfToPngTranslator> { }

/// <summary>The manifest half of this MIXED plugin folder (DLL + translators.json).</summary>
public class PrusaManifestTests : ManifestContractTests
{
    /// <inheritdoc/>
    protected override string ManifestPath =>
        Path.Combine(AppContext.BaseDirectory, "translators", TranslatorManifest.FileName);

    /// <summary>The one scripted conversion, keyed as reserved in the ownership table.</summary>
    [Fact]
    public void CarriesTheSlicingEntry()
    {
        var entry = Assert.Single(Load().Translators);
        Assert.Equal("3mf_to_gcode", entry.Key);
        Assert.Equal("model/3mf", entry.InputMimeType);
        Assert.Equal("text/x-gcode", entry.OutputMimeType);
        Assert.Equal("${ENV:PRUSASLICER_CMD}", entry.Command);
    }

    /// <summary>DLL keys and manifest keys share one folder; a clash would make the server
    /// drop whichever registers second.</summary>
    [Fact]
    public void ManifestKeysDoNotCollideWithTheDllTranslators()
    {
        string[] dllKeys = [new ThreeMfToStlTranslator().Key, new ThreeMfToPngTranslator().Key];

        Assert.DoesNotContain(Load().Translators, t => dllKeys.Contains(t.Key));
    }
}

/// <summary>Builds tiny real inputs in-test so the C# conversions run anywhere.</summary>
public class ConversionTests
{
    private static Stream Cube3mf(bool withThumbnail)
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var entry = zip.CreateEntry("3D/3dmodel.model").Open())
                entry.Write(Encoding.UTF8.GetBytes("""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <model unit="millimeter" xmlns="http://schemas.microsoft.com/3dmanufacturing/core/2015/02">
                      <resources><object id="1" type="model"><mesh>
                        <vertices>
                          <vertex x="0" y="0" z="0"/><vertex x="10" y="0" z="0"/>
                          <vertex x="10" y="10" z="0"/><vertex x="0" y="0" z="10"/>
                        </vertices>
                        <triangles>
                          <triangle v1="0" v2="1" v3="2"/><triangle v1="0" v2="1" v3="3"/>
                          <triangle v1="1" v2="2" v3="3"/><triangle v1="2" v2="0" v3="3"/>
                        </triangles>
                      </mesh></object></resources>
                      <build><item objectid="1"/></build>
                    </model>
                    """));
            if (withThumbnail)
            {
                using (var small = zip.CreateEntry("Metadata/small.png").Open())
                    small.Write(TinyPng);
                using (var thumb = zip.CreateEntry("Metadata/thumbnail.png").Open())
                    thumb.Write([.. TinyPng, 0, 0, 0, 0]); // larger must win
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static async Task<byte[]> Run(ITranslator t, Stream input)
    {
        await using var output = await t.TranslateAsync(input);
        using var ms = new MemoryStream();
        await output.CopyToAsync(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task Stl_IsAValidBinaryStlOfTheMesh()
    {
        var bytes = await Run(new ThreeMfToStlTranslator(), Cube3mf(withThumbnail: false));

        Assert.Equal(84 + 4 * 50, bytes.Length);                       // header + 4 triangles
        Assert.Equal(4u, BitConverter.ToUInt32(bytes, 80));            // triangle count
        // First triangle, first vertex after the 12-byte normal: (0,0,0); second: (10,0,0).
        Assert.Equal(0f, BitConverter.ToSingle(bytes, 84 + 12));
        Assert.Equal(10f, BitConverter.ToSingle(bytes, 84 + 12 + 12));
    }

    [Fact]
    public async Task Png_ExtractsTheLargestMetadataThumbnail()
    {
        var bytes = await Run(new ThreeMfToPngTranslator(), Cube3mf(withThumbnail: true));

        Assert.Equal(TinyPng.Length + 4, bytes.Length);
        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], bytes[..4]);
    }

    [Fact]
    public async Task Png_WithoutAThumbnail_PointsAtTheAlternatives()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Run(new ThreeMfToPngTranslator(), Cube3mf(withThumbnail: false)));

        Assert.Contains("3mf_to_glb", ex.Message);
    }
}

/// <summary>Repo-level rules across BOTH halves of the folder.</summary>
public class PrusaSlicerTranslatorSuiteTests
{
    [Fact]
    public void AllFiveReservedPairsAreDistinct()
    {
        var manifest = TranslatorManifest.Load(
            Path.Combine(AppContext.BaseDirectory, "translators", TranslatorManifest.FileName));

        var pairs = new List<(string, string)>
        {
            (new ThreeMfToStlTranslator().InputMimeType, new ThreeMfToStlTranslator().OutputMimeType),
            (new ThreeMfToPngTranslator().InputMimeType, new ThreeMfToPngTranslator().OutputMimeType),
        };
        pairs.AddRange(manifest.Translators.Select(t => (t.InputMimeType, t.OutputMimeType)));

        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }
}
