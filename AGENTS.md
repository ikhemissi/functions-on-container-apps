# AGENTS.md — Azure Functions on Container Apps

This repository is a **public sample**: it shows one pattern, working, for anyone to read, deploy and reuse. Read [README.md](README.md) first.

## How the sample is kept

- **The README is the product:** keep its order (header, overview image, why, what it shows, how it works, what runs where, quick start, daily use, design notes, tech stack, documentation, versions and preview notes, clean up, cost, licence) and keep it true: it states only what [the verification table](docs/verification.md) shows, or says "expected" and why.
- **Decisions:** every design choice a reader could question gets a numbered line in [docs/DECISIONS.md](docs/DECISIONS.md) (date, decision, why). A decision that changes is superseded by a new line, not rewritten.
- **Verification:** every claim is checked and recorded in [docs/verification.md](docs/verification.md) (what, how, result with its date and the versions involved), including the measured cost.
- **The overview image** (`docs/overview.svg`): arrows go from the caller to what it calls; it shows SKUs and measured behaviour, not prices. Render it and look at it before publishing.
- **Ask before deciding** when a choice changes what the sample shows; propose a default.
- **Report as you go:** end every turn that did work with four lines: *done* (finished and verified), *running* (background work, with its time limit), *next* (with an estimate) and *needs you* (a decision, a sign-in, a page that could not be read) or nothing. Give every background command and helper a time limit; when one runs over, stop it and say so.
- **Grounded claims:** link the source of every product fact (feature, region, limit, version); mark what was not checked as an assumption.

## Rules

- **This repository is public.** Never commit secrets, keys, tokens, tenant or subscription IDs, e-mail addresses or real user data; tenant-specific values (tags, IDs) go to the azd environment, never to files. Use your own Entra identity and managed identities; local settings go to `.env` (git-ignored).
- **Neutral wording:** describe products factually, with dates and versions; no verdicts, no "production-ready", no claims the repository does not show.
- **Fictional scenario and data** only; screenshots taken in one session and cleaned of identities.
- **Commits:** conventional commits (`feat: …`, `fix(infra): …`, `docs: …`), ending with the trailer `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
- **CI** (`.github/workflows/ci.yml`) stays green on every pull request; each job can be run locally with the commands it shows.

## Infrastructure

- **One Terraform root** (`infra/`, one state), one file per part of the architecture. The files from shared building blocks (`container-apps-environment.tf`, `container-registry.tf`, `key-vault.tf`, `monitoring.tf`, `main.tf`, `providers.tf`, …), the modules in `infra/modules/` and the hook steps in `infra/hooks/<event>/` stay as they are, so that their improvements can be merged in later. Put the sample's own resources in a new file of `infra/` (calling a module of `infra/modules/` where one fits), its settings as variables mapped in `infra/main.tfvars.json`, its outputs as azd values, and its steps in `infra/hooks/<event>/NN-<name>.sh`.
- **Before every `azd provision`:** `azd provision --preview` (one plan for the whole root) and read it: only expected adds and changes, no unexpected destroy. Module tests: `terraform init -backend=false && terraform test` in `infra/modules/<module>`.

## GitHub Copilot cloud agent

The cloud agent has no Azure access; its toolchain comes from `.github/workflows/copilot-setup-steps.yml` (.NET 10, Terraform). Deployment and the live checks of [verification](docs/verification.md) are done in a session with Azure access.

- Follow [docs/DECISIONS.md](docs/DECISIONS.md); when an issue needs a decision it does not cover, say so in the PR instead of choosing silently.
- Never run `azd`, `az` or anything that needs credentials; never change the files of shared building blocks (see Infrastructure).
- Offline checks, all of them before asking for review:
  - Terraform: `terraform -chdir=infra fmt -check -recursive`, `terraform -chdir=infra init -backend=false && terraform -chdir=infra validate`, and `terraform init -backend=false && terraform test` in each `infra/modules/<module>`.
  - .NET: `dotnet build src && dotnet test src` (warnings as errors), and the container image builds (`docker build src/ReceiptFunctions` in CI).
  - Scripts: `shellcheck -x` on every `*.sh`.

## Code humans can maintain

Everything here must be maintainable by human developers, whoever (or whatever) wrote it. A developer new to the project understands a module in minutes and changes it with confidence.

- **Simple and explicit over clever:** no metaprogramming, hidden magic or generic frameworks; an abstraction only when a second real use exists.
- **One role per unit:** a module states its role in one or two lines at the top; a function does one thing and fits on a screen (about 40 lines), otherwise split it into named steps.
- **Names carry meaning:** domain words, full words, booleans that read as questions; no single letters except short loop indices.
- **Flat control flow:** early returns, named intermediate values, no dense one-liners or formatter opt-outs.
- **Comments explain why** (the decision, the constraint, a link to the design or ADR), never what the code already says.
- **Consistency beats preference:** follow the patterns already in the repository before introducing a new one.
- **Few dependencies:** ask before adding one; prefer the standard library and what is already used.
- **Tests read as documentation:** named after the behaviour or spec scenario they check; arrange, act, assert.
- **Read your diff as a reviewer before calling a change done;** if a piece cannot be explained simply, rewrite it.

## Known pitfalls

If `.fast-start/pitfalls.md` exists (local only, git-ignored: never commit, copy or quote it), read it before changing
infrastructure or services: it lists pitfalls known for this kind of project.

- None recorded yet.
Add a pitfall here every time something bites you, in one line: the symptom, then what to do.
