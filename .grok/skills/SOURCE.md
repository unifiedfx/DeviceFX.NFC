# Source

Project skills live in `.grok/skills/<skill-name>/`. Invoke with `/<skill-name>` (for example `/maui-shell-navigation` or `/migrate-dotnet9-to-dotnet10`) or by describing a matching task. Grok loads skills on demand from the description field.

Conversion for Grok (all sources):

- Each upstream skill directory was copied as-is (`SKILL.md`, `references/`, `scripts/`).
- Copilot/Claude plugin metadata, marketplace install files, and `evals/` were omitted.
- No `SKILL.md` frontmatter or body edits. Format already matches Grok (`name` + `description`).

## davidortinau/maui-skills (MIT)

Grok project skills converted from [davidortinau/maui-skills](https://github.com/davidortinau/maui-skills).

- Commit: `79bb4bfd8f671ed0dd97bdf92a36430928e8d984` (2026-07-06)
- Upstream path: `plugins/maui-skills/skills/`
- Installed at: `.grok/skills/<skill-name>/`
- License: `.grok/skills/LICENSE` (Copyright 2026 David Ortinau)

## dotnet/skills — migrate-dotnet9-to-dotnet10 (MIT)

Official .NET team skill from [dotnet/skills](https://github.com/dotnet/skills), plugin `dotnet-upgrade`.

- Commit: `7c1ae3fdf2eb64b758bb3a7b7f92cad3fbd95868` (2026-08-14)
- Upstream path: `plugins/dotnet-upgrade/skills/migrate-dotnet9-to-dotnet10/`
- Installed at: `.grok/skills/migrate-dotnet9-to-dotnet10/`
- License: `.grok/skills/migrate-dotnet9-to-dotnet10/LICENSE` (Copyright .NET Foundation and Contributors)

This skill is general .NET 9 → .NET 10 (TFM, SDK, C# 14, libraries). Pair it with `maui-current-apis` and `maui-safe-area` for MAUI 10 control and layout changes.
