
# QFG5Extractor Comprehensive Architecture, UI/UX, & Performance Audit

---

## 1. Executive Summary

This audit assesses the architecture, UI system, startup pipeline, tab lifecycle, rendering mechanics, and resource management of **QFG5Extractor** (.NET Framework 4.5, WinForms, OpenTK 1.1, GDI+).

### Key Findings
1. **Startup Critical Path & Eager Instantiation**: [MainForm](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/MainForm.cs#L100-L259) eagerly instantiates all 10 `UserControl` tab classes during startup before the main window is shown. When a user has previously configured a `BaseDataPath` in `settings.cfg`:
   - [GraTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L40-L48) synchronously opens and scans every `.GRA` file in the game directory **4 consecutive times per file** to extract metadata and heuristics on the UI thread before the window renders.
   - [SaveTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/SaveTab.cs#L416-L505) spawns a background thread that queues hundreds of `this.Invoke` calls back onto the UI thread during startup, creating hundreds of unmanaged WinForms `Panel` and `PictureBox` control handles for the inventory palette.
   - [MdlViewer](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Viewers/Model/MdlViewer.cs#L88-L127) initializes OpenTK's `GLControl`, creates a native OpenGL context, loads `model_db.json`, and starts a `33ms` (~30 FPS) WinForms render timer that runs continuously even when the tab is hidden.
2. **UI System Inconsistencies**: Custom controls ([QFG5Button](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Button.cs), [QFG5PathField](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5PathField.cs), [QFG5ProgressBar](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5ProgressBar.cs), [QFG5PreviewFrame](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5PreviewFrame.cs)) lack cohesive sizing, typography, and state styling:
   - `QFG5Button` has no disabled rendering state, drawing identical graphics whether enabled or disabled.
   - Button text scaling and vertical centering collide with fixed margins and multiline text.
   - High-resolution textures (`qfg-background.png` at 2.8 MB, `qfg-button.png` at 1.2 MB) are rescaled via GDI+ `DrawImage` on every paint event without caching downsampled bitmaps.
   - `QFG5PreviewFrame` has an empty `OnPaint` method (`// Strokes removed for Phase 15 polish`), rendering as an invisible spacer rather than a themed visual frame.
3. **Threading & Re-entrancy Risk**: 9 occurrences of `Application.DoEvents()` are scattered across bulk extraction routines ([SpkTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Storage/SPK/SpkTab.cs#L169), [ModelTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Model/ModelTab.cs#L181), [PanoTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Panorama/PanoTab.cs#L171), [MsgTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Message/MsgTab.cs#L209), [GraTab](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L330)). These keep the UI responsive but introduce re-entrancy vulnerabilities (user clicking tabs, close buttons, or trigger actions mid-extraction).

---

## 2. Startup Critical Path

The startup execution sequence flows synchronously through [Program.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Program.cs#L10-L15) into [MainForm.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/MainForm.cs#L100-L259):

```
Program.Main()
 │
 ├──► 1. Application.EnableVisualStyles() + SetCompatibleTextRenderingDefault(false)
 │
 ├──► 2. MainForm Constructor Execution
 │     │
 │     ├──► a. SharedState.LoadSettings()
 │     │        └─ Synchronously reads and parses settings.cfg from disk
 │     │
 │     ├──► b. QFG5Theme Static Constructor
 │     │        ├─ Path discovery: checks localKit, cwdKit, parentKit, and devKit (up to 4 levels up)
 │     │        └─ Image.FromFile(): decodes 6 high-res images (button, background, frame, logos)
 │     │
 │     ├──► c. Form Geometry & Styles Setup
 │     │        └─ Double buffering, size bounds (1200x800), centering, black background
 │     │
 │     ├──► d. Bottom Logo Bar Creation
 │     │
 │     ├──► e. Eager Construction of 10 Tab Instances
 │     │        ├─ new SpkTab()           --> Scans directory for .spk if BaseDataPath configured
 │     │        ├─ new ModelTab()         --> Checks paths, builds layout & SplitContainer
 │     │        ├─ new PanoTab()          --> Checks paths, builds layout & SplitContainer
 │     │        ├─ new MsgTab()           --> Checks paths, builds layout
 │     │        ├─ new GraTab()           --> [BOTTLENECK] Iterates & reads all .GRA files 4x each
 │     │        ├─ new MdlViewer()        --> [BOTTLENECK] Loads OpenTK, builds GLControl, loads DB, starts 33ms timer
 │     │        ├─ new TextureEditorTab() --> Allocates 64x64 ARGB Bitmap, builds palette & tools
 │     │        ├─ new SaveTab()          --> [BOTTLENECK] Starts Task.Run, queues 100s of UI invocations for palette
 │     │        ├─ new FlagsTab()         --> Scans 10 hardcoded disk paths for flags.dat, refreshes grid
 │     │        └─ new SettingsTab()      --> Populates path text fields from SharedState
 │     │
 │     ├──► f. Inter-Tab Event Wiring
 │     │        ├─ SaveTab.SaveFileLoaded -> FlagsTab.LoadSave()
 │     │        └─ SharedState.OnSwitchTabRequested -> showTab()
 │     │
 │     ├──► g. Header Tab Ribbon Construction
 │     │        └─ Loops 10 pages: reads 10 icon files from disk, wraps in QFG5TabPanel + QFG5Button
 │     │
 │     ├──► h. showTab(0)
 │     │        └─ Inserts SpkTab into tabBody, activates ribbon highlight
 │     │
 │     └──► i. MainForm.Shown Event
 │              └─ Centers bottom logoBox
 │
 └──► 3. First Window Render & Message Loop
```

### Estimated Relative Weight of Startup Work
*(Derived from code paths, disk access volume, and control allocation patterns)*

```
Startup Execution
├── settings loading (Disk read: settings.cfg): ~2%
├── theme loading (6 image files decoded, ~6 MB raw PNGs): ~8%
├── MainForm chrome (geometry, logo bar, tab ribbon layout, 10 icon decodes): ~5%
└── tab construction: ~85%
    ├── GraTab (Synchronous quad-pass reading of all .GRA files if path set): ~35%
    ├── MdlViewer (OpenTK/OpenGL context initialization, model_db.json read): ~25%
    ├── SaveTab (Task.Run spawning hundreds of UI thread control allocations): ~10%
    ├── SpkTab (Synchronous Directory.GetFiles if path set): ~5%
    ├── FlagsTab (10 hardcoded file probes): ~3%
    └── Remaining tabs (Model, Pano, Msg, Texture, Settings): ~7%
```

---

## 3. Most Expensive Initialization Areas

1. **`GraTab.LoadFiles()` Quad-Read Loop ([GraTab.cs:L254-L300](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L254-L300))**:
   - If `BaseDataPath` is populated, `GraTab` runs `LoadFiles()` during construction.
   - For every `.GRA` file found in the directory, it executes:
     1. `converter.TryReadHeader()`: `File.OpenRead()`, reads 8 bytes.
     2. `converter.TryReadMetadata()`: `FileStream(Open, Read)`, reads 32 bytes.
     3. `converter.TryReadFlags()`: opens file again to inspect collection headers.
     4. `converter.AnalyzeHeuristics()`: opens file a fourth time to examine sprite compression modes.
   - In a standard game installation containing over 100 `.GRA` files, this performs **400+ synchronous file handle openings and reads on the UI thread** before `MainForm` appears.
2. **`MdlViewer` OpenGL Context Creation & 33ms Background Timer ([MdlViewer.cs:L88-L127](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Viewers/Model/MdlViewer.cs#L88-L127))**:
   - Initializing `GLControl(new GraphicsMode(32, 24, 0, 8))` loads native OpenGL drivers and creates a Win32 hardware rendering context.
   - Starts `spinTimer = new Timer { Interval = 33 }` immediately in the constructor. Even while the user is viewing `SpkTab`, this timer fires every 33ms on the main UI thread.
3. **`SaveTab` Inventory Palette Control Creation ([SaveTab.cs:L416-L505](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/SaveTab.cs#L416-L505))**:
   - `PopulatePalette()` starts a `Task.Run` background thread that loops over every game item in `ItemDatabase`.
   - For each item, it calls `this.Invoke()` to create a `Panel` and a `PictureBox` inside `visualGrid`.
   - Creating hundreds of native WinForms control handles in rapid succession saturates the UI thread message queue right at application startup.
4. **`FlagsTab` Multi-Path Probing ([FlagsTab.cs:L147-L173](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/FlagsTab.cs#L147-L173))**:
   - Probes 10 static paths (including external development paths like `/home/vandali/Desktop/...` and Wine prefixes) on the UI thread during constructor execution.

---

## 4. UI System Problems

### [QFG5Button.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Button.cs)
- **No Disabled State**: `OnPaint` does not check `this.Enabled`. When disabled (e.g., during extraction or when no save is loaded), buttons render with full opacity, idle background texture, and normal text colors. There is zero visual disabled feedback.
- **Forced Uppercase & Word Break Artifacts**: Lines 56-60 execute `this.Text.ToUpper()` with `TextFormatFlags.WordBreak`. On smaller buttons, multi-word labels break into two lines and clip vertically against the 28-30px fixed height.
- **No Text Offset on Click**: When `ModelType == ButtonState.Pressed`, the button switches to `ButtonPressed` texture, but text coordinates remain unchanged (`paddedRect`), reducing tactile press feedback.
- **Per-Paint GDI+ Allocations**: Creates new `SolidBrush` instances for hover/selected states on every paint pass instead of caching static brushes.

### [QFG5PathField.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5PathField.cs)
- **Multiline Layout Hack**: The internal `TextBox` has `Multiline = true` (line 36) solely to allow vertical expansion. This causes single-line path text to stick to the top edge rather than centering vertically.
- **Font Mismatch**: Hardcodes `Font = new Font("Arial", 12, FontStyle.Bold)`. Across tabs, neighboring labels and buttons use 8pt, 9pt, or 10pt fonts, causing noticeable scale dissonance.

### [QFG5ProgressBar.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5ProgressBar.cs)
- **Invisible Empty State**: When `Value == 0`, `OnPaint` does not draw any track, border, or background. The control is completely invisible until progress exceeds 0.
- **Flat Visual Fill**: Fills with a flat `SolidBrush(Color.Gold)` without shading, borders, or aesthetic framing to match the QFG5 bronze style.

### [QFG5PreviewFrame.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5PreviewFrame.cs)
- **Defunct Painting**: `OnPaint` contains only commented-out stroke code (`// Strokes removed for Phase 15 polish`). It functions as a generic container panel with 2px padding, providing none of the intended stone/wood framing.

### [QFG5Layouts.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Layouts.cs)
- **Hardcoded Column Widths**: `CreatePathRow` hardcodes `LabelWidth = 100`, `ButtonWidth = 90`, and `WideButtonWidth = 110`. Longer localized labels or multi-word actions ("EXTRACT ALL", "WIREFRAME3D") clip on non-standard font scalings.
- **ComboBox Wrapper Inconsistency**: [QFG5ComboBox](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Layouts.cs#L186-L220) also has its border strokes commented out, appearing as an unbordered dark rectangle next to themed buttons.

---

## 5. Global Fixes vs. Individual Fixes

| Problem Domain | Global Fix (Shared Controls / Core) | Individual Tab Fix (Specific UserControls) |
| :--- | :--- | :--- |
| **Disabled Button Feedback** | Add disabled rendering in [QFG5Button.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Button.cs) (dimmed texture or 50% alpha text). | None needed once updated centrally. |
| **Path Field Vertical Alignment** | Adjust padding and remove `Multiline = true` hack in [QFG5PathField.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5PathField.cs). | Adjust enclosing row heights where path fields are hosted. |
| **Progress Bar Track Visibility** | Draw dark recessed track rectangle in [QFG5ProgressBar.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5ProgressBar.cs) when `Value == 0`. | None needed once updated centrally. |
| **Preview Frame Borders** | Restore themed border painting in [QFG5PreviewFrame.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5PreviewFrame.cs). | None needed once updated centrally. |
| **Bitmap Scaling Performance** | Cache pre-scaled versions of `qfg-button.png` and `qfg-buttonClicked.png` in [QFG5Theme.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Theme.cs). | None needed once updated centrally. |
| **Font Consistency** | Standardize typography tokens in `QFG5Theme` (Title, Header, Body, Button, Caption). | Update tab-specific labels and grid headers to use standardized font tokens. |
| **Lazy Tab Initialization** | Implement deferred tab loading in [MainForm.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/MainForm.cs). | Ensure each tab exposes a clean `Initialize()` or `OnActivated()` lifecycle hook. |
| **Tab Re-entrancy During Long Tasks** | Centralize a busy/lock flag in [SharedState.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/SharedState.cs) to disable tab switching during background tasks. | Disable trigger buttons during operations in individual tabs. |
| **Missing Empty States & Placeholders** | Provide standard `CreateEmptyState()` helper in [QFG5Layouts.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Layouts.cs). | Add helpful placeholder labels inside empty listboxes and pictureboxes in individual tabs. |

---

## 6. Tab-by-Tab Audit

### Tab 1: SPK Archive ([SpkTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Storage/SPK/SpkTab.cs))
- **Purpose**: Scan, unpack, and repack proprietary `.SPK` game archive files.
- **Current Workflow**: Point to data folder -> click Scan (populates CheckedListBox) -> select destination folder -> select files ("All" / "None") -> click "Extract" or "Repack All".
- **UX Problems**:
  - Checkbox labels in the footer ("Hierarchy") are plain gray text with browser-default appearance.
  - "Repack All" button sits directly adjacent to selection buttons ("All", "None") without visual separation or confirmation prompt.
  - Extraction provides no cancel capability once initiated.
- **Layout Problems**:
  - The CheckedListBox is hosted inside a `QFG5PreviewFrame` that currently renders with no visible borders, making the black list box float abruptly on the dark background.
  - Bottom action bar combines selection utilities ("All", "None") and destructive operations ("Repack All") into a single row.
- **Performance Concerns**:
  - Synchronous `Directory.GetFiles()` on startup if `BaseDataPath` is set.
  - Uses `Application.DoEvents()` at lines 169 and 220 in the middle of extraction loops.
- **Potential Improvements**: Add confirmation dialog before repacking; provide cancel button during extraction; standardize footer action grouping.
- **Dependencies**: Writes `SharedState.BaseDataPath = outDir` upon successful standard extraction, which triggers path updates in other tabs.
- **Risk**: **Low**. Straightforward WinForms layout with minimal external UI dependencies.

---

### Tab 2: MDL Model ([ModelTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Model/ModelTab.cs))
- **Purpose**: Batch import/export 3D model resources (MDL to/from BMP textures and HAK geometry).
- **Current Workflow**: Select operation from ComboBox (e.g. "Export BMP (MDL -> BMP)") -> specify Input/Output folders -> click "RUN" -> preview converted file in list and picturebox.
- **UX Problems**:
  - "RUN" button is visually separated from options and placed on the first row next to the Operation dropdown, rather than following the natural INPUT -> OPTIONS -> ACTION flow.
  - Preview picture box shows static BMPs but lacks dimensions, bit depth, or metadata overlay.
- **Layout Problems**:
  - `advTable` splits Input and Output path rows equally, but secondary labels have muted gray colors that reduce readability against dark backgrounds.
  - `SplitContainer` ratio (35% list, 65% preview) can pinch the converted file list on smaller window widths.
- **Performance Concerns**:
  - Batch export runs synchronously on UI thread with `Application.DoEvents()` at line 181.
- **Potential Improvements**: Reorder controls to follow Input -> Operation -> Action hierarchy; add metadata readouts to the preview frame.
- **Dependencies**: Subscribes to `SharedState.OnBaseDataPathChanged` and `SharedState.OnExtractionPathChanged`.
- **Risk**: **Low**. Self-contained batch conversion page.

---

### Tab 3: Pano Image ([PanoTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Panorama/PanoTab.cs))
- **Purpose**: Decode and reconstruct 360-degree panoramic room backgrounds by pairing `.NOD` palette data with `.IMG` pixel data.
- **Current Workflow**: Point to NOD and IMG folders -> select output folder -> click "RUN" to extract panoramas or "INJECT" to repack modified bitmaps.
- **UX Problems**:
  - Three distinct path fields are stacked vertically (NOD Path, IMG Path, Output Path), consuming significant vertical real estate before the file list.
  - Preview box displays high-resolution panoramic images in `PictureBoxSizeMode.Zoom` with no panning or scrolling capability.
- **Layout Problems**:
  - Layout height allocation allocates 90px to the path stack, squeezing the preview pane on lower display resolutions (e.g., 1000x650).
- **Performance Concerns**:
  - Synchronous `Application.DoEvents()` at line 171 during batch unpacking.
- **Potential Improvements**: Provide interactive horizontal panoramic viewer; collapse dual input path fields when standard folder hierarchy is detected.
- **Dependencies**: Subscribes to `SharedState.OnBaseDataPathChanged` and `SharedState.OnExtractionPathChanged`.
- **Risk**: **Low**. Standard conversion page.

---

### Tab 4: Message ([MsgTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Message/MsgTab.cs))
- **Purpose**: Extract and repack binary `.QGM` dialogue strings and message tables.
- **Current Workflow**: Point to QGM folder -> set output directory -> click "Extract All" or "Repack All".
- **UX Problems**:
  - Extracted messages are dumped to disk, but the tab provides no in-app preview of strings, dialogue branches, or character lines.
  - Status footer gives minimal feedback during long extractions.
- **Layout Problems**:
  - Center area contains only a single `convertedListBox` inside an unbordered preview frame, leaving large portions of empty black space.
- **Performance Concerns**:
  - Synchronous loop with `Application.DoEvents()` at line 209.
- **Potential Improvements**: Add dual-pane string viewer (list of files on the left, searchable string table on the right); add progress percentage label.
- **Dependencies**: Subscribes to `SharedState.OnBaseDataPathChanged` and `SharedState.OnExtractionPathChanged`.
- **Risk**: **Low**. Clean, uncomplicated structure.

---

### Tab 5: GRA Sprite ([GraTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs))
- **Purpose**: Inspect, decode, preview, and export 2D animated sprites and UI assets from `.GRA` files.
- **Current Workflow**: Browse to GRA folder -> DataGridView lists sprite metadata (Dimensions, Offsets, Sprite count, Flags) -> select row to preview frames in PictureBox -> play animation -> choose PNG/MP4 batch export.
- **UX Problems**:
  - Selecting a row immediately loads and plays the animation, which can cause hitching on large sprite sheets.
  - Checkbox columns in the `DataGridView` ("PNG", "MP4") require click or shift-click, which is undiscoverable without prior instructions.
  - Animation controls (Play/Pause, speed) are absent from the preview frame.
- **Layout Problems**:
  - `DataGridView` headers use standard WinForms styling (`SystemColors.Control`) which clashes with the dark QFG5 theme.
  - SplitContainer defaults to 50% split, leaving preview small on the right.
- **Performance Concerns**:
  - **Severe Startup Bottleneck**: Synchronously reads every `.GRA` file 4 times during startup if `BaseDataPath` is set.
  - `Application.DoEvents()` occurs in 4 separate places (lines 330, 385, 442, 452).
- **Potential Improvements**: Defer directory scanning until tab is opened; add playback transport bar (play/pause/step/loop) beneath sprite preview; theme DataGridView headers to gold/dark bronze.
- **Dependencies**: Subscribes to `SharedState.OnBaseDataPathChanged` and `SharedState.OnExtractionPathChanged`.
- **Risk**: **High**. Complex `DataGridView` event wiring and heavy file parsing logic.

---

### Tab 6: 3D Viewer ([MdlViewer.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Viewers/Model/MdlViewer.cs))
- **Purpose**: Real-time 3D model viewport, skeletal animation player (.ANM), texture inspector, and GLB exporter.
- **Current Workflow**: Select MDL folder and ANM folder -> select model from list -> choose animation from ComboBox -> play animation / orbit / toggle render mode -> export PNG or GLB -> click "Edit Texture" to transfer to Texture Editor.
- **UX Problems**:
  - Floating vertical toolbar on the right of the viewport uses single-letter buttons ("R", "X", "Y", "S", "E", "A", "📷", "GLB") that rely entirely on tooltips for discoverability.
  - Animation frame indicator (`lblAnimFrame`) only updates during playback; scrubbing through individual frames is not supported.
  - Mode toggle button cycles through 3 states in a single button ("Wireframe3D", "Texture2D", "Textured3D") with no indicator of available alternatives.
- **Layout Problems**:
  - Sidebar width is hardcoded to 340px; viewport takes the remainder.
  - Top path management consumes two full rows (96px height) across both columns.
- **Performance Concerns**:
  - Continuous 33ms WinForms timer running even when off-screen.
  - In `GlControl_Paint`: loops through every polygon face and rebinds OpenGL textures via `GL.BindTexture` if face texture IDs differ.
- **Potential Improvements**: Stop/pause `spinTimer` when tab is not visible; add animation timeline scrubber; replace single-letter buttons with an integrated toolbar.
- **Dependencies**: Emits `SharedState.RequestEditTexture` and `SharedState.RequestSwitchTab(6)`; reads `model_db.json`.
- **Risk**: **High**. Contains OpenTK/OpenGL native lifecycle, context handling, matrix calculations, and animation playback state.

---

### Tab 7: Texture Edit ([TextureEditorTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/TextureEditorTab.cs))
- **Purpose**: Pixel art editor designed for modifying QFG5 textures with palette constraints and injecting them directly into `.MDL` files.
- **Current Workflow**: Receive texture from 3D Viewer or open file -> use pencil, eraser, fill, magic wand -> adjust hue/saturation -> click "Inject" to write back into original MDL.
- **UX Problems**:
  - "Inject" button is colored gold and placed in the top bar, but if the user opened a standalone BMP instead of arriving from an MDL, clicking "Inject" fails with an error.
  - Zoom control requires clicking tiny +/- buttons or dragging a trackbar; mouse wheel zoom is not supported on the canvas.
  - Drawing tools have no keyboard shortcuts (e.g. B for brush, E for eraser).
- **Layout Problems**:
  - The right-side tool panel (380px fixed width) has excessive vertical whitespace between sections ("DRAW", "SELECT", "ADJUST", "ZOOM", "COLOR").
  - Palette swatches at the bottom of the canvas are fixed at 25x25px with hardcoded primary colors, rather than extracting the active model's authentic 256-color game palette.
- **Performance Concerns**:
  - `Canvas_MouseMove` recalculates aspect ratio, scaling factors, and offsets on every single mouse move event.
  - `MagicWandSelect` and `SelectColorCluster` run flood-fill algorithms using `Bitmap.GetPixel` in a loop, which is notoriously slow in GDI+.
- **Potential Improvements**: Add mouse wheel zoom and pan; support loading the active MDL's exact 256-color palette; lock-bits optimization for flood fill.
- **Dependencies**: Subscribes to `SharedState.OnEditTextureRequested` and `SharedState.OnEditModelRequested`.
- **Risk**: **Medium**. Involves direct bitmap manipulation, GDI+ canvas painting, and binary MDL palette injection.

---

### Tab 8: Save Editor ([SaveTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/SaveTab.cs))
- **Purpose**: Inspect and modify hero save game files (`.QG5`), including inventory items, quantities, and reservoir offsets.
- **Current Workflow**: Browse/Load save file -> DataGridView lists inventory items -> drag-and-drop items from visual palette into grid -> edit amounts -> click "Save".
- **UX Problems**:
  - The item preview popup (`previewPopup`) appears on click as a floating box in the bottom corner with a tiny "Close" label.
  - Saving provides no backup creation prompt (`.bak`).
  - No character summary is displayed (Hero name, Class, Health, Stamina, Mana, Honor).
- **Layout Problems**:
  - SplitContainer splits 45% grid, 55% visual palette.
  - The visual palette loads hundreds of items categorized with gold section headers, requiring significant vertical scrolling.
- **Performance Concerns**:
  - `PopulatePalette()` queues hundreds of UI invocations during application startup.
- **Potential Improvements**: Defer inventory palette creation until tab is opened; add hero statistics panel; auto-create `.bak` file before saving.
- **Dependencies**: Fires `SaveFileLoaded(path)` event, which is wired to `FlagsTab.LoadSave()`.
- **Risk**: **High**. Contains custom binary save game parser and complex drag-and-drop grid interaction.

---

### Tab 9: Flags ([FlagsTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/FlagsTab.cs))
- **Purpose**: Inspect and toggle story/quest progression boolean flags within a loaded QFG5 save game.
- **Current Workflow**: Save file is automatically loaded via `SaveTab.SaveFileLoaded` -> DataGridView lists flag index, name, and checkbox.
- **UX Problems**:
  - Marked as "Flags (Beta)" in the tab ribbon, communicating an unfinished state.
  - Does not provide a standalone "Browse / Load" save button; it relies entirely on the user first loading a save in `SaveTab`.
  - No filter or search box to find specific flags (e.g., "Dragon", "Elsa", "Rite").
- **Layout Problems**:
  - Contains a single full-width DataGridView with empty space above and below.
- **Performance Concerns**:
  - Probes 10 hardcoded file paths during startup.
- **Potential Improvements**: Add flag search/filter field; add standalone Load/Save toolbar; add category grouping for quest rites.
- **Dependencies**: Receives save file path directly from `SaveTab.SaveFileLoaded`.
- **Risk**: **Low**. Standard DataGridView list view.

---

### Tab 10: Settings ([SettingsTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/SettingsTab.cs))
- **Purpose**: Configure global directories (Game Data, Extraction, Modded Files, Game Location) and launch the game.
- **Current Workflow**: Review/edit path fields -> click Save -> optionally click "Launch Game" or "Restore Backup".
- **UX Problems**:
  - Status feedback after clicking "Save" is missing (no success toast or status text).
  - "Launch Game" button does not indicate whether the executable path was verified.
- **Layout Problems**:
  - Form layout uses 30px padding with 45px rows, but lacks grouping containers or descriptions explaining what each path does.
- **Performance Concerns**: None.
- **Potential Improvements**: Add directory validation badges (green checkmark if directory exists and contains expected game files, red alert if missing); add clear path descriptions.
- **Dependencies**: Directly modifies `SharedState` properties and calls `SharedState.SaveSettings()`.
- **Risk**: **Low**. Simple form.

---

## 7. Resource & Memory Risks

```
Resource Leak & Memory Hotspots
├── GDI+ Bitmaps / Images
│   ├── QFG5Theme.ButtonIdle (1.2 MB PNG) ──► Re-scaled on every button paint
│   ├── QFG5Theme.WindowBg (2.8 MB PNG)   ──► Re-scaled across full screen on every paint
│   ├── MainForm.pbIcon.Image             ──► Image.FromFile() locks files on disk indefinitely
│   └── SaveTab itemImageCache            ──► Static dictionary holding item icons in memory
├── Unmanaged Control Handles
│   └── SaveTab visualGrid                ──► Creates 200+ Panel and PictureBox WinForms handles
├── Timers Running Unchecked
│   ├── MdlViewer.spinTimer (33ms)        ──► Never stopped when tab hidden or form idle
│   ├── SaveTab.previewTimer (100ms)      ──► Runs continuously during item hover
│   └── GraTab.animTimer (100ms)          ──► Active during sprite playback
└── OpenGL GPU Resources
    ├── MdlViewer.currentTextureIds       ──► Deleted on new model load, but not on control Dispose
    └── MdlViewer.fallbackTextureId       ──► Not deleted on form teardown
```

1. **Undisposed GDI+ Bitmaps from `Image.FromFile()`**:
   - `QFG5Theme` uses `Image.FromFile()` for all theme images. In GDI+, `Image.FromFile` locks the underlying file on disk for the lifetime of the process. If a user modifies or patches UI assets, an `IOException` occurs.
   - In `MainForm.cs:L218`, the 10 ribbon tab icons are loaded directly via `Image.FromFile(iconPath)`.
2. **WinForms Window Handle Exhaustion**:
   - In Windows, processes have a limit of 10,000 USER/GDI handles. In `SaveTab.cs`, creating separate `Panel` and `PictureBox` instances for every game item consumes over 400 window handles immediately.
3. **Missing `Dispose(bool)` Cleanup**:
   - [MdlViewer.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Viewers/Model/MdlViewer.cs) does not implement an override for `Dispose(bool disposing)`. When `MainForm` closes, the OpenGL textures allocated via `GL.GenTextures` are left to the driver to clean up, and `spinTimer` is not formally stopped.

---

## 8. OpenGL / Rendering Risks

1. **Immediate Mode Pipeline (`GL.Begin` / `GL.End`)**:
   - `MdlViewer` renders all 3D meshes using legacy immediate mode (`GL.Begin(PrimitiveType.Triangles)`).
   - In `GlControl_Paint` ([MdlViewer.cs:L1101-L1179](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Viewers/Model/MdlViewer.cs#L1101-L1179)), for each triangle face, it checks texture IDs and repeatedly calls `GL.End()`, `GL.BindTexture()`, and `GL.Begin(PrimitiveType.Triangles)`. On complex meshes with multiple materials, this causes heavy pipeline flushes.
2. **Per-Frame Dynamic Matrix Multiplications**:
   - During skeletal animation playback, bone matrices are multiplied per submesh node per frame on the CPU via `GL.MultMatrix(ref skel)`.
3. **Double Redraw on Tab Selection**:
   - When switching to the 3D Viewer tab, `glControl.HandleCreated` triggers, creating a `100ms` `startupTimer` to invalidate the control a second time (lines 354-365).
4. **Context Switching**:
   - The code properly checks `if (!glControl.Context.IsCurrent) glControl.MakeCurrent()`. However, calling `SetupViewport()` multiple times on every resize and visible change creates redundant matrix stack operations.

---

## 9. SharedState / Event Risks

```
SharedState Pub-Sub Event Network
─────────────────────────────────────────────────────────────────────────────
[SharedState.BaseDataPath]
    ├──► SpkTab.LoadSpkFiles()             (Scans SPK directory)
    ├──► ModelTab.UpdatePaths()            (Sets input to hdnw/mdl)
    ├──► PanoTab.UpdatePaths()             (Sets nod/img folders)
    ├──► MsgTab.UpdatePaths()              (Sets qgm folder)
    ├──► GraTab.LoadFiles()                (Quad-reads all .GRA files)
    └──► MdlViewer.LoadFilesInFolder()     (Reads model names)

[SharedState.ExtractionPath]
    ├──► ModelTab.outputFolderTextBox
    ├──► PanoTab.outputFolderTextBox
    ├──► MsgTab.outputFolderTextBox
    └──► GraTab.txtOutput

[Cross-Tab Direct Commands]
    ├──► MdlViewer ──► SharedState.RequestEditTexture() ──► TextureEditorTab
    ├──► MdlViewer ──► SharedState.RequestSwitchTab(6)  ──► MainForm.showTab(6)
    └──► SaveTab   ──► SaveTab.SaveFileLoaded           ──► FlagsTab.LoadSave()
```

### Architectural Fragilities to Preserve
- **Static Event Lifetime Leaks**: Because `SharedState` is a static class, subscriptions (`+=`) to its events hold strong references to the subscriber. Because tabs are allocated once in `MainForm` and kept alive, this does not currently cause memory leaks, but if tabs are ever recreated or disposed, failing to unhook (`-=`) will cause tab instances to leak in memory.
- **Recursive Event Cascades**: In `SpkTab.cs:L176`, completing an extraction sets `SharedState.BaseDataPath = outDir`. This triggers `OnBaseDataPathChanged` across all tabs simultaneously, causing `GraTab`, `ModelTab`, and `MdlViewer` to initiate directory scans concurrently while `SpkTab` is still finishing its UI update.

---

## 10. `Application.DoEvents()` Audit

All 9 occurrences of `Application.DoEvents()` were audited:

| File | Line | Context / Operation | Re-entrancy Risk | Recommendation |
| :--- | :--- | :--- | :--- | :--- |
| [SpkTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Storage/SPK/SpkTab.cs#L169) | 169 | SPK Extraction loop | **High**: User can click "Extract" or "Repack" again mid-operation, or switch tabs. | Keep for now; wrap with `runButton.Enabled = false` and tab freeze. Future: async/task worker. |
| [SpkTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Storage/SPK/SpkTab.cs#L220) | 220 | SPK Repack loop | **High**: Same re-entrancy risk as above. | Same as above. |
| [ModelTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Model/ModelTab.cs#L181) | 181 | MDL Batch Export/Import | **High**: User can change operation in ComboBox or re-click RUN. | Guard with busy flag. |
| [PanoTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Panorama/PanoTab.cs#L171) | 171 | Panorama Unpacking loop | **Medium**: File conversion loop. | Guard with busy flag. |
| [MsgTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Message/MsgTab.cs#L209) | 209 | Message Extraction loop | **Medium**: File conversion loop. | Guard with busy flag. |
| [GraTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L330) | 330 | Sprite Export (Single) | **Low**: Quick single file export. | Guard with busy flag. |
| [GraTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L385) | 385 | Sprite MP4 Conversion | **High**: Video encoding takes significant time; window clicks can trigger concurrent exports. | Guard with busy flag. |
| [GraTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L442) | 442 | Batch Sprite Export (PNG) | **High**: Long loop iterating all checked grid rows. | Guard with busy flag. |
| [GraTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs#L452) | 452 | Batch Sprite Export (MP4) | **High**: Extremely CPU-intensive batch loop. | Guard with busy flag. |

---

## 11. DPI & Resizing Risks

1. **Missing DPI Awareness Manifest**:
   - The application does not specify DPI awareness in an `app.manifest` or via `SetProcessDpiAwareness`.
   - On Windows 10/11 high-DPI displays (125%, 150%, 175%, 200%), the application is subject to GDI bitmap stretching, causing text and UI elements to appear soft or blurry.
2. **Fixed Pixel Coordinate Assumptions**:
   - Top tab ribbon allocates `10%` per column in `tabHeaders`. At the minimum supported window width (`1000px`), each tab gets `94px`. With fixed 56px icons and padding, button text ("Texture Edit", "Flags (Beta)") is forced onto multiple lines and clips.
3. **Fixed Splitter Ratios**:
   - `QFG5Layout.SetSplitterRatioOnce` sets splitter distances based on initial window size. When resizing the window from 1200px to 1600px, SplitContainers do not proportionately adjust unless resized manually.

---

## 12. Proposed Design System

To achieve visual consistency while preserving the vintage Quest for Glory V aesthetic:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ STANDARD QFG5 DESIGN SYSTEM TOKENS                                          │
├───────────────────────┬─────────────────────────────────────────────────────┤
│ Page Padding          │ 16px uniform outer padding                          │
│ Section Spacing       │ 12px vertical spacing between control groups        │
│ Standard Row Height   │ 36px (for path fields, action rows, input rows)     │
│ Button Dimensions     │ Compact: 80x32px | Standard: 110x32px | Wide: 140x32│
│ Label Sizing          │ Primary Label Width: 110px | Alignment: MiddleLeft  │
│ Input Field Height    │ 32px uniform (single-line, vertically centered)     │
│ Font System           │ - Window Title: Arial 14pt Bold (Gold #FFD700)      │
│                       │ - Section Header: Arial 10pt Bold (Gold #FFD700)    │
│                       │ - Primary Buttons: Arial 9pt Bold (Black / Gold)    │
│                       │ - Input & List Text: Arial 9pt Regular (White)      │
│                       │ - Captions & Hints: Arial 8pt Regular (Gray #A0A0A0)│
│ State Colors          │ - Idle Button: Bronze/Wood Textured                 │
│                       │ - Hover: Light Amber Overlay (80, 255, 252, 210)    │
│                       │ - Pressed: Dark Amber Texture + 1px text depress    │
│                       │ - Disabled: 40% Luminance Desaturated + Muted Gray  │
│ Container Frames      │ Recessed stone border with 1px dark inner shadow    │
│ Status Areas          │ Gold accent text, 24px height, bottom docked        │
└───────────────────────┴─────────────────────────────────────────────────────┘
```

---

## 13. Performance Improvement Plan

### A. Startup Optimization
- **Deferred / Lazy Tab Loading**: Replace eager instantiation of all 10 tabs with lazy instantiation on first tab click (e.g. `Func<UserControl>[] tabFactories`).
- **Deferred Filesystem Scanning**: Defer `LoadFiles()` in `GraTab` and `Directory.GetFiles()` in `SpkTab` until the user actively clicks on those tabs.
- **Single-Pass GRA Header Inspection**: In `GraConverter`, combine `TryReadHeader`, `TryReadMetadata`, `TryReadFlags`, and heuristic analysis into a single `FileStream` pass instead of opening the file 4 separate times.

### B. Runtime & Memory Optimization
- **Texture Cache for Theme Bitmaps**: Scale `qfg-button.png` and `qfg-buttonClicked.png` once to standard dimensions (80x32, 110x32, 140x32) at startup and cache the downsampled bitmaps.
- **Background Timer Gating**: Pause `MdlViewer.spinTimer` whenever `MdlViewer.Visible == false` or `activeTabIndex != 5`.
- **Virtual Inventory Palette**: Replace individual `Panel` and `PictureBox` controls in `SaveTab` with an owner-drawn control or virtual grid to eliminate handle consumption.

---

## 14. Implementation Roadmap

### Phase 0: Baseline Measurements & Telemetry
- Objective: Measure and log exact startup timings and per-tab instantiation costs without altering logic.
- Files affected: [Logger.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/Logger.cs), [MainForm.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/MainForm.cs).
- Risk: **Low**. Diagnostic only.

### Phase 1: Shared UI Foundation & Theming Fixes
- Objective: Fix central control issues in `QFG5Button`, `QFG5PathField`, `QFG5ProgressBar`, `QFG5PreviewFrame`.
- Files affected: `Core/UI/QFG5Button.cs`, `QFG5PathField.cs`, `QFG5ProgressBar.cs`, `QFG5PreviewFrame.cs`, `QFG5Theme.cs`.
- Risk: **Low**. Centralized fixes propagate across all tabs without altering tab code.

### Phase 2: Top Ribbon & Navigation Alignment
- Objective: Standardize tab header heights, icon spacing, and hover/selection halos.
- Files affected: `Core/MainForm.cs` (header generation).
- Risk: **Low**. UI layout only.

### Phase 3: Global Layout Consistency & Layout Helpers
- Objective: Enhance `QFG5Layouts.cs` with standard builders (`CreatePathRow`, `CreateStatusFooter`, `CreateActionRow`) and apply uniform 32px heights.
- Files affected: `Core/UI/QFG5Layouts.cs`.
- Risk: **Low**.

### Phase 4: Individual Tab UX Refinement
- Objective: Polish the workflow layout (INPUT -> OPTIONS -> ACTION -> RESULT) across individual tabs.
- Files affected: `SpkTab.cs`, `ModelTab.cs`, `PanoTab.cs`, `MsgTab.cs`, `GraTab.cs`, `MdlViewer.cs`, `TextureEditorTab.cs`, `SaveTab.cs`, `FlagsTab.cs`, `SettingsTab.cs`.
- Risk: **Medium**. Requires careful preservation of control names, event bindings, and UI hooks.

### Phase 5: Loading & Startup Performance (Lazy Tab Initialization)
- Objective: Switch `MainForm` to lazy tab allocation; optimize `GraConverter` single-pass reading.
- Files affected: `Core/MainForm.cs`, `Resources/Gra/GraConverter.cs`, `GraTab.cs`.
- Risk: **Medium**. Must preserve inter-tab communication (`SaveTab` -> `FlagsTab` and `MdlViewer` -> `TextureEditorTab`).

### Phase 6: Runtime Performance & Rendering
- Objective: Pause 3D viewer timer when inactive; pre-scale theme button images; reduce redundant matrix setup in OpenTK viewport.
- Files affected: `Viewers/Model/MdlViewer.cs`, `Core/UI/QFG5Theme.cs`.
- Risk: **Medium**. Must ensure OpenGL context remains valid when switching back to 3D Viewer.

### Phase 7: Accessibility & Usability
- Objective: Add keyboard shortcuts, tooltips, and tab-stop ordering.
- Files affected: All tab UserControls.
- Risk: **Low**.

### Phase 8: Final Visual Polish
- Objective: Validate color harmony, border alignments, and visual styling against original QFG5 game assets.
- Files affected: UI styles and theme assets.
- Risk: **Low**.

---

## 15. Test Plan

To verify that future UI/performance refactoring does not break core functionality:

| Feature / Format | Test Verification Procedure | Expected Result |
| :--- | :--- | :--- |
| **SPK Extraction** | Extract a retail `.SPK` archive (e.g., `hdnw/mdl.spk`) with "Hierarchy" checked. | Directory structure created, files unpacked matching original byte sizes. |
| **SPK Repacking** | Repack an extracted directory with one altered file into a new `.SPK`. | Archive compiles; game or extractor can re-read directory offset table. |
| **MDL Export / Import** | Export model textures to BMP, then import modified BMP back to MDL. | BMP exported with correct palette; re-imported MDL retains valid vertex count. |
| **ANM Animation** | Load an animated character (e.g. `HERO.MDL`) and load accompanying `.ANM` track. | Bones transform smoothly in viewport without mesh tearing or matrix scramble. |
| **GLB Export** | Export a multi-mesh model to `.GLB` format. | Exported `.GLB` loads cleanly in standard 3D viewer (Blender, Windows 3D Viewer). |
| **PANO Extraction** | Pair a `.NOD` file with a corresponding `.IMG` file and export panorama. | Produces seamless 360-degree BMP background with correct RGB555 colors. |
| **MSG Extraction** | Extract binary `.QGM` message file to text/JSON. | Generates readable string tables with accurate IDs. |
| **GRA Extraction & Playback**| Select an animated `.GRA` file, play frames in viewer, export to PNG/MP4. | Sprite frames animate at correct delay and export with transparent background. |
| **Texture Editor Injection**| Load texture from MDL, edit pixels, click "Inject", then reload in 3D Viewer. | Modified pixels reflect on the 3D model in real time. |
| **Save Game Editing** | Load `.QG5` save file, change gold amount or add inventory item, save file. | Save writes correctly; game or re-opened tool confirms updated inventory offset. |
| **Flags Editing** | Load save in `SaveTab`, switch to `FlagsTab`, toggle flag, save file. | Save reflects toggled boolean flag at offset `0x125B`. |
| **Path Synchronization** | Change `BaseDataPath` in `SettingsTab` or via `SpkTab`. | All tabs update their respective input directory paths automatically. |
| **Cross-Tab Handoff** | Click "Edit Texture" in `MdlViewer`. | Switches to Tab 6 (`TextureEditorTab`) with the selected texture loaded. |

---

*This concludes the engineering audit. No files were modified, created, or refactored. Awaiting your direction before proceeding to any phase of the implementation roadmap.*







30/09/26 - QFG5Extractor Performance Baseline & Diagnostic Instrumentation

# Phase 0 — QFG5Extractor Performance Baseline

## 1. Files Modified

Diagnostic timing stopwatches, logging hooks, and resource counters were added to the following files without altering application behavior, layouts, or file-format processing logic:

1. [src/Core/Logger.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/Logger.cs)
   - Added `StartupWatch` (`System.Diagnostics.Stopwatch`) to measure overall session time from process entry.
   - Added category logger helpers: `LogStartup()`, `LogPerf()`, and `LogResource()` emitting ISO-8601 millisecond timestamps and category tags (`[STARTUP]`, `[PERF]`, `[RESOURCE]`).
   - Added `CountControls()` helper to recursively enumerate control hierarchies (total controls, `PictureBox`, `Panel`, `Button`, `DataGridView`) without altering UI state.
   - Added `GetMemorySnapshot()` helper capturing `GC.GetTotalMemory(false)` and `Process.WorkingSet64`.

2. [src/Program.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Program.cs)
   - Added session startup marker `===== QFG5Extractor Performance Baseline START =====`.
   - Added build configuration detector (`#if DEBUG` / `#else`).
   - Added initial process memory snapshot logging.

3. [src/Core/UI/QFG5Theme.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/UI/QFG5Theme.cs)
   - Added stopwatches to measure UI kit path discovery and total theme initialization time.
   - Instrumented `LoadImage()` to measure per-image load duration, dimensions (`Width x Height`), pixel format, and approximate decoded bitmap memory size.

4. [src/Core/MainForm.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Core/MainForm.cs)
   - Added granular `Stopwatch` timers around `SharedState.LoadSettings()`.
   - Instrumented each of the 10 tab constructors individually with start/end millisecond logging (`Tab <Name>: START` / `Tab <Name>: END <ms> ms`).
   - Instrumented top navigation ribbon creation (duration, number of panels, buttons, icons loaded).
   - Instrumented `showTab(0)` invocation.
   - Added first visual paint timestamp via one-time check in `Form.Paint`.
   - Added `Form.Shown` event diagnostics, control tree count snapshots, active timer state inspection, and session end marker `===== QFG5Extractor Performance Baseline END =====`.

5. [src/Resources/Gra/GraTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Gra/GraTab.cs)
   - Instrumented `LoadFiles()` to record total `.GRA` files discovered, total files inspected, analysis duration, and average analysis duration per file.

6. [src/Resources/Save/SaveTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/SaveTab.cs)
   - Instrumented `PopulatePalette()` to measure background task duration and count items processed, `Panel` controls created, `PictureBox` controls created, and UI `Invoke` operations queued.

7. [src/Viewers/Model/MdlViewer.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Viewers/Model/MdlViewer.cs)
   - Instrumented constructor total, `model_db.json` load duration, and `InitializeComponent()` (including `GLControl` allocation).
   - Added timing around `glControl.Load` (OpenGL context initialization).
   - Added first GL paint timestamp in `GlControl_Paint`.
   - Logged `spinTimer` startup status and interval.

8. [src/Resources/Save/FlagsTab.cs](file:///run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/src/Resources/Save/FlagsTab.cs)
   - Instrumented `LoadFlagNames()` to measure path probe duration, count searched paths, and log the resolved file path.

---

## 2. Files NOT Modified

The following subsystems and logic were preserved with zero behavioral or architectural modifications:

- **Archive Logic**: `Storage/SPK/SpkConverter.cs`, `SpkTab.cs` extraction/repacking routines.
- **Model & Mesh Logic**: `Resources/Model/ModelConverter.cs`, `MeshLoader.cs`, `qfg5modelMesh.cs`, `qfg5modelMeshImport.cs`, `qfg5modelBitmap.cs`, `NormalGenerator.cs`, `MeshData.cs`, `OldMdlViewerLogic.cs`.
- **Animation & Skeletal Logic**: Bone transform matrices, `.ANM` keyframe loading, and skeletal vertex weighting.
- **Export Pipelines**: `Viewers/Model/GlbExporter.cs` and texture export routines.
- **2D Sprite / Pano Logic**: `Resources/Gra/GraConverter.cs`, `Resources/Panorama/PanoConverter.cs`, `PanoTab.cs`.
- **Message Logic**: `Resources/Message/MsgConverter.cs`, `MsgTab.cs`.
- **Save Game & Flags Logic**: `Resources/Save/SaveGameParser.cs`, `ItemDatabase.cs`.
- **OpenGL Viewport Rendering**: Pipeline calls (`GL.Begin`, `GL.End`, `GL.MultMatrix`, matrix stacks, polygon modes).
- **Core Architecture & Threading**: `SharedState.cs` event definitions and static pub-sub brokers.
- **Message Pump Calls**: All 9 existing `Application.DoEvents()` invocations remain untouched.

---

## 3. Build Result

- **Build Tool**: Mono `xbuild 14.0` / Mono C# Compiler (`mcs 6.12.0.199`)
- **Target Configuration**: `Debug|x86` (`.NET Framework 4.5`, `AllowUnsafeBlocks=true`)
- **Build Status**: **SUCCESS (0 Errors, 0 New Warnings)**
- **Compiler Warnings**: 14 pre-existing CS0219/CS0169/CS0414 warnings (unused local variables in legacy model decoders); no warnings introduced by diagnostic changes.

---

## 4. Runtime Test Result

The instrumented binary was executed in the workspace environment on X11 display `:0` across two scenarios:

- **TEST A**: Normal configured installation with real game data path populated:
  `BaseDataPath = /run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5 Data Extracted`
- **TEST B**: Unconfigured installation (`BaseDataPath` empty / first launch)

All timing measurements and control counts below are empirical readings extracted from the live session execution logs.

---

## 5. Startup Timing Table

| Stage | Test A (Configured Game Path) | Test B (Unconfigured / Empty Path) |
|---|---:|---:|
| **Program.Main to MainForm Constructor** | 620 ms | 388 ms |
| **MainForm Constructor Total** | **5,840 ms** | **751 ms** |
| ├── SharedState.LoadSettings | 5 ms | 2 ms |
| ├── QFG5Theme Initialization | 124 ms | 114 ms |
| ├── Tab: SpkTab | 67 ms | 48 ms |
| ├── Tab: ModelTab | 26 ms | 25 ms |
| ├── Tab: PanoTab | 6 ms | 4 ms |
| ├── Tab: MsgTab | 9 ms | 7 ms |
| ├── **Tab: GraTab** | **5,050 ms** *(BOTTLENECK)* | **34 ms** |
| ├── Tab: MdlViewer | 72 ms | 53 ms |
| ├── Tab: TextureEditorTab | 26 ms | 29 ms |
| ├── Tab: SaveTab | 47 ms | 43 ms |
| ├── Tab: FlagsTab | 305 ms | 294 ms |
| ├── Tab: SettingsTab | 3 ms | 3 ms |
| ├── Ribbon Construction | 34 ms | 30 ms |
| └── showTab(0) | 1 ms | 0 ms |
| **First Paint (Window Rendered)** | 6,592 ms | 1,263 ms |
| **MainForm.Shown Event** | 6,533 ms | 1,208 ms |
| **TOTAL STARTUP TIME TO USER INTERACTION** | **6,592 ms** | **1,263 ms** |

---

## 6. Detailed Hotspot Measurements

### GRA (`GraTab`)
- **Files Discovered**: 996 `.GRA` files in `cdn/GRA/`
- **Files Inspected**: 996 files
- **Analysis Duration**: **4,990 ms**
- **Average Per File**: **5.01 ms / file**
- **Additional Observations**: In Test A, `GraTab` alone accounted for **86.5% of total MainForm constructor time** (5,050 ms out of 5,840 ms). The constructor executes before the window handle is created, blocking the UI thread for ~5 seconds while reading metadata and headers across 996 binary files. In Test B (no path set), it completed in 34 ms.

### SaveTab (`SaveTab`)
- **Items Processed**: 174 items (loaded from `objects.dat`)
- **Panels Created**: 174 `Panel` instances
- **PictureBoxes Created**: 174 `PictureBox` instances
- **Invokes Queued**: 179 UI thread `Invoke` operations (4 category header invokes, 174 item control invokes, 1 completion invoke)
- **Initialization Duration**: Asynchronous `Task.Run` triggered from constructor; runs concurrently in background after UI displays.
- **Additional Observations**: Although the constructor returns quickly (47 ms), `SaveTab` creates **348 separate WinForms native controls** (`Panel` + `PictureBox`) via 179 sequential UI invocations.

### MdlViewer (`MdlViewer`)
- **Constructor Total**: 69–72 ms
- **GLControl Creation**: 44–60 ms (inside `InitializeComponent`)
- **model_db.json Loading**: 1 ms
- **OpenGL Context Init (`glControl.Load`)**: 0–2 ms (deferred by driver until first render pass)
- **Timers**: `spinTimer` starts immediately in constructor with `interval=33 ms` and `enabled=True`.
- **First GL Paint**: 23,698 ms (fires only when user switches to tab 5 or off-screen handle renders).

### Theme (`QFG5Theme`)
- **Path Discovery**: 2 ms (resolved to `/run/media/sarihassoun/5C2B03407CC398CA/Development/Projects/QFG5-Tool/QFG5Extractor/UI Kit`)
- **Total Initialization**: 114–124 ms
- **Image Count**: 6 images decoded (1 missing: `qfg-gameLogo.png`)
- **Decoded Bitmaps Measured**:
  - `qfg-background.png`: 42–45 ms (1688x1008, 32bpp Argb, **~6,646 KB decoded**)
  - `qfg-previewBox.jpg`: 17–19 ms (1728x1152, 24bpp Rgb, **~5,832 KB decoded**)
  - `qfg-button.png`: 17–18 ms (1376x480, 24bpp Rgb, **~1,935 KB decoded**)
  - `qfg-buttonClicked.png`: 14–16 ms (1376x480, 24bpp Rgb, **~1,935 KB decoded**)
  - `qfg-frame.png`: 4–5 ms (96x1008, 32bpp Argb, **~378 KB decoded**)
  - `qfg-toolkitLogo.png`: 2 ms (253x120, 32bpp Argb, **~118 KB decoded**)
- **Additional Observations**: Over **18.8 MB** of uncompressed bitmap memory is allocated during static theme construction before any forms are drawn.

### FlagsTab (`FlagsTab`)
- **Path Probing**: 290–305 ms
- **Probed Paths**: 7 paths probed out of 10 configured paths before resolving `../../Resources/QG5Edit/flags.dat`.
- **Flags Loaded**: 727 flag names parsed into grid.

---

## 7. Resource Snapshot

| Stage | Total Controls | PictureBoxes | Panels | DataGridViews | Buttons | Approx. Memory (GC / WorkingSet) |
|---|---:|---:|---:|---:|---:|---:|
| **Initial Process** | 0 | 0 | 0 | 0 | 0 | 4 MB / 18 MB |
| **After Theme Init** | 0 | 0 | 0 | 0 | 0 | 4 MB / 60 MB |
| **After Tabs Init (10 Tabs)** | **352** | 5 | 122 | 3 | 78 | 8–11 MB / 74–77 MB |
| **After Ribbon** | 352 | 5 | 122 | 3 | 78 | 8–11 MB / 74–77 MB |
| **After showTab(0)** | 352 | 5 | 122 | 3 | 78 | 8–11 MB / 74–77 MB |
| **After MainForm Shown** | **58** *(Active Tree)* | 11 | 17 | 0 | 17 | 8–11 MB / 74–78 MB |

*(Note: "After Tabs Init" reflects all 10 instantiated UserControls in memory. "MainForm Shown" counts controls currently mounted in the active `MainForm` visual tree with Tab 0 displayed).*

---

## 8. Active Timers

Immediately following application startup, the following timer states were measured:

1. **`MdlViewer.spinTimer`**:
   - Interval: **33 ms** (~30 FPS)
   - Enabled: **`True`** *(Confirmed running continuously in background even though MdlViewer is not visible).*
2. **`SaveTab.previewTimer`**:
   - Interval: 100 ms
   - Enabled: `False` (activates on hover)
3. **`GraTab.animTimer`**:
   - Interval: 100 ms
   - Enabled: `False` (activates on sprite play)

---

## 9. Top Measured Bottlenecks

1. **`GraTab` Startup Scan (`LoadFiles`)**: **5,050 ms**
   - Single largest delay in the application. When a valid game directory is configured, scanning and inspecting 996 `.GRA` files blocks the UI thread for over 5 seconds during `new GraTab()`.
2. **`FlagsTab` Multi-Path Probing (`LoadFlagNames`)**: **290–305 ms**
   - Probing 7 disk paths sequentially on startup introduces an unnecessary ~300 ms freeze.
3. **Theme Bitmap Decoding (`QFG5Theme`)**: **114–124 ms**
   - Synchronously decoding high-resolution theme PNGs/JPGs adds ~120 ms and 18+ MB of memory overhead.
4. **`MdlViewer` Background Turntable Timer (`spinTimer`)**:
   - Fires every 33 ms on the main UI thread throughout the application's entire execution lifecycle, regardless of which tab is active.

---

## 10. Audit Finding Corrections

| Audit Claim | Measured Result | Evaluation |
|---|---|---|
| *"MainForm eagerly constructs all 10 tabs."* | Confirmed | Eagerly creates all 10 `UserControl` instances (352 controls) during `MainForm` constructor. |
| *"GraTab is a severe startup bottleneck."* | Confirmed (5,050 ms) | When path is configured, it scans 996 files and consumes 86% of constructor time. |
| *"SaveTab blocks UI thread during startup."* | Partially Confirmed | `SaveTab` constructor itself is fast (47 ms) because it offloads to `Task.Run`, but it queues 179 `Invoke` calls creating 348 controls shortly after startup. |
| *"MdlViewer GLControl creation is expensive."* | Partially Confirmed (44–60 ms) | Total constructor is ~70 ms; noticeable, but minor compared to `GraTab`. |
| *"MdlViewer 33ms timer runs in the background."* | Confirmed | `spinTimer` starts at startup with `enabled=True` and ticks continuously at 33 ms. |
| *"FlagsTab probes disk paths during startup."* | Confirmed (290–305 ms) | Probes 7 paths sequentially before finding `flags.dat`. |
| *"Theme image decoding contributes to startup delay."* | Confirmed (114–124 ms) | Loads 6 images (~18.8 MB decoded bitmap buffer) synchronously. |

---

## 11. Recommended Next Step

Based strictly on these empirical baseline measurements:

1. **Phase 1 (Shared UI Foundation)** should proceed next to resolve the button rendering, progress bar, preview frame, and text alignment issues in the central controls without touching tab logic.
2. When reaching **Phase 5 (Startup Performance)**, the priority sequence must be:
   - **Deferred `GraTab` scanning**: Moving `LoadFiles` from `GraTab` constructor to first tab activation will immediately eliminate **~5.05 seconds (77%)** of configured application startup time.
   - **Gating `MdlViewer.spinTimer`**: Stopping `spinTimer` when `MdlViewer` is not visible will eliminate redundant 30 FPS tick events on the UI thread.
   - **Path caching in `FlagsTab`**: Resolving `flags.dat` relative to `BaseDataPath` or executable directory will save ~300 ms.
