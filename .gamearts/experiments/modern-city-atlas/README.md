# Modern city atlas experiment

Owner requested a trial of built-in image generation followed by Codex atlas assembly. This folder is an experiment, separate from production lifecycle records. Nothing here is approved or integrated into the city.

- [Diagnostic sheet](diagnostic-built/modern-city-diagnostic.png): house shell, park tree and streetlight; 4091×1170.
- [Measured atlas JSON](diagnostic-built/modern-city-diagnostic.atlas.json): actual cell rectangles, source offsets, default geometric pivots and source hashes.
- [Review/results](review.json), [source pixel checks](source-checks.json), [actual prompts and observed calls](image-generation-receipts.json).
- [Strict job](strict-frames.json) fails on near-magenta source pixels. No strict sheet was published.
- [Diagnostic job](diagnostic-frames.json) explicitly permits near-key pixels solely to pack the unchanged source rectangles for review. Its validator pass is under that exception, **not** a strict production-mask pass.

Five image calls produced three house attempts, one tree and one streetlight. House attempt 2 is selected; the first is too front-facing and the alpha retry has a halo/partial opacity. All originals are retained. No sprite pixels were cleaned, quantized or rescaled; all three atlas frames recover byte-for-byte as RGBA. Canonical pixel-grid recovery refused the selected house because no consistent column grid was detected; no recovered frame was created.

The house retry improves roof visibility. Camera consistency, native pixel grid, relative sizes and exact masking still fail or need review. The source city's concept remains unapproved; the project art profile, ground pivots, component separation, production assets and Godot acceptance remain open. An atlas organises the generated pixels; it does not make them better artwork or a faithful scene automatically.
