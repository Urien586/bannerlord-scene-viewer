# File formats

Worked out from the files themselves and checked on two multiplayer scenes. Read-only use is verified; nothing here has been tested for writing files the game accepts.

All numbers are little-endian unless noted.

## navmesh.bin

Container:

| Offset | Type | Meaning |
|---|---|---|
| 0 | char[4] | `RNM1` |
| 4 | u32 | file size - 8 |
| 8 | u64 | version (3) |
| 16 | u64 | uncompressed size |
| 24 | u64 | compressed size (file size - 8) |
| 32 | u64 | 0 |
| 40 | 8 bytes | unknown |
| 48 | ... | one LZ4 block (no frame) |

Uncompressed body:

- `NMG9`
- u32 vertex count, then per vertex `f32 x, y, z` (z is height)
- u32 edge count, then per edge 24 bytes: `i32 -1, v0, v1, -1, -1, 0`
- u32 face count, then per face:
  - u32 `n` (3 or 4)
  - `n` × u32 vertex index
  - `n` × u32 edge index (`e[k]` is the edge `v[k]`–`v[k+1]`)
  - i32 (always 2), i32 `b`, i32 face id, i32 (0), i32 flags, u8 `d`
- tail: u32 32, then 256 zero bytes

Two faces that list the same edge index are neighbours. Connected groups of faces are the "islands" the viewer shows.

## terrain.bin

Header: `ZGR6RTRN`, u32 version (2), then a table of parts, 20 bytes each, up to offset 0x70:

| Field | Type |
|---|---|
| tag | char[4]: `MIDX`, `HGHT`, `NRML`, `WGHT`, `PHYM` |
| count | u32 |
| offset | u32 |
| size | u32 |
| uncompressed size | u32 |

Each part at `offset`: a table of u32 offsets (one per terrain node, into the uncompressed data), then a 40-byte header (u64 3, u64 uncompressed size, u64 compressed size, ...), then one LZ4 block that ends at header start + compressed size.

The node grid, node size, and `min_height`/`max_height` come from the `<terrain>` element in `scene.xscene`. Nodes are stored y-outer, x-inner. In every node image, the PNG row is world y and the column is world x.

- **HGHT**: per node, u32 length + 16-bit greyscale PNG (129×129 in the tested scenes). Height = `min_height + value / 65535 * (max_height - min_height)`. Checked against the navmesh: median difference 0.05 m.
- **WGHT**: per node, 16 slots, one per terrain layer in `<layers>` order. Each slot is u32 present; if 1, then u32 length + 8-bit greyscale PNG with that layer's weight.
- **NRML**: per node, u32 length + RGBA PNG (normals; the viewer computes shading from heights instead).
- **MIDX**: four u32-length-prefixed 4096×4096 greyscale PNGs (likely the season masks).
- **PHYM**: not decoded.

Each layer's textures (`diffusemap`, `normalmap`...) are named in `scene.xscene` under `<layers><layer><summer><textures>`.
