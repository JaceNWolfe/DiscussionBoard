# Discussion Board Starter Project

This repository provides the starter project for the **Discussion Board** ASP.NET Core assignment.

You will fork this repository into your own GitHub account and use your fork throughout the project. Your Git repository will serve as the working history of your project and will also be used to create the ZIP file you submit to Blackboard.

> **Important:** Do not download the repository as a ZIP file to begin the assignment. Fork the repository on GitHub and then clone your fork as described below.

## Current Project Status

| Lesson | Status |
| --- | --- |
| Starter Project | ![Starter Project Tests](../../actions/workflows/starter-tests.yml/badge.svg) |

Each badge shows the current status of the automated tests for that stage of the project. A new lesson's badge should initially be failing; it should become green as you complete that lesson's requirements.

## Project Environment

- .NET 10
- Rider-friendly repository 
- Shared MSBuild settings in `Directory.Build.props`
- Local .NET tools in `.config/dotnet-tools.json`
- xUnit.net v3-generation tests using the 4.x package line and Microsoft Testing Platform v2
- SQLite development databases are generated state and are not committed

# Fork and Clone the Starter Repository

## Fork the Repository

Open the starter repository on GitHub and select **Fork** in the upper-right corner of the repository page.

This creates your own copy of the repository under your GitHub account.

You will make your changes in **your fork**, not in the original class repository.

## Clone Your Fork

On the GitHub page for **your fork**, select **Code → SSH** and copy the SSH repository address.

In a command-line terminal such as Git Bash, change to the directory where you keep your class projects with the cd command:

In Git Bash:
```bash
cd ~/RiderProjects/
```

In Windows Terminal:
```bash
cd $env:USERPROFILE\RiderProjects
```

In cmd.exe:
```bash
cd %USERPROFILE%\RiderProjects
```

Then run:

```bash
git clone <your-repository-address>
```

For example:

```bash
git clone git@github.com:yourusername/DiscussionBoard.git
```

Then enter the repository directory:

```bash
cd <repository directory>
```

## Verify Your Repository

Run:

```bash
git status
```

You should see a message similar to:

```text
On branch main
Your branch is up to date with 'origin/main'.

nothing to commit, working tree clean
```

Next, check which GitHub repository your local project is connected to:

```bash
git remote -v
```

The `origin` addresses should point to your GitHub fork. For example:

```text
origin  git@github.com:yourusername/DiscussionBoard.git (fetch)
origin  git@github.com:yourusername/DiscussionBoard.git (push)
```

## Add the Instructor Repository

Your fork is called `origin` locally and points to your repository on GitHub. You will also add the instructor's starter repository as a second remote called `upstream`.

You only need to do this once:

```bash
git remote add upstream git@github.com:WVUP/DiscussionBoard.git
```

Verify the configuration:

```bash
git remote -v
```

You should now see both `origin` and `upstream`.

---

# Open and Verify the Starter Project

Open `DiscussionBoard.slnx` in JetBrains Rider.

Allow Rider to restore the project's NuGet packages if necessary.

Before changing any code, verify that the starter project builds successfully.

From Rider, use **Build → Build Solution**.

You can also build from a terminal in the repository root:

```bash
dotnet build
```

Then run the automated tests:

```bash
dotnet test
```

Finally, run the ASP.NET Core application from Rider and verify that it opens successfully in your browser.

Do not begin the assignment until the starter project builds, the tests run, and the application starts successfully.

# Working with GitHub

Check your repository on GitHub and make sure your recent commits appear there in your main branch.

A good rule for this class is:

> **Pull when you begin. Commit while you work. Push before you leave.**

---

# Receiving Lesson Updates

New lesson requirements, automated tests, GitHub Actions workflows, and README updates are released through the course updater.

The updater receives instructor materials from `upstream` while leaving your normal `origin` workflow unchanged.

## Install the Next Lesson

Before installing a lesson update, make sure your current work is committed in git:

```bash
git status
```

Your working tree should be clean.

Then run:

```bash
dotnet run --project tools/CourseUpdater
```

The updater will:

1. Verify that your working tree is clean.
2. Fetch the available instructor lesson releases.
3. Determine the next lesson you need.
4. Install that lesson's tests, GitHub Actions workflow, README changes, and other instructor-provided files.

After the update is installed, run:

```bash
dotnet clean
dotnet restore
dotnet build
dotnet test
```

A newly released lesson may contain tests that fail at first. **This is expected.** Those tests describe behavior you will implement during the lesson.

Push the installed lesson update to your GitHub fork:

```bash
git push
```

## Check Lesson Status

To see which lesson updates have already been installed:

```bash
dotnet run --project tools/CourseUpdater -- --list
```

If the updater cannot install a lesson automatically, it will return the repository to its previous state. Run:

```bash
git status
```

and ask for assistance before trying to force the update.

# Before Submitting

Before creating your submission, make sure the project is in a working state.

Run:

```bash
dotnet build
```

Then:

```bash
dotnet test
```

Make sure all tests are passing.

Run the application and verify its behavior in your browser.

Next, check Git:

```bash
git status
```

You should see:

```text
nothing to commit, working tree clean
```

If you have changes that should be part of your assignment, commit them before continuing.

Finally:

```bash
git push
```

Verify on GitHub that your most recent commit appears in your repository and that all badges in your README are green.

---

# Submitting the Project to Blackboard

Your Blackboard submission should contain the **committed version** of your Git project.

From the root directory of the repository, run:

```bash
git archive HEAD -o <yourname>-DiscussionBoard.zip
```

For example:

```bash
git archive HEAD -o JaneSmith-DiscussionBoard.zip
```

This creates a ZIP archive from the most recent Git commit.

Because the archive is created by Git, files that are not tracked by the repository—such as build output and IDE-specific files—will not be included.

> **Important:** `git archive` includes only committed files. If you forgot to commit a change, that change will **not** be included in your submission.

After creating the archive, open the ZIP file and verify that it contains:

```text
src/
tests/
tools/
.course/
DiscussionBoard.slnx
global.json
README.md
```

Submit the resulting ZIP file to Blackboard along with the URL of your project's GitHub repository.

---

# Git Command Quick Reference

| Command | Purpose |
| --- | --- |
| `git status` | See the current state of your repository |
| `git diff` | See your uncommitted changes |
| `git pull` | Get changes from your GitHub fork |
| `git add .` | Stage your changes for a commit |
| `git commit -m "message"` | Create a checkpoint |
| `git push` | Send your commits to GitHub |
| `git log --oneline` | View your commit history |
| `dotnet run --project tools/CourseUpdater` | Install the next lesson update |
| `dotnet run --project tools/CourseUpdater -- --list` | Show installed lesson updates |
| `git restore <file>` | Discard uncommitted changes to a file |

## The Workflow to Remember

```text
START WORK
    ↓
git status
    ↓
git pull
    ↓
Make a small set of changes
    ↓
Build and test
    ↓
git add .
    ↓
git commit
    ↓
Continue working
    ↓
git push
    ↓
CHECK GITHUB
```

**Pull when you begin. Commit while you work. Push before you leave.**


