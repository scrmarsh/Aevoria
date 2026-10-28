# GitHub workflow

Repository: https://github.com/scrmarsh/Aevoria

Commit coherent development checkpoints to `main`, with a short description of
the change and the checks actually run. A development commit is not release or
visual acceptance. Preserve rejected evidence and distinguish untested source
from the last tested player in `AEVORIA_RESUME.md`.

Before committing, inspect `git status`, review the staged diff, check for private
credentials or save data, and run the checks appropriate to the changes. Include
Unity `.meta` files with their assets and package/project settings needed to open
the project. Include portable `.csproj` files with their tests.

Run `py Tools/ValidateGitPreservation.py --index` before committing protected
delivery files, and `py Tools/ValidateGitPreservation.py` afterwards. The manifest
in `Docs/PROTECTED_ASSETS.json` records101 accepted character delivery files and
the Chronicle. Their original byte hashes include line endings; `.gitattributes`
disables conversion for these files and source assets. The protected Blender
backup files are intentionally tracked as part of that accepted delivery.

Do not add `Library`, `Temp`, generated players, `Archives`, original player saves,
or bulk native capture directories. Their `.gitignore` exclusions do not delete
them. Written validation reports and requirement briefs remain tracked; publish
selected additional evidence deliberately when needed. Use GitHub Releases for
future accepted player distributions, separately from source commits.

Never commit credentials, force-push shared history, or claim that an unfinished
candidate is a release. Continue making focused commits after meaningful verified
changes rather than waiting for the entire next release to be finished.
