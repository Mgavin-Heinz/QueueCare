# Contributing to QueueCare

Git evidence is marked, so every member commits their own work from their own GitHub account.

## Branches
| Branch | Purpose |
| --- | --- |
| `main` | Demo-ready code only. Updated from `dev` before submission and tagged `v1.0`. |
| `dev` | Integration branch. All features merge here through a pull request. |
| `feature/<name>` | One branch per task, e.g. `feature/live-queue-page`. |
| `fix/DEF-xx` | Bug fixes from the defect log, e.g. `fix/DEF-03`. |

## One-time setup
```
git config --global user.name  "Your Name"
git config --global user.email "the-email-on-your-github-account"
git clone https://github.com/Mgavin-Heinz/QueueCare.git
cd QueueCare
git checkout dev
```

## Every task
```
git checkout dev
git pull
git checkout -b feature/<task-name>
# work in small steps, commit each time one thing works
git add .
git commit -m "Add live queue page with 30-second refresh"
git push -u origin feature/<task-name>
```
Then open a pull request **into `dev`** on GitHub and ask one teammate to review it.

## Commit messages
Write it as a command, say what changed, keep it under ~60 characters, one change per commit.

| Good | Bad |
| --- | --- |
| Add BookingService with daily capacity check | update |
| Fix DEF-03: double booking on the same slot | fixed stuff |
| Build reception dashboard with call-next button | Tirick's work |

## Rules
- Commit from your own laptop and account. Push every day you code.
- Never commit someone else's work for them, and never upload the project in one big commit.
- Every pull request is reviewed by one other member before merging.
- Never commit passwords or connection strings (`appsettings.Development.json` stays local).
- Never `git push --force` to `dev` or `main`.
- Project documents (backlog, test plan, diagrams) go in `/docs`.
