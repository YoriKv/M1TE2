# M1TE file formats

M1TE works with two on-disk formats:

1. **`.M1` session files** — the editor's own project format (palette + 3 background
   maps + all tilesets). Versioned.
2. **`.map` raw tilemap files** — a single background layer as a raw array of SNES
   tilemap entries, in hardware VRAM (screen-block) order. This is the format the
   SNES PPU reads and that carts (e.g. Yoshi's Island) store after decompression.

All multi-byte values are little-endian. A "tile cell" is one 16-bit SNES tilemap
entry. Maps are addressed as `(x, y)` with `x` = column (0 = left), `y` = row
(0 = top).

---

## 1. The tilemap entry (2 bytes per cell)

Both formats encode each map cell as the standard 16-bit SNES tilemap word
`vhopppcc cccccccc`:

```
byte 0 (low):   c c c c c c c c      tile index bits 7..0
byte 1 (high):  v h o p p p c c
                │ │ │ └─┴─┴──────── tile index bits 9..8  (10-bit tile, 0..1023)
                │ │ │   p p p ────── palette (BG CGRAM row, 0..7)
                │ │ o ────────────── priority bit
                │ h ──────────────── horizontal flip
                v ────────────────── vertical flip
```

---

## 2. Map dimensions and internal model

A map can be **32 or 64 tiles wide** (`map_width`) and **1..64 tiles tall**
(`map_height`). On real SNES hardware these correspond to the BG size bits in
`BGxSC` ($2107-$210A):

| width × height | BGxSC size bits | screens |
|---|---|---|
| 32 × 32 | `00` | SC0 |
| 64 × 32 | `01` | SC0 (left), SC1 (right) |
| 32 × 64 | `10` | SC0 (top),  SC1 (bottom) |
| 64 × 64 | `11` | SC0 (TL), SC1 (TR), SC2 (BL), SC3 (BR) |

Internally M1TE stores every layer in a fixed **64-wide × 64-tall** array
(stride 64): `index = layer * 4096 + y * 64 + x`. A 32-wide map simply uses
columns 0..31 of each row; the unused margin is never displayed or saved.

---

## 3. `.map` raw tilemap files — hardware screen-block order

A `.map` is a raw array of tilemap entries (no header) for **one** background
layer, `map_width * map_height` cells, `2` bytes each. The bytes are laid out in
**SNES "screen-block" order**: the map is divided into 32×32-tile *screens* stored
in the order **SC0, SC1, SC2, SC3 = TL, TR, BL, BR**

```
screenIndex = screenX + screenY * (map_width / 32)      // screenX varies fastest
```

and **each screen is stored row-major, 32 rows × 32 columns** (`0x800` bytes per
full screen). This is exactly how the PPU addresses a tilemap and how the data
sits in VRAM after a wholesale DMA.

Consequences per shape:

- **32 × 32** → 1 screen → plain row-major, `0x800` (2048) bytes.
- **32 × 64** (e.g. YI in-level BG2, "64 tall") → SC0 (top 32 rows) then SC1
  (bottom 32 rows). Because the width is 32, this is **byte-identical to plain
  row-major** over 64 rows. `0x1000` (4096) bytes.
- **64 × 32** (e.g. YI overworld terrain, "64 wide") → SC0 = the **entire left
  32×32 block**, then SC1 = the **entire right 32×32 block**. This is **NOT** plain
  64-wide row-major. `0x1000` (4096) bytes.
- **64 × 64** → TL, TR, BL, BR, each a full 32×32 screen. `0x2000` (8192) bytes.

> The 64-wide case is the one that differs from a naïve row-major dump. M1TE's
> `PackScreenBlock` / `UnpackScreenBlock` (in `MenuClicks.cs`) implement this
> ordering; verified byte-exact against the Yoshi's Island overworld BG1 tilemap
> (cart file `$7C`, VRAM byte `$0000`).

### Reading / writing `.map`

A `.map` carries no dimensions, so **set Map Width and Map Height in the editor
before loading or saving** a raw map. On load, M1TE clears the target layer and
unpacks `map_width × map_height` cells in screen-block order; if the file is
shorter, the remainder stays blank; if longer, the excess is ignored. The active
layer is chosen by the current BG view (BG1/BG2/BG3).

`Save a Map (W x H)` and `Save a Map (W x Height)` both emit the active layer at
the current `map_width × map_height` in screen-block order. They can also write an
RLE-compressed `.rle` (LC_LZ2 / "lz2") variant.

The "load a map to selected Y" variants insert a `map_width`-wide, **row-major**
strip starting at the selected row (an append/stitch tool); they do not screen-
block-reorder.

---

## 4. `.M1` session files

The session file stores the whole editing state: a 16-byte header, the 128-colour
palette, the three background maps, and the eight tilesets (four 4bpp + four 2bpp).

### 4.1 Header (16 bytes)

| Offset | Size | Field | Notes |
|---|---|---|---|
| 0 | 2 | magic `"M1"` (`0x4D 0x31`) | |
| 2 | 1 | version | `1` = legacy, `2` = variable width/height |
| 3 | 1 | # palettes | `1` |
| 4 | 1 | # maps | `3` |
| 5 | 1 | # 4bpp tilesets | `4` |
| 6 | 1 | # 2bpp tilesets | `4` |
| 7 | 1 | `map_height` | 1..32 (v1) / 1..64 (v2) |
| 8 | 1 | `tilesize` | 0 = 8×8, 1 = 16×16 |
| **9** | 1 | **`map_width`** | **v2 only** (32 or 64); absent/`0` in v1 ⇒ treated as 32 |
| 10..15 | 6 | reserved (`0`) | |

### 4.2 Body

After the header:

```
palette        256 bytes   128 colours × 2 (15-bit BGR555, little-endian)
maps           see below   3 background layers, 2 bytes/cell
4bpp tilesets  32768 bytes  4 sets × 256 tiles × 32 bytes (4bpp planar 8×8)
2bpp tilesets  16384 bytes  4 sets × 256 tiles × 16 bytes (2bpp planar 8×8)
```

The **maps** section differs by version:

- **v1 (legacy):** three layers, each a packed **32 × 32** grid, row-major, 2
  bytes/cell → `3 × 0x800` = **6144 bytes**. Total file = **55568 bytes**.
- **v2:** the three internal **64 × 64** layer arrays written verbatim (stride 64),
  row-major, 2 bytes/cell → `3 × 4096 × 2` = **24576 bytes**. Total file =
  **74000 bytes**. The active region is bounded by the header's `map_width` /
  `map_height`; cells outside it are saved as zero.

> Note: the `.M1` map section is **plain row-major at stride 64**, not screen-block.
> Screen-block ordering applies only to the hardware-facing `.map` export. The
> `.M1` format is M1TE's lossless internal representation.

### 4.3 Backward compatibility

- Loading is decided by file size: **55568 ⇒ v1**, **74000 ⇒ v2**. Any other size
  is rejected.
- A v1 file loads into the stride-64 model at width 32 (its 32×32 layers are placed
  into columns 0..31), `map_height` clamped to 1..32.
- Saving always writes **v2** (74000 bytes). Old M1TE builds that hard-expect 55568
  bytes will not read a v2 file; re-export at 32×32 from this build if a legacy file
  is required.

---

## 5. Worked example — Yoshi's Island assets

| Asset | width × height | `.map` size | `.map` layout |
|---|---|---|---|
| In-level BG2 ("64 tall") | 32 × 64 | `0x1000` (4096 B) | SC0 top, SC1 bottom = plain row-major |
| Overworld terrain ("64 wide") | 64 × 32 | `0x1000` (4096 B) | SC0 left block, SC1 right block |

To edit the overworld terrain: set Map Width = 64, Map Height = 32, pick the BG
layer, `Load a Map`, edit, then `Save a Map (W x H)`. The bytes round-trip exactly
to the cart's screen-block layout.
