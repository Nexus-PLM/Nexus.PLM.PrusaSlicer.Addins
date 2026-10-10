# Nexus PLM for PrusaSlicer

Product lifecycle management for PrusaSlicer: the print project (`.3mf`) and its G-code tracked in and
out of Nexus PLM - the same command set the Nexus PLM add-ins give Word, LibreOffice, OpenOffice,
ONLYOFFICE, Inkscape and GIMP.

**Status: scaffold.** The integration route is being decided; see `CLAUDE.md` for what was
measured about PrusaSlicer before any code was written.

## What this talks to

Only the Nexus PLM **Addin Service** on `http://localhost:5100`, hosted by the Nexus PLM tray
application (`Nexus.PLM.WPF.Addins`). Never the Engine or the vault directly.

## Licence

MIT - see `LICENSE`.


## Server-side translators (the repo's first shipped code)

`Nexus.PLM.PrusaSlicer.Translators` ships this repo's example conversions for the Translation
server, deployed to `Nexus\Translators\prusaslicer\` - the rollout's first MIXED plugin
folder: two C# translators and a `translators.json` script entry side by side.

| Key | Conversion | Vehicle | Needs on the server |
|---|---|---|---|
| `3mf_to_stl` | 3MF → binary STL | C#, pure .NET | nothing |
| `3mf_to_png` | 3MF → embedded preview PNG | C#, pure .NET (unzip) | nothing |
| `3mf_to_gcode` | 3MF project → G-code | manifest, console slicer | PrusaSlicer + `PRUSASLICER_CMD` |

A `v*` tag attaches `Nexus.PLM.PrusaSlicer.Translators.zip` (publish output + manifest) to the
GitHub release; keys are reserved in `Nexus.PLM.Services/docs/translator-ownership.md`
(OrcaSlicer owns 3mf→obj/glb and gcode→png).
