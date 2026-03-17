# OneDrive Sync — User Guide

## Getting started

OneDrive Sync is a desktop application for Linux and Windows that keeps your Microsoft personal OneDrive folders in sync with your local machine. You can connect multiple accounts, choose exactly which folders to sync, and review any conflicts before they are resolved.

---

## Adding your first account

1. Click the **Accounts** icon in the left rail (the person silhouette).
2. Click **Add account** — either the button in the left panel or the one in the main area.
3. The three-step wizard opens:
   - **Step 1 — Sign in.** Click **Open browser to sign in**. Your default browser will open a Microsoft sign-in page. Sign in with your personal Microsoft account (Outlook, Hotmail or Live). The app waits for you to complete sign-in and then automatically moves on.
   - **Step 2 — Select folders.** Your OneDrive root folders are listed. Tick the ones you want to sync. Click **Skip** if you would rather choose folders later from the Files section.
   - **Step 3 — Confirm.** Review your choices and click **Finish**.

Your account now appears in the left panel and in the Accounts section.

---

## Selecting folders to sync

### Root folders

Navigate to **Files** (the folder icon in the rail). Your account appears as a tab at the top. Each root folder in your OneDrive is listed with a status badge showing whether it is included or excluded. Click **Include** or **Exclude** on any row to toggle it.

### Sub-folders

This is the less obvious feature: every folder row has a small expand arrow (▸) on its left edge. Click it to load the folder's sub-folders from OneDrive. Sub-folders appear indented beneath their parent and each has its own **Include / Exclude** toggle. You can nest as deeply as you like — only the folders you explicitly include will be synced.

> **Tip:** Including a sub-folder does not automatically include its parent. If you want only `Documents/Work` synced and not the rest of `Documents`, expand `Documents` and include only `Work`.

### Other folder actions

Each folder row also has two icon buttons on the right:

| Icon | Action |
|------|--------|
| ↑ Upload arrow | Opens the folder in your system file manager (only works once a local sync path has been set in Settings) |
| ≡ Lines | Navigates to the Activity view filtered to that folder |

---

## Syncing

### Manual sync

- Click **Sync now** on any account card in the Dashboard.
- Or use the **Sync now** button in the status bar at the bottom of the window.

### Scheduled sync

The app automatically checks for changes on a schedule (default: every 60 minutes). You can adjust this in **Settings → Sync policy → Sync interval**.

---

## Resolving conflicts

A conflict occurs when the same file has been modified both locally and on OneDrive since the last sync. Conflicts never block other files from syncing — they are queued and you resolve them at your own pace.

1. A badge on the **Activity** icon in the rail shows the number of pending conflicts.
2. Click **Activity** then switch to the **Conflicts** tab.
3. Each conflict row shows the file name and path. Click **Resolve** to expand the resolution panel.
4. The panel shows the local and remote versions side by side with their modification times and sizes.
5. Choose a resolution policy:

| Policy | What happens |
|--------|-------------|
| **Ignore** | Skip this conflict — both versions remain unchanged until you resolve it |
| **Keep both** | The local copy is renamed with a timestamp suffix; the remote version is downloaded |
| **Last write wins** | Whichever version was modified more recently overwrites the other |
| **Local wins** | Your local version is kept; the remote is overwritten on next sync |
| **Remote wins** | The remote version is downloaded and replaces your local copy |

6. Click **Apply** to resolve, or **Dismiss** to skip without resolving.

---

## Settings

Click the **Settings** icon (gear) at the bottom of the rail.

### Appearance

Switch between **Light**, **Dark** and **System** themes. The change applies immediately.

### Sync policy

- **Default conflict resolution** — the policy applied automatically when a conflict is detected. Defaults to *Ignore* so nothing is ever overwritten without your review.
- **Sync interval** — how often the scheduler checks for changes (5, 15, 30, 60 minutes or 2 hours).

### Account sync paths

Each connected account needs a local folder to sync into. Click **Browse…** next to an account, choose a folder, and click **Save**. Sync will not run for an account until a path is set.

> **Example paths:**
> - Linux: `/home/jason/OneDrive/personal`
> - Windows: `C:\Users\Jason\OneDrive\personal`

---

## The Dashboard

The Dashboard gives you an at-a-glance health check across all accounts:

- **Global stats strip** at the top shows total accounts, folders, conflicts, overall status and time since last sync.
- **Account sections** below show per-account storage, folder count, conflict count and the three most recent activity items.
- Click **▾ / ▸** on any account section to collapse or expand it.
- Click **Sync now** on any section to trigger an immediate sync for that account.

---

## The Activity log

The **Activity** section (clock icon) records every file operation performed by the sync engine. Use the filter chips at the top right to show only downloads, uploads or errors. Click **Clear** to empty the log.

Conflicts appear in the **Conflicts** tab and remain there until resolved.

---

## Tips

- **Multiple accounts** — you can add as many personal Microsoft accounts as you like *. Each gets its own colour dot, tab in Files, and section in the Dashboard.
- **Token cache** — you do not need to sign in again each time you launch the app. Tokens are securely cached using your system keychain (libsecret on Linux, DPAPI on Windows).
- **Selective sync** — syncing a large OneDrive by default is opt-in, not opt-out. No folders are synced until you explicitly include them.
- **Conflict-safe** — with the default *Ignore* policy, a conflict will never silently overwrite your work. Change the policy per-account in Settings once you are comfortable with the behaviour.

- * the limit probably exists but we've not found one yet! Probably unique colours will run out before your desire to add accounts will!
