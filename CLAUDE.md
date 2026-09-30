# CLAUDE.md — Nexus.PLM.PrusaSlicer.Addins

The Nexus PLM add-in for **PrusaSlicer**. Scaffold created 29 Sep 2026; no add-in code yet.

## Measured before writing anything (29 Sep 2026)

- **PrusaSlicer 2.9.6** (installed, `C:\Program Files\Prusa3D\PrusaSlicer`) has **no plug-in,
  scripting or menu API** in any build. Its only hooks:
  - **post-processing scripts** (Print Settings > Output options) - each is run with the path of a
    temporary G-code file after slicing/export, before the file goes to disk or a print host;
  - the **CLI**, `prusa-slicer-console.exe` (slice, `--export-3mf`, `--export-gcode`);
  - **physical printer upload** - PrusaSlicer speaks the OctoPrint, PrusaLink, PrusaConnect, Duet,
    Repetier, Klipper/Moonraker, FlashAir and AstroBox HTTP APIs. A print host the Addin Service
    impersonates would turn "Send to printer" into "send this G-code to PLM".
- There is nothing to hang a menu on, so the `.3mf` round trip (Open from PLM / Save to PLM /
  Check In / Revise) has to be driven from **outside** the app: the tray host, a file association,
  or a small companion window. Decide this with Marc before coding.
- **3MF metadata is not a safe home for attributes**: PrusaSlicer 2.9.0 drops every custom
  `<metadata preserve="1">` element from `3D/3dmodel.model` on save (prusa3d/PrusaSlicer#13920,
  closed as legacy). Whether foreign files in the archive's `Metadata/` folder survive a save has
  NOT been measured yet - measure before designing the attribute record.

## Rules that are not negotiable (inherited from every other Nexus add-in)

- **The service is the only thing this talks to.** `http://localhost:5100`; never the Engine,
  never the vault. Anything host-specific is declared by the add-in (`HOST_NAME`,
  `FILE_EXTENSIONS`) and travels with the request; the service keeps no list of hosts.
- **Uploads send the document's OWN path**, never a temp copy: `SaveRequest.FilePath` is one path
  the service both reads and records as `plm_file_path`. Marc: "it must get written to the
  staging directory."
- **Revise ups the revision in place** when the staged file is the open document.
- **Save As New offers only filled-in values that are not PLM's own** (part number, revision, the
  four stamps). The service merges the offer onto the new revision as-is.
- **No toast of our own where the service already toasts** (Sign Out).
- **Never commit to `main` or `next`.** Work on a `Marc/` branch; PRs to `next`; Marc merges.
- Every change adds a test. Tests run without PrusaSlicer present.

## Sibling add-ins to copy from

`Nexus.PLM.Inkscape.Addins` and `Nexus.PLM.Gimp.Addins` (Python; `nexusplm/{client,state,
identity,navigator}.py` are host-independent and were copied verbatim between them).
`Nexus.PLM.OrcaSlicer.Addins` is this repo's twin - the two slicers share the `.3mf` project format
and the same print-host upload APIs.
