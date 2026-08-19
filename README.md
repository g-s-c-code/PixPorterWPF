# PixPorter

A fast, lightweight image format converter  — convert single files, batch-convert entire folders, control quality, and strip metadata, all without leaving your workflow.

---

## Features

- **Drag & drop** images or folders directly into the app
- **Batch conversion** of entire directories
- **9 supported formats** — PNG, JPG, WEBP, GIF, BMP, TIFF, TGA, QOI, PBM
- **Quality control** for lossy and compression-based formats (JPG, WebP, PNG)
- **Metadata stripping** — removes EXIF, IPTC, XMP, and ICC profile data
- **Custom output folder** — redirect output anywhere, or default to alongside the source
- **Dark and light theme** with automatic system theme detection

---

## Default Format Mappings

When no output format is specified, PixPorter uses these defaults:

| Input | Output |
|-------|--------|
| `.webp` | `.png` |
| `.png` | `.webp` |
| `.jpg` / `.jpeg` | `.webp` |
| `.gif` | `.webp` |
| `.tiff` | `.webp` |
| `.bmp` | `.webp` |
| `.tga` | `.webp` |
| `.qoi` | `.webp` |
| `.pbm` | `.webp` |

---

## Quality

Quality applies to JPG, WebP, and PNG output. For JPG and WebP it controls pixel fidelity — lower values produce smaller, lossier files. For PNG it controls compression level only; PNG is always lossless and no pixel data is ever discarded.

---

## Open Source

PixPorter is built on [SixLabors.ImageSharp](https://sixlabors.com/products/imagesharp/), a fully managed, cross-platform image processing library.

> Licensed under the Apache License 2.0. © Six Labors.
