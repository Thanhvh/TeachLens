# Publishing TeachLens to GitHub

Repository name used below: `TeachLens`  
GitHub account: `Thanhvh`

## Option A — Terminal

1. On GitHub, create a new **empty** repository named `TeachLens`. Do not add a README, `.gitignore` or license because they already exist locally.
2. Open PowerShell in `D:\Software\_development\TeachLens`.
3. Run:

```powershell
git init -b main
git add .
git commit -m "Release TeachLens 1.0 Acorn"
git remote add origin https://github.com/Thanhvh/TeachLens.git
git push -u origin main
```

GitHub may open a browser to sign in. Do not paste a password or access token into source files.

## Option B — GitHub Desktop

1. Open GitHub Desktop and choose **File → Add local repository**.
2. Select `D:\Software\_development\TeachLens`.
3. If prompted, choose **Create a repository**, set the branch to `main`, and keep the existing files.
4. Enter the summary `Release TeachLens 1.0 Acorn` and click **Commit to main**.
5. Click **Publish repository**.
6. Use the name `TeachLens`, choose public or private, and publish to the `Thanhvh` account.

## Create the 1.0 release

Build locally:

```powershell
.\build.ps1
Compress-Archive -Path .\artifacts\TeachLens-v1.0-Acorn\* -DestinationPath .\TeachLens-v1.0-Acorn-portable.zip -Force
```

On GitHub, open **Releases → Draft a new release**:

- tag: `v1.0.0`;
- title: `TeachLens 1.0 — Acorn`;
- description: copy the 1.0 section from `CHANGELOG.md`;
- attach `TeachLens-v1.0-Acorn-portable.zip` and `SHA256SUMS.txt`.

Publish only after running the self-test and manually checking the three overlay modes.

