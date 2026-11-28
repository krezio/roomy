# Contributing to Roomy

Thanks for wanting to help. Roomy deletes files on people's PCs, so every change has to be one a person has read, run and understood.

## Before you open a pull request

- **Bug fixes and translations:** go ahead and open a PR.
- **Anything else** (new features, refactors, performance tweaks, "cleanups"): open an [issue](https://github.com/krezio/roomy/issues) first and wait for a yes. Unrequested changes in these areas will usually be closed.
- **Keep it small.** One fix or feature per PR. Large PRs that touch many unrelated things are hard to review safely and will be closed.

## What every pull request needs

- **You ran it.** Build Roomy and use the part you changed on a real Windows PC, not just the tests.
- **The tests pass:** `dotnet test tests/Roomy.Tests -c Release`.
- **An honest description.** Say what you changed, why, and exactly what you checked. If something changes behavior, say so plainly, even if it's small.
- **No changes to what gets deleted** (cleanup targets, guards, the junction and protected-folder checks) without an issue discussing it first.

AI tools are fine to use, but you're responsible for every line. PRs that look generated and untested, or whose description doesn't match the code, will be closed without a detailed review.

## Translations

All text lives in `src/Roomy/Localization/strings.json`. See [the localization guide](src/Roomy/Localization/README.md). You only need to add your language to that one file.

## Reporting bugs

Use the [issue templates](https://github.com/krezio/roomy/issues/new/choose). If Roomy crashed, attach `%LOCALAPPDATA%\Roomy\crash.log`.

## Security issues

If you find something that could make Roomy delete the wrong files, please describe it in an issue with steps to reproduce. See [#4](https://github.com/krezio/roomy/issues/4) for a great example.
