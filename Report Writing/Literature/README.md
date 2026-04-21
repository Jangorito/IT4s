# Literature Workflow

This folder is the dissertation literature intake and indexing area for Intelli-Trading Fours.

## Folder Structure

```text
Literature/
  ToReview/
  Processed/
  Notes/
    literature_index.md
    claim_support_log.md
    processing_log.md
    source_cards/
    templates/
      source_card_template.md
  references.bib
```

## How To Add New Literature

1. Put new PDFs in `Literature/ToReview/`.
2. Use structured filenames where possible:

   ```text
   Author_Year_ShortTitle.pdf
   ```

   Examples:

   ```text
   Pachet_2002_Continuator.pdf
   Rowe_1991_MachineListening.pdf
   Donnay_2014_TradingFours.pdf
   ```

3. Add BibTeX entries to `Literature/references.bib` when available.
4. If a paper has a specific use, note it when asking for parsing, for example:

   ```text
   Parse new literature. The Donnay paper is mainly for trading fours and turn-taking.
   ```

## Parsing Output

For each parsed source, a source card should be created in:

```text
Literature/Notes/source_cards/
```

The master index should then be updated:

```text
Literature/Notes/literature_index.md
```

Claims that need citation support should be tracked in:

```text
Literature/Notes/claim_support_log.md
```

## Parsing Quality

Each source card includes a parsing quality field:

```text
Parsing quality: good / partial / poor
Needs manual check: yes / no
```

Scanned PDFs or badly extracted PDFs should be marked honestly so they are not over-trusted.

