# QFG5 reverse-engineering notes

These notes consolidate the archived QFG5 reverse-engineering posts listed in
[Sources](#sources), then compare their claims with the current source tree. The
posts are reverse-engineering observations rather than a formal specification:
unknown fields and tentative interpretations remain explicitly unknown here.

## Engine and resource context

- QFG5 is described as a 2.5D engine: 3D objects interact in rooms whose
  backgrounds are static images, with output rendered in 15-bit RGB while
  using floating-point calculations and paletted assets.
- Game logic is split between the main executable and dynamically loaded room
  modules (native Windows or Mac formats). The engine also contains hardcoded
  behavior and object-oriented room interactions. An asset extractor does not
  thereby implement the game engine or room logic.
- Data is stored in individual resource files and `.SPK` archives. The posts
  describe SPK as a ZIP-like container with a custom index/header and altered
  ZIP entry signatures/fields. The author observed no compression, but
  qualified that observation (“as far as I can tell”).
- Cutscenes are MOV files using Cinepak (the author notes that a particular
  copy may have been recompressed to SVQ1); audio is WAV using MS ADPCM.
- The resource-format inventory post names sixteen resource categories,
  IDs `0`–`15`; the engine overview's earlier “fifteen” count is inconsistent
  with that later explicit enumeration.

## File-format notes

### Complete resource category inventory

The resource-index IDs in the explicit enumeration are:

| ID | Type | Description in post |
| ---: | --- | --- |
| 0 | MDL | Model |
| 1 | ANM | Model animation |
| 2 | ROM | Room parameters |
| 3 | NOD | Room-background palette |
| 4 | IMG | Room-background image |
| 5 | ZZZ | Room-background depth buffer |
| 6 | GRA | Sprites |
| 7 | QGM | Messages |
| 8 | FTR | Room data |
| 9 | WAV | Effects and music |
| 10 | RGD | Region data |
| 11 | MOV | Intro and cutscenes |
| 12 | QGF | Fonts |
| 13 | STR | Some room data |
| 14 | AUD | Speech audio, still WAV format |
| 15 | SNC | Lipsync data |

The resource post says it will document the formats except AUD, MOV, and WAV.
Categories/extensions are reported by the reverse engineer; not every release
is guaranteed to use every category.

### SPK archive details

- Four known archives and tentative contents: `CDA.SPK` appears to contain
  speech and lipsync; `CDN.SPK` probably contains scene-specific data;
  `HDN.SPK` contains music, the world map, and other files; `HDNW.SPK`
  contains mostly 3D models/animations plus some QGF fonts.
- The archive starts with a resource catalog, followed by data in a modified
  ZIP layout. All described index fields are little-endian.
- Resource-category table: magic `0xFFAACCEE`, table size including 16-byte
  header (reported as 144), entry count (reported as 16), flags (usually
  `0x200` or `0x10200`), then pairs of 32-bit resource type and offset-to-type
  table.
- Resource-entry tables: magic `0xFFEEFFEE`, size 144, 16 entries, flags
  reported as `0x404643` (uncertain), then entries selected by category flags.
  For flags matching `0x0001xxxx`, entries are triplets of 32-bit resource ID,
  offset, and size. For `0x0000xxxx`, entries are a zero-terminated filename
  plus 32-bit offset and size.
- ZIP-like entry signatures are modified: local header `0x05034B50` instead
  of `0x04034B50`; central-directory record `0x03014B50` instead of
  `0x02014B50`; end record `0x07054B50` instead of `0x06054B50`. Entries are
  reported as stored uncompressed.

### Engine structure, loop, and GUI

- The project began as an attempt to reverse engineer and possibly recreate
  QFG5's hardcoded engine, with a stated aim to document formats and engine
  behavior for posterity. The author noted the lack of public format
  documentation and the substantial scope (roughly five megabytes of code).
- The engine was intended to run on little-endian PC and big-endian Mac; the
  PC version reportedly used OS/DirectX interfaces for event tracking,
  drawing, and audio. Do not assume all on-disk fields inherit host byte
  order.
- The engine has global manager-like objects, a main engine object with many
  callable methods, and dynamically loaded room modules. A room module creates
  a room object with roughly a dozen event-facing functions that may call back
  into engine functionality. Much game behavior is hardcoded in the engine
  and/or room module code rather than represented solely as data.
- Described main-loop order: compute elapsed milliseconds; update timer
  callbacks; update messages/speech; handle multiplayer events if enabled;
  play music/effects/speech; load a pending room; process main-window keys;
  update lighting; process sub-window keys; update map or room view (including
  debug display); handle mouse input and draw GUI/status.
- General scene draw order: cylindrical panorama; prepare scene objects;
  apply night effects (darkness and applicable blinking stars); render 2D
  sprites and 3D objects; draw effects. Effects include particles, translucent
  auras, and image-distorting warps. Hero, NPCs, stationary objects,
  projectiles, and spells share object-oriented machinery.
- Inter-object messages were designed partly for intended multiplayer.
  Messages are described as a 32-bit type plus type-dependent data. Multiplayer
  was disabled and its required network DLL was missing. Cutscenes may enqueue
  hardcoded commands.
- GUI is a stack of windows over the 3D world. Windows are screen rectangles
  with optional GRA background and widgets. The main screen routes keyboard
  and mouse input, redraws the active window and dispatches widget clicks;
  widgets may provide delayed tooltip text. Widget types include buttons
  (GRA art plus QGM caption rendered with QGF), images, text labels, radio
  buttons, text input, and model rendering. Most windows and behavior are
  hardcoded, though an engine interface reportedly permits room-created
  dialogue windows with up to four buttons.

### Room backgrounds and room-associated resources

- **`.NOD` palette:** starts with `QFG5` magic and a 32-bit file size. The
  engine observation is that it reads a 1024-byte RGBA palette at offset
  `0xA8`; those palette bytes are associated with the matching room image.
- **`.IMG` background:** header contains two 32-bit big-endian words and two
  64-bit big-endian floating-point values, followed by the same fields in
  little-endian form. The first values describe height and width; the floats
  are believed to describe room positioning and panorama angles, but some
  meanings were initially uncertain. Image pixels are palette indices
  compressed with RLE, laid out in columns from the right edge. The decoded
  image is transposed/rotated relative to an ordinary raster; the decoded
  height is doubled and the lower part is unused (possibly a depth-buffer
  remnant).
- **IMG RLE:** each line/column begins with a signed 8-bit count. Zero ends
  that line/column; a negative count copies the following `abs(count)` raw
  bytes; a positive count repeats the next byte that many times.
- **`.ZZZ` depth map:** headerless RLE depth values, where `0` is nearest and
  `255` farthest. It shares the IMG RLE scheme and dimensions, but is stored
  as lines from the right edge (mirrored relative to the background).
- **`.FTR` regions:** signed 16-bit values. First is the region count; each
  region has four words (tentatively depth, a zero, exit-portal flag, point
  count) followed by 16-bit point pairs.
- **`.ROM` room properties:** two integers and two floats. The first integer
  appears to be a big-endian version of the third field; the third is the
  number of additional room resources. Meanings of the second value and the
  floats are not established. The floats are nonzero only for room 200 in the
  observation.

### `.GRA` sprites and GUI imagery

- A GRA file begins with a 32-bit image coding mode, 32-bit sprite-collection
  count, a 512-byte RGB555 palette (256 two-byte entries), then 32-bit offsets
  to collections.
- Each collection begins with seven 32-bit values: x/y position, width,
  height, frame count, frame delay, and flags. Frame offsets follow and are
  relative to the collection data start.
- Modes described:
  - `0`: uncompressed indexed pixels.
  - `1`: uncompressed indexed pixels interleaved with depth bytes.
  - `2`: IMG-style RLE.
  - `3`: mode 2, with palette index `0xFF` transparent.
  - `4`: mode 2, with `0xFF` restoring the original background; this is
    compositing behavior, not simply the same as transparency.
- GRA is used for animated sprites, window backgrounds, GUI elements, and
  talking-character portraits. It is not limited to a single image per file.
- A January follow-up describes room/view/sprite number groupings: rooms may
  have multiple views, and views may have many sprites. Room-associated
  sprites are stored rotated, except map markers; GUI sprites are not rotated.
  These are useful naming/layout clues, not a complete GRA decoder spec.

### Fonts, messages, speech, and lipsync

- **`.QGF` font:** six 32-bit header values: maximum character width,
  character height, inter-character spacing, an unknown value, a complex
  pseudo-3D/simple-line flag, and a likely shadow flag. A 512-byte character
  width table follows (populated for character indices 16–256), then 512
  32-bit character-data offsets. Glyph data is byte pairs: a negative first
  byte skips pixels; a nonnegative first byte is opacity (for complex fonts,
  `31` is fully opaque; for simple fonts, any nonnegative value is opaque).
  The second byte appears ignored.
- **`.QGM` file header:** four-byte `" MGQ"` magic, 32-bit version, 32-bit
  message-block count, unknown 16-bit value, and 16-bit file ID. Versions 3
  and 4 were the versions observed as supported.
- **QGM block header:** four 16-bit message-ID values, four unknown 16-bit
  fields (the first may be speaker ID), 16-bit dialogue-option count,
  16-bit flags, another unknown 16-bit field, 16-bit internal message number,
  16-bit text length, and 16-bit label-presence flag. Flag value `4` indicates
  obfuscated contents. The header is followed, as indicated, by an optional
  13-byte label, dialogue-option links, and message text; an additional
  32-bit value follows the block and its meaning is unknown.
- QGM blocks may be ordinary text or dialogue-tree nodes. Options link to
  other messages rather than containing all branch text themselves. Preserve
  block IDs, options, labels, and trailing fields; internal message numbers
  are not necessarily ordered.
- **Message/audio/lipsync identifiers:** four 16-bit ID values are encoded in
  base 36 to form names of the form
  `A<3-char QGM ID><2-char ID1><2-char ID2>.<2-char ID3><1-char ID4>`.
  Matching speech uses the same name; lipsync uses `S` instead of the first
  `A`. Example IDs in the post link dialogue options to other blocks.
- **QGM text de-obfuscation:** apply only when the block's flag value includes
  `0x0004`. For each complete four-byte little-endian word, XOR with
  `0x0F1ACC1D`, rotate the 32-bit result left by 15, and write it back in
  little-endian order. Invert each byte in the final 0–3-byte tail. Do not
  confuse the flag with a file-version-wide switch.
- **Lipsync:** the update describes a series of 16-bit time positions
  (tertias, or 1/60 second) and sprite IDs, followed by another table of equal
  size whose meaning remains unknown.

### Models and animation

- **`.MDL` models:** observed IDs are `MIRT` (common textured format) and
  `8XOV` (a near variant with texture-related fields absent). Header fields
  include ID, header size, model ID, 16-byte ASCIIZ model name, mesh count,
  texture count (`MIRT`), unknown value, 1024-byte ABGR palette, texture-data
  offset (`MIRT`), then mesh offsets.
- Mesh records have a 16-byte part name, 20 floats of unknown purpose, vertex,
  texture-vertex and triangle counts, then offsets to vertex, UV, face, and
  unknown data. Vertices are float triplets; UVs are float pairs; faces contain
  three vertex indices, three UV indices, a texture number, and three unknown
  floats. The unknown per-vertex data consists of four signed integers, with
  the last reported as zero.
- `MIRT` texture data has texture offsets followed by six-field headers
  (float width/height, integer powers-of-two, integer masks) and indexed pixel
  data. For a 128×64 texture the reported values are `128.0`, `64.0`, `7`,
  `6`, `127`, `63`.
- **`.ANM` animation:** the same four-byte IDs appear, but animation files have
  a different layout; `8XOV` is more common there. See [`.ANM` animation
  records](#anm-animation-records) below for the later format description.
- Rendering observations are not a file-format guarantee: the engine maps
  room backgrounds to a virtual cylinder using precomputed column/scale tables
  and fixed-point values. The author says the panorama header floats denote
  start/end angles. Underwater distortion adds another table whose details
  were not investigated. The model renderer uses nonstandard projection
  behavior; the end post reports that ordinary OBJ viewers did not reproduce
  textures/polygons reliably. Treat renderer/export fidelity as unresolved.

### `.ANM` animation records

- Although animation and model files may both start with `8XOV` or `MIRT`,
  animation data has a different structure; the author describes one animation
  format, with `8XOV` more common for animations.
- Header: four-byte magic; 32-bit header size (reported as always 36); 16-bit
  animation name; 32-bit animation count; 32-bit block count per animation;
  32-bit frame delay (usually 33 or 66 milliseconds).
- Each animation block contains two 32-bit integers (observed as `1` and `0`),
  a three-float translation vector, and a nine-float rotation matrix.
- Sequences are believed to apply to model meshes, with each mesh carrying an
  animation ID; this relationship is an inference, not a fully established
  binding rule.

### Region data: `.RGD`

- RGD is 3D room-region/navigation geometry represented as a densely
  cross-referenced collection of arrays. The author did not identify every
  field or fully verify region classification.
- The described 32-bit header fields, in order, are: two constants (observed
  `0` and `2`); total region count; region-data offset; offset to a list of
  offsets whose targets are vector-index lists; an ignored offset; offset to
  region-positioning data; another region count; offset and count for a full
  region-ID list; data-start offset (usually `0x5C`); segment count and
  segment-data offset; point count and point-data offset; vector count and
  vector-data offset; a flag indicating whether subsequent fields are
  meaningful; special (possibly walkable) region count; offsets to two
  connectivity matrices; and offset to the special-region-ID list.
- Each region includes at least a 3D-vector index and an offset to segment
  IDs. Segment records include two point indices and an offset to region IDs;
  point data are pairs of doubles; vector data are triples of doubles. The
  connectivity matrices are square by the special-region count, with `-1` and
  `-2` indicating no connection.
- The later revisit describes the geometry as a flat map made of polygonal
  surfaces at different heights, with region edges of different types and a
  subset of marked walkable regions. One example had about 900 regions and 15
  marked walkable. It is used to map mouse coordinates to destination
  regions, plot paths, check enemy line-of-sight, and check projectiles. The
  height-related field and several region semantics remain unknown.
- Some data blocks include padding or unparsed bytes; `251.RGD` reportedly
  contains text such as “ctile Only. Region 448 contains entry point(s)”.
  Do not assume every block is a packed homogeneous array.

### Star points: `.STR`

- Reported only in 30 room subvariants. Format: 32-bit entry count followed
  by pairs of 32-bit integers interpreted as points. Despite the extension,
  they describe stars/shiny points in some room skies.

### Save games

- Save files begin with ASCII `"QG5P1.000 "` followed by 16-bit constant
  `0xDE89`, 16-bit inventory-item count XORed with `0xBEAD`, 16-bit random
  noise-byte count XORed with `0xFACE` (reported range 50–150), then 16-bit
  constant `0xA8A5`.
- Next is a 32-bit player type, the indicated noise bytes, and a 1096-byte
  player-state class dump. That dump is obfuscated by XORing each byte with
  the following byte; the post does not fully specify whether “following”
  means the following stored byte or original byte, nor the boundary
  treatment. The state includes characteristics and spell skills.
- Inventory follows, with the header's item count entries of four 32-bit
  values: sprite/item ID (in the `101xxx` range), amount, unique-item flag,
  and “applied zap spell on weapon” flag.
- Dialogue state: 32-bit count of rooms with dialogue state, then per room a
  room ID and message-state count, followed by two 32-bit flags per message
  state. Their meanings are tentatively “already said” and “no longer
  available”.
- Object state: object count, then data for each of 133 rooms: room object
  count and each object's data size, type, ID, plus object-specific bytes
  when size is nonzero.
- Then a 1536-byte engine-state block (puzzle flags, current time, bank state,
  etc.); hero-specific generic-object size and data; equipment consisting of
  nine belt item IDs, selected belt index, selected flag, and five equipped
  IDs (sword, shield, helmet, armor, amulet); map-state size and bytes; and
  current room ID plus room-data size and bytes. Room data is omitted when
  current room is `0` or `900` (global map).

### Rendering and projection

- The engine is described as a portable, software-only 2.5D renderer, not a
  conventional GPU pipeline. It combines projected room backgrounds, custom
  depth data, 3D meshes, and 2D sprites (including animated water).
- A mesh has independently transformable parts. Rendering includes triangle
  orientation/position calculation, back-surface pruning, rasterization into
  a color buffer with a separate Z-buffer, and texture-based pixel selection.
- There are opaque and blended rendering modes, each with variants. Paletted
  textures use precomputed shading lookup tables; some meshes use special
  tables. Glowing objects may be rendered in several passes, applying tables
  selectively and hiding some meshes between passes.
- Cylindrical panorama rendering uses precalculated per-column lookup,
  offset, and scale/step values in 16-bit fixed point. The image is transposed
  to simplify column processing. Panorama header angles indicate start/end
  angles and affect the distance from which the panorama is visible. Underwater
  distortion uses an additional, undocumented table.
- A later analysis reports model projection as differing from the usual
  `x / (z / k + 1)`: the `+1` is absent in the engine code. Standard OBJ
  exports/viewers did not reliably reproduce textures or some polygons. The
  exact rendering path is hard to infer because x87 decompilation can misstate
  operations and lose intermediate floating-point stack values; assembly or
  geometric corroboration is needed.

### Gameplay formulae (reported reverse-engineering observations)

These values describe gameplay code observed by the author and are not
independently validated against runtime or every character class.

- Derived values: maximum stamina = `(Agility + Vitality) / 2`; maximum mana =
  `(Intelligence + 2 * Magic) / 3`; maximum weight = `(Strength + 80) * 30`;
  spell strength = `(2 * Magic + spell_skill) / 15`.
- Attribute gains from use: damage raises Vitality by one-eighth damage;
  healing raises Vitality by one-eighth healing points (1000 for a healing
  potion, 100 for pills), halved while poisoned; spell casting raises
  Intelligence by one-eighth and Magic by one-quarter of mana spent; stamina
  use raises Vitality by one-eighth of points used. Mana appears to regenerate
  one point per five hours. Some event wins raise Intelligence by three times
  awarded score (ten times for network events).
- Overload appears to clamp excess weight to 384–1536 quarks, cost
  `excess_weight / 128` stamina, and raise Strength by 2. Swimming without an
  amulet costs 51 stamina and gives 1 Vitality.
- Rest uses nights (12 in-game hours): stamina recovery =
  `(Agility + Vitality) / 2 * nights * 8`; health recovery rate =
  `(Strength + 2 * Vitality) / 3`, with recovered HP equal to rate times
  nights; mana recovery = `(Intelligence + 2 * Magic) / 3 * nights`.
- Skill check procedure: require the skill; if no threshold is supplied,
  choose one randomly from 100–500; adjust skill for items and action bonus;
  use base points 400 for Agility/Defence/Magic/Stealth, 200 for
  Offence/Throwing/Swimming, and 100 otherwise; spend
  `(threshold * 3) / (base * 2) + 1` stamina except for Luck; for
  Magic/Climbing/Acrobatics/Throwing/Stealth/Lock-picking/Pickpocket/Swimming,
  add a random 0–50 if a Luck check passes; update skill-dependent values
  whether the check succeeds or not; pass when adjusted skill is at least the
  threshold.
- Reported skill-update amounts: Offence (+20 Strength, +12 Agility,
  +20 Offence); Defence (+20 Agility, +6 Intelligence, +30 Defence);
  Stealth (+20 Agility, +6 Intelligence, +40 Stealth); Climbing (+20 Strength,
  +15 Vitality, +40 Climbing); Swimming (+20 Strength, +15 Vitality,
  +40 Swimming); Acrobatics (+15 Strength, +25 Agility, +8 Vitality,
  +40 Acrobatics); Throwing (+30 Agility, +15 Strength, +40 Throwing);
  Lock-picking (+25 Agility, +10 Intelligence, +100 Lock-picking);
  Pickpocketing (+25 Agility, +10 Intelligence, +60 Pickpocketing);
  Magic (+20 Magic); Luck (+30 Luck); other skills and spell skills (+40).
- Skill updates are doubled; one twentieth of the doubled value adds to score.
  Throwing also subtracts one stamina; poison suppresses Vitality gain. The
  bonus accumulates in persistent skill XP; while XP is at least current skill
  value, subtract that value and add a random 0–5 to the skill. Negative XP
  subtracts a random 0–5. Skill values are generally limited to 100–500, with
  some class-specific caps of 550. Examples: Dead Parrot Inn dagger throw uses
  Throwing threshold 350; taking a hit updates Defence; reading a spell scroll
  checks Magic at threshold 100.

### Cut or partially implemented content

- The author found signs of content that was cut or unfinished, in addition to
  known censored Nawar dialogue, disabled multiplayer, and removed Glide
  spell. The engine recognizes about 30 spells; roughly ten more IDs may
  exist. Dragon Frost is an inferred blue-dragon/frost counterpart to Dragon
  Fire. Spell IDs beginning `101500` are ordinary spells, `101600` paladin
  abilities, and unknown attack-like IDs begin `101700`; a multiplayer
  connection is only a possibility.
- Model 98 “Sparkles”, roaches, and a blue dragon with 20–50 drachma loot are
  reported unused or unfamiliar assets. Wizard's Whirl has an alternative
  background and whirlpool-replacement lip sprites; little associated code
  was found beyond fixed whirl positions and possible hero/Erasmus
  interaction. The Whirlwind spell may have been intended as its prize. These
  are exploratory observations, not verified final design.

### Resource naming, room views, and sprite ranges

- A room can have multiple views and each view can have up to about 100
  associated sprites. Example: room 260, view 2600, torches 260000/260001,
  chests 260095/260096. Room 900's background is a standard BMP; points of
  interest are separate sprites.
- Room-associated sprites are generally stored rotated; map markers are an
  exception. GUI sprites in the `1xxxxx` range are generally not rotated.
- Reported sprite ranges: `100000–100006` main GUI; `100010–100181` portraits
  (excluding `100100`); `100100`, `100200–101001`, `101003` dialogue GUI;
  `101002` inventory/spell/paladin icons; `101101–101607` animated inventory
  views; `120000–130001` special-case GUI; `140000–140048` safe-cracking
  minigame; `143000–144005` Wizard's Whirl; `150000–155000` telescope
  content; `156000–157002` Scientific Isle pizza diagrams; `160000` save/load
  background; `161000` map (same visual as rotated `900099`); `199909–199913`
  Wheel of Fortune content, including 3D models. Ranges are reported examples,
  not a guaranteed exhaustive ID map.

### Filesystem portability

The author's independent Rust implementation notes that game paths are
case-insensitive in intent, while extracted filenames may use mixed case.
Portable tooling should resolve path components case-insensitively where
needed rather than relying on exact casing or assuming one host filesystem's
behavior.

## Sources

The original site was unavailable; all QFG5 links supplied for this update
were fetched through Wayback Machine:

- [A new project](https://web.archive.org/web/20251109002403/https://codecs.multimedia.cx/2023/11/a-new-project/)
- [QfG5 engine: a brief overview](https://web.archive.org/web/20251109010247/https://codecs.multimedia.cx/2023/11/qfg5-engine-a-brief-overview/)
- [QfG: SPK format](https://web.archive.org/web/20251109020441/https://codecs.multimedia.cx/2023/11/qfg-spk-format/)
- [QfG5 resource formats](https://web.archive.org/web/20251027212330/https://codecs.multimedia.cx/2023/11/qfg5-resource-formats/)
- [QfG5: room image formats](https://web.archive.org/web/20251028234600/https://codecs.multimedia.cx/2023/11/qfg5-room-image-formats/)
- [QfG5: messages](https://web.archive.org/web/20251027211754/https://codecs.multimedia.cx/2023/11/qfg5-messages/)
- [QfG5 GUI](https://web.archive.org/web/20251027211244/https://codecs.multimedia.cx/2023/11/qfg5-gui/)
- [QfG5: model format](https://web.archive.org/web/20251105212344/https://codecs.multimedia.cx/2023/12/qfg5-model-format/)
- [QfG5: some cut content](https://web.archive.org/web/20251027225949/https://codecs.multimedia.cx/2023/12/qfg5-some-cut-content/)
- [QfG5: leftover formats](https://web.archive.org/web/20251027220829/https://codecs.multimedia.cx/2023/12/qfg5-leftover-formats/)
- [QfG5 savegame format](https://web.archive.org/web/20251027222548/https://codecs.multimedia.cx/2024/01/qfg5-savegame-format/)
- [QfG5: some words about rendering](https://web.archive.org/web/20251105025236/https://codecs.multimedia.cx/2024/01/qfg5-some-words-about-rendering/)
- [QfG5: now with some code](https://web.archive.org/web/20251104232753/https://codecs.multimedia.cx/2024/01/qfg5-now-with-some-code/)
- [QfG5: panorama projection](https://web.archive.org/web/20251109015607/https://codecs.multimedia.cx/2024/02/qfg5-panorama-projection/)
- [QfG5: some notes about the design](https://web.archive.org/web/20251109012918/https://codecs.multimedia.cx/2024/02/qfg5-some-notes-about-the-design/)
- [QfG formulae](https://web.archive.org/web/20251104134103/https://codecs.multimedia.cx/2024/03/qfg-formulae/)
- [QfG5 RGD revisited](https://web.archive.org/web/20251027212507/https://codecs.multimedia.cx/2024/03/qfg5-rgd-revisited/)
- [QfG5: the end](https://web.archive.org/web/20251104103914/https://codecs.multimedia.cx/2024/03/qfg5-the-end/)

Where the author uses tentative wording (for example, unknown fields, ROM
semantics, SPK flags/compression, save-state XOR boundaries, and depth-buffer
remnants), it is retained rather than promoted to a confirmed specification.
