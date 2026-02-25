# GitHub Upload Guide

Follow these steps to upload your `QFG5Extractor` project to GitHub correctly.

## 1. Create a Repository on GitHub
1.  Go to [github.com/new](https://github.com/new).
2.  **Repository name:** `QFG5Extractor` (or your preferred name).
3.  **Public/Private:** Choose based on your preference.
4.  **DO NOT** check "Initialize this repository with a README", "Add .gitignore", or "Choose a license" (we already created these locally).
5.  Click **Create repository**.

## 2. Link Local Files to GitHub
After creating the repo, GitHub will show you a page with "Quick setup". Look for the section titled **"…or push an existing repository from the command line"**.

Copy and run these commands in your terminal (inside the `QFG5Extractor` folder):

```bash
# Add the GitHub repository as a 'remote'
git remote add origin https://github.com/YOUR_USERNAME/QFG5Extractor.git

# Set the main branch
git branch -M main

# Push the code to GitHub
git push -u origin main
```
> [!NOTE]
> Replace `YOUR_USERNAME` with your actual GitHub username in the URL above.

## 3. Upload the 'Release' Binary (Recommended)
You should keep the large `.exe` files out of the main source history. Instead, use "GitHub Releases":
1.  On your new GitHub project page, look for **"Releases"** on the right sidebar.
2.  Click **"Create a new release"**.
3.  **Tag version:** `v1.0.0`
4.  **Release title:** `Initial Release`
5.  **Description:** Summarize the features (SPK, Model, Pano, Msg extraction).
6.  **Attach binaries:** Drag and drop your `QFG5Extractor.exe` from `src/bin/Release/` into the upload box.
7.  Click **Publish release**.

## 4. Keeping it updated
Whenever you make more changes in the future:
```bash
git add .
git commit -m "Describe your changes"
git push
```
