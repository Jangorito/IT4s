# Literature Processing Log

Use this file to record when PDFs are parsed and when indexes are updated.

| Date | Source | Action | Output | Notes |
|---|---|---|---|---|
| 2026-04-20 | All PDFs in `Literature/ToReview` | Initial batch parse using local `pypdf` install | 24 source cards and updated `literature_index.md` | 23 PDFs yielded usable text; Rowe 1991 appears scanned/image-only. |
| 2026-04-20 | Rowe 1991 - Machine listening and composing | Attempted text extraction | [source card](source_cards/rowe_1991_machine_listening_composing.md) | Parsing quality poor; needs OCR, text-based PDF, or manual notes before precise claims are made. |
| 2026-04-20 | Ostermann et al.; Hu et al.; Petit and Serrano papers | Parsed, but bibliographic metadata incomplete | Source cards created | Add BibTeX entries later to confirm exact year and venue. |

## Tooling Notes

`pypdf` was installed locally into `Literature/.tools/` because no system PDF parser was available. The directory is ignored by Git in `.gitignore`.

