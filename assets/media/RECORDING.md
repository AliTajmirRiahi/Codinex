# Media / Recording Guide

This folder holds the screencasts referenced by the root `README.md`.
Drop the real files here with the **exact names** below and they will render automatically.

## Files expected by README.md

| File | Type | Length | Notes |
|------|------|--------|-------|
| `hero-poster.png`            | PNG  | –      | Still frame used as the `<video>` poster. |
| `hero-demo.mp4`              | MP4  | ~75 s  | Full "prompt → changeset → build → tests → commit" walkthrough. Prefer uploading via GitHub drag-and-drop and pasting the `user-attachments` URL; keep this committed copy as a fallback (Git LFS if > 5 MB). |
| `01-connect-provider.gif`    | GIF  | 8–15 s | Add a custom provider, capability badges light up. |
| `02-local-model-ollama.gif`  | GIF  | 8–15 s | Offline chat against a local Ollama / LM Studio model. |
| `03-context-references.gif`  | GIF  | 8–15 s | `@`-mention picker: file / folder / class / method / solution. |
| `04-agentic-tools.gif`       | GIF  | 8–15 s | Visible tool chain: list dir → search → find symbol → read element. |
| `05-changeset-review.gif`    | GIF  | 8–15 s | Change Review window: diff, approve one, reject one. |
| `06-build-and-tests.gif`     | GIF  | 8–15 s | Run Tests tool: red → patch → green. |
| `07-fix-diagnostics.gif`     | GIF  | 8–15 s | "fix this error" → reads diagnostics → applies fix. |
| `08-commit-message.gif`      | GIF  | 8–15 s | Generate commit message button in the VS Git UI. |
| `09-memory.gif`              | GIF  | 8–15 s | remember / forget a project fact, recalled later. |
| `10-rtl-persian.gif`         | GIF  | 8–15 s | Persian prompt flips composer to RTL. |
| `11-clarify.gif`             | GIF  | 8–15 s | Ask-User-Question card with options. |

## Capture settings

- Visual Studio tool window at a fixed size (~1280×800; ~1000×720 if docked), **DPI 100 %**, dark theme.
- Editor font ~14 pt so text stays readable after scaling.
- GIF: 12–15 fps, ≤ 15 s, palette-quantized, target **< 3 MB**.
- MP4 (hero): 1280×720 or 1920×1080, 30 fps, H.264, target **< 10 MB**.
- Use a small throwaway sample solution; hide personal paths and keys.

## Tools (Windows)

- **ScreenToGif** — region capture, GIF + MP4 export, trim/caption editor. Recommended.
- **ShareX** — quick GIF capture.
- **OBS Studio** — crisp MP4 for the hero clip.
