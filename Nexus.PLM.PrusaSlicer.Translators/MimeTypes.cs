namespace Nexus.PLM.PrusaSlicer.Translators;

/// <summary>
/// The MIME values this repo's translators speak. Values follow the installation's format
/// catalog (the MIME Types app) — 3MF is <c>model/3mf</c>, never the long vendor-tree value.
/// </summary>
public static class MimeTypes
{
    /// <summary>3MF project/model (<c>.3mf</c>).</summary>
    public const string ThreeMf = "model/3mf";

    /// <summary>STL mesh.</summary>
    public const string Stl = "model/stl";

    /// <summary>PNG raster.</summary>
    public const string Png = "image/png";
}
