# OneDrive Multi-Account Sync - User Manual

**Version**: 3.0  
**Last Updated**: January 8, 2026

---

## Table of Contents

1. [Welcome](#welcome)
2. [Getting Started](#getting-started)
3. [Managing Your Accounts](#managing-your-accounts)
4. [Selecting Folders to Sync](#selecting-folders-to-sync)
5. [Running a Sync](#running-a-sync)
6. [Understanding Conflict Resolution](#understanding-conflict-resolution)
7. [Advanced Settings](#advanced-settings)
8. [Automatic Sync Features](#automatic-sync-features)
9. [Viewing Sync History and Logs](#viewing-sync-history-and-logs)
10. [Troubleshooting - Basic](#troubleshooting---basic)
11. [Troubleshooting - Advanced](#troubleshooting---advanced)
12. [Frequently Asked Questions](#frequently-asked-questions)

---

## Welcome

Welcome to OneDrive Multi-Account Sync! This application helps you synchronize multiple Microsoft OneDrive accounts to your local computer. Whether you have personal and work accounts, or manage multiple business accounts, this tool makes it easy to keep your files in sync across all of them.

### What This Application Does

- **Sync Multiple Accounts**: Connect and sync as many OneDrive accounts as you need
- **Choose What to Sync**: Select specific folders from each account instead of syncing everything
- **Two-Way Sync**: Changes made locally or in OneDrive are synchronized in both directions
- **Conflict Resolution**: When the same file changes in both places, you decide which version to keep
- **Automatic Monitoring**: Optionally enable automatic syncing when files change locally or on a schedule
- **Safe and Secure**: Uses Microsoft's official authentication system - your credentials stay with Microsoft

---

## Getting Started

### First Time Launch

When you first open the application, you'll see:
- An empty account list on the left
- A message prompting you to add your first account
- Menu options at the top for settings and logs

### Adding Your First Account

1. Click the **"Add Account"** button in the left panel
2. A browser window will open asking you to sign in to Microsoft
3. Sign in with your Microsoft account (personal or work/school)
4. You'll be asked to grant permissions - this allows the app to access your OneDrive files
5. After authorizing, close the browser and return to the application

**What Permissions Are Requested?**

The application needs permission to:
- **Read your files**: To know what files exist in your OneDrive
- **Write files**: To upload changes you make locally
- **Access when you're not using the app**: To enable automatic sync features

Your password is never shared with this application - Microsoft handles all authentication securely.

### Understanding the Default Setup

After adding an account, the application creates a default folder on your computer to store synced files. This is typically located at:
- **Windows**: `C:\Users\[YourName]\Documents\AStarOneDrive\[AccountName]`
- **macOS**: `/Users/[YourName]/Documents/AStarOneDrive/[AccountName]`
- **Linux**: `~/Documents/AStarOneDrive/[AccountName]`

You can change this location in the account settings (see [Managing Your Accounts](#managing-your-accounts)).

---

## Managing Your Accounts

### Viewing Account Information

The left panel shows all your connected accounts. For each account, you'll see:
- **Display Name**: Your Microsoft account name (e.g., john.doe@company.com)
- **Authentication Status**: Whether the account is currently signed in
- **Last Sync Time**: When the last successful sync completed

### Selecting an Account

Click on any account in the list to:
- View and select folders to sync (right panel)
- Start a manual sync
- See sync progress and status

### Updating Account Settings

To change settings for an account:

1. Go to **File Menu** → **Update Account Details** (or press **F2**)
2. Select the account you want to modify
3. You can change:
   - **Local Sync Path**: Where files are stored on your computer
   - **Detailed Sync Logging**: Enable to see detailed information about every file operation
   - **Debug Logging**: Enable to save troubleshooting information to the database
   - **Max Parallel Uploads/Downloads**: How many files to transfer at once (1-10)
   - **Max Items in Batch**: How many items to process in one batch (1-100)
   - **Auto-Sync Interval**: How often to check OneDrive for changes (60-1440 minutes, or leave blank to disable)

4. Click **Update** to save your changes

**Tip**: Higher parallel upload/download settings speed up sync for many small files, but use more network bandwidth.

### Removing an Account

1. Select the account in the left panel
2. Click the **"Remove Account"** button
3. Confirm the removal

**Important**: Removing an account does NOT delete your local files. They remain on your computer, but will no longer sync with OneDrive.

---

## Selecting Folders to Sync

### Understanding the Folder Tree

The right panel shows your OneDrive folder structure. Each folder has a checkbox with three possible states:

- **✓ Checked**: This folder and all its contents will sync
- **☐ Unchecked**: This folder will not sync
- **▣ Partially Checked**: Some subfolders are selected, but not all

### Selecting Folders

1. Select an account from the left panel
2. Browse the folder tree in the right panel
3. Click checkboxes to select folders you want to sync
4. Subfolders are automatically included when you check a parent folder

**Tips**:
- Checking a parent folder automatically checks all subfolders
- Unchecking a parent folder automatically unchecks all subfolders
- You can mix and match - check some subfolders while leaving others unchecked

### Expanding Folders

Click the **▶** arrow next to a folder name to expand and see its subfolders. The application loads subfolders on-demand, so large folder structures load quickly.

### Clearing All Selections

Click the **"Clear All"** button at the top-right of the folder tree to uncheck all folders. This is useful if you want to start fresh with your selections.

---

## Running a Sync

### Starting a Manual Sync

1. Select an account from the left panel
2. Choose which folders to sync (if you haven't already)
3. Click the **"Start Sync"** button at the bottom of the screen

### What Happens During Sync

The sync process works in several phases:

1. **Scanning for Changes** (Remote)
   - The application asks OneDrive what files have changed
   - You'll see the current folder being scanned
   - This phase is usually very fast thanks to OneDrive's change tracking

2. **Scanning for Changes** (Local)
   - The application checks which local files have changed
   - Compares file modification times and sizes

3. **Uploading Changes**
   - Files you created or modified locally are uploaded to OneDrive
   - You'll see a progress bar and transfer speed
   - Multiple files upload at once (based on your settings)

4. **Downloading Changes**
   - Files changed in OneDrive are downloaded to your computer
   - You'll see a progress bar and transfer speed
   - Multiple files download at once (based on your settings)

5. **Handling Deletions**
   - Files deleted in one location are removed from the other
   - This keeps both sides in sync

### Monitoring Sync Progress

While syncing, you'll see an overlay showing:
- **Current Phase**: What the sync is currently doing
- **Progress Bar**: Visual representation of completion
- **Files Transferred**: How many files have been processed
- **Transfer Speed**: Current upload/download speed
- **Time Remaining**: Estimated time to completion (updated per phase)
- **Data Transferred**: How much data has been uploaded/downloaded

### Canceling a Sync

Click the **"Cancel Sync"** button at the bottom of the screen to stop an in-progress sync. The application will:
- Finish uploading/downloading the current file
- Save progress so you can resume later
- Mark incomplete operations so they can be retried

**Note**: Canceling is safe and won't leave files in a bad state.

---

## Understanding Conflict Resolution

### What Is a Conflict?

A conflict occurs when the same file changes in **both** locations between syncs:
- You edit a document on your computer
- Someone else (or you from another device) edits the same document in OneDrive
- The sync needs your help to decide which version to keep

### When Conflicts Appear

Conflicts are detected during the sync process. When conflicts are found:
- The sync overlay **stays open** instead of auto-closing
- A **"⚠️ View Conflicts"** button appears in yellow/orange
- The number of conflicts is shown in the sync summary

### Viewing Conflicts

Click either:
- **"View Conflicts"** button on the sync overlay, OR
- **"⚠️ View Conflicts"** button in the main window (appears when conflicts exist)

### The Conflict Resolution Screen

For each conflict, you'll see:

**File Information**:
- File name and path
- Local version: Modified date and size
- Remote version: Modified date and size
- When the conflict was detected

**Resolution Options**:
1. **None**: Skip this file for now (you can resolve it later)
2. **Keep Local**: Use the version on your computer (overwrites OneDrive)
3. **Keep Remote**: Use the version in OneDrive (overwrites your local file)
4. **Keep Both**: Rename your local file and download the OneDrive version

### Choosing a Resolution Strategy

For each conflict:
1. Review the modification dates and file sizes
2. Consider which version is more recent or important
3. Select your preferred resolution from the dropdown

**Tips**:
- **Keep Both** is the safest option if you're unsure
- You can set different strategies for different files
- The local file is renamed to `filename_conflict_[timestamp].ext` when keeping both

### Resolving All Conflicts

After choosing strategies for each conflict:
1. Click the **"Resolve All"** button
2. The application applies each conflict's strategy
3. Files are uploaded, downloaded, or renamed as needed
4. Conflicts marked as "None" are skipped

**Important**: "Resolve All" respects your individual choices - it doesn't apply one strategy to all files.

### After Resolution

Once conflicts are resolved:
- The conflict resolution screen closes
- The sync summary updates
- The **"⚠️ View Conflicts"** button disappears (if no conflicts remain)
- Your files are now in sync again

---

## Advanced Settings

### Performance Tuning

Access these settings via **File → Update Account Details**:

**Max Parallel Uploads/Downloads (1-10)**:
- Default: 3
- Higher values = faster sync for many small files
- Lower values = less network/CPU usage
- Recommended: 3-5 for typical use, 8-10 for large photo/video libraries

**Max Items in Batch (1-100)**:
- Default: 50
- How many items to process before checking for cancellation
- Higher values = slightly better performance
- Lower values = more responsive cancellation
- Recommended: Keep default unless experiencing issues

### Logging Options

**Detailed Sync Logging**:
- **When to enable**: If you need to see exactly what happens to each file
- **Impact**: Creates detailed records of every file operation
- **Location**: Stored in the database, viewable via **File → View Sync History**

**Debug Logging**:
- **When to enable**: When troubleshooting problems with support
- **Impact**: Saves extensive diagnostic information
- **Location**: Database, viewable via **File → View Debug Logs**

**Warning**: Enabling both logging options uses more disk space and may slightly slow down syncs.

---

## Automatic Sync Features

### File Watching (Immediate Sync)

After your first successful manual sync, the application automatically monitors your local folder for changes:

**How It Works**:
- When you save a file, the change is detected immediately
- A sync starts automatically after a short delay (to batch multiple changes)
- Only changed files are uploaded - very fast!

**Status Indicator**:
- The account shows "Auto-sync enabled" when file watching is active
- If file watching stops, run a manual sync to re-enable it

### Scheduled Remote Checks

Configure the application to check OneDrive for changes on a schedule:

**Setting Up Scheduled Checks**:
1. Go to **File → Update Account Details**
2. Select the account
3. Set **Auto-Sync Interval** to your desired frequency (60-1440 minutes)
   - 60 minutes = check every hour
   - 240 minutes = check every 4 hours
   - 1440 minutes = check once per day
4. Leave blank to disable scheduled checks

**How It Works**:
- At the specified interval, the application checks OneDrive for changes
- If changes are found, a sync automatically starts
- This ensures you get updates even if the files weren't changed on your computer

**Tip**: Set this to 240-360 minutes (4-6 hours) for a good balance between freshness and battery/network usage.

### Disabling Automatic Sync

**File Watching**: Cannot be disabled (resumes after next manual sync)

**Scheduled Checks**: Set the interval to blank/empty in account settings

---

## Viewing Sync History and Logs

### Sync History

Access via **File → View Sync History**:

**What You'll See**:
- List of all sync sessions with date/time
- For each session:
  - Start and end times
  - Total files processed
  - Files uploaded, downloaded, and deleted
  - Any errors that occurred
  - Total data transferred

**Filtering**:
- Select an account to see only its sync history
- Search for specific dates or file paths

**Uses**:
- Verify when files were last synced
- Track how much data has been transferred
- Investigate sync errors or unexpected changes

### Debug Logs

Access via **File → View Debug Logs**:

**Prerequisites**: Debug Logging must be enabled in account settings

**What You'll See**:
- Detailed technical information about sync operations
- Performance metrics
- Error details and stack traces
- Internal state information

**When to Use**:
- Troubleshooting persistent sync issues
- Providing information to support
- Understanding performance problems

---

## Troubleshooting - Basic

### Problem: No Sync History or Logs Appear

**Cause**: The account isn't configured to record this information.

**Solution**:
1. Go to **File → Update Account Details**
2. Select the account
3. Enable **Detailed Sync Logging** (for sync history)
4. Enable **Debug Logging** (for debug logs)
5. Run a new sync
6. Check the history/logs again

### Problem: Sync Takes Very Long

**Cause**: Syncing many files or large files naturally takes time.

**Solutions**:
- **Check progress**: Look at the number of files and data being transferred
- **Increase parallel transfers**: Go to account settings and increase "Max Parallel Uploads/Downloads" to 8-10
- **Be patient with large files**: A 2GB video will take time on any connection
- **Check your internet speed**: The sync can only go as fast as your connection allows

### Problem: Files Not Syncing

**Possible Causes and Solutions**:

1. **Folders Not Selected**:
   - Check that the folders containing your files are checked in the folder tree
   - Click "Start Sync" after selecting folders

2. **File Watching Disabled**:
   - Run a manual sync to re-enable automatic file watching
   - Check the account status shows "Auto-sync enabled"

3. **Authentication Expired**:
   - If you see an authentication error, remove and re-add the account
   - You may need to sign in again

### Problem: "⚠️ View Conflicts" Button Appears

**This Is Normal**: It means files changed in both places and need your attention.

**Solution**:
1. Click the **"⚠️ View Conflicts"** button
2. Review each conflict
3. Choose a resolution strategy for each (Keep Local, Keep Remote, or Keep Both)
4. Click **"Resolve All"**
5. The button will disappear after conflicts are resolved

### Problem: Can't Find My Files

**Check These Locations**:

1. **Local Sync Path**:
   - Go to **File → Update Account Details**
   - Select the account
   - Note the "Local Sync Path" shown
   - Open that folder in File Explorer/Finder

2. **Folder Selection**:
   - Ensure the folders containing your files are checked in the folder tree
   - Uncheck folders won't sync their contents

### Problem: Application Closes Unexpectedly

**Immediate Steps**:
1. Restart the application
2. Check if any syncs are in progress
3. Wait for syncs to complete

**If It Keeps Happening**:
- Enable Debug Logging (File → Update Account Details)
- Reproduce the crash
- Check the debug logs (File → View Debug Logs)
- Contact support with the error details

---

## Troubleshooting - Advanced

### Database and Data Locations

**Database Location**:
- **Windows**: `C:\Users\[YourName]\AppData\Local\AStarOneDrive\sync.db`
- **macOS**: `~/Library/Application Support/AStarOneDrive/sync.db`
- **Linux**: `~/.local/share/AStarOneDrive/sync.db`

This database stores:
- Account information
- Sync state and progress
- Folder selections
- Conflict history
- Sync history (if enabled)
- Debug logs (if enabled)

### Resetting the Application

**Warning**: This deletes all accounts, settings, and sync history. Your local files remain untouched.

**Steps**:
1. Close the application completely
2. Delete the database file (see locations above)
3. Restart the application
4. Add your accounts again
5. Reconfigure folder selections

### Understanding Sync State

The application tracks sync state for each file:
- **Synced**: File is up-to-date in both locations
- **Pending Upload**: Local changes waiting to be uploaded
- **Pending Download**: Remote changes waiting to be downloaded
- **Failed**: An error occurred (will retry on next sync)
- **Conflict**: Needs user resolution

### Manual File Cleanup

If you need to manually fix sync issues:

**Removing a File from Sync**:
1. Stop any running syncs
2. Delete the file from BOTH locations (local and OneDrive)
3. Run a sync to confirm both sides are clean
4. Re-create or re-download the file as needed

**Forcing a Re-Sync**:
1. Close the application
2. Delete the database (see "Resetting the Application")
3. Start the application
4. Add your account
5. Select folders to sync
6. Run a full sync (this will download everything fresh)

### Network and Connectivity Issues

**Symptoms**:
- Syncs start but never complete
- Frequent timeout errors
- Slow transfer speeds

**Troubleshooting**:
1. **Check Internet Connection**: Ensure you can access onedrive.com in a browser
2. **Firewall/Antivirus**: Ensure the application isn't blocked
3. **Proxy Settings**: If behind a corporate proxy, ensure it's properly configured
4. **Reduce Parallel Transfers**: Lower "Max Parallel Uploads/Downloads" to 1-2
5. **Try Smaller Batches**: Reduce "Max Items in Batch" to 10-20

### Performance Optimization

**For Large Number of Small Files** (e.g., source code, photos):
- Increase "Max Parallel Uploads/Downloads" to 8-10
- Keep "Max Items in Batch" at 50-100

**For Large Files** (e.g., videos, backups):
- Reduce "Max Parallel Uploads/Downloads" to 1-2
- This prevents timeouts and memory issues

**For Poor Internet Connections**:
- Reduce "Max Parallel Uploads/Downloads" to 1-2
- Reduce "Max Items in Batch" to 10-20
- Consider syncing during off-peak hours

### Conflict Resolution Strategies in Detail

**When to Use Each Strategy**:

1. **Keep Local**:
   - You know the local version is correct
   - The remote changes are unwanted
   - You're the primary editor of the file

2. **Keep Remote**:
   - Someone else made important changes
   - Your local changes are obsolete
   - You want the latest version from the team

3. **Keep Both**:
   - You're not sure which version is correct
   - Both versions contain valuable changes
   - You want to manually merge the files later
   - Safety first - you can always delete one later

**Understanding Keep Both Naming**:
- Local file is renamed: `document_conflict_2026-01-08_14-30-45.docx`
- Remote file downloads with original name: `document.docx`
- Timestamp format: YYYY-MM-DD_HH-MM-SS

### File Hash Conflicts

In rare cases, files may have the same size and modification time but different content (hash mismatch). This can happen with:
- Files modified without changing size
- Corrupted downloads
- Sync interrupted mid-transfer

**Resolution**:
- Always use "Keep Remote" or "Keep Both" to get the correct file
- Check the file content after resolution
- If problems persist, delete and re-download the file

### Advanced Logging

**Reading Sync History**:
- Look for patterns in failures (same files, same times)
- Check transfer speeds to identify network issues
- Note the duration of each phase (scanning, uploading, downloading)

**Reading Debug Logs**:
- Look for ERROR or WARNING level messages
- Check for repeated errors on the same files
- Note any stack traces for sharing with support

### When to Contact Support

Contact support if you experience:
- Data loss or file corruption
- Repeated crashes
- Authentication problems that persist after re-adding the account
- Syncs that never complete despite good internet
- Unexpected behavior not covered in this manual

**Information to Provide**:
1. What you were trying to do
2. What actually happened
3. Error messages (screenshots are helpful)
4. Debug logs (enable Debug Logging first)
5. Your operating system and version

---

## Frequently Asked Questions

### Can I sync the same folder from multiple accounts?

**No**. Each account needs its own local folder. This prevents conflicts and confusion about which account owns which files.

### What happens if I manually move files in my local folder?

The application detects moves as "delete old file, create new file." On the next sync:
- The file at the old location is deleted in OneDrive
- The file at the new location is uploaded as new to OneDrive

### Can I use this with OneDrive for Business?

**Yes**! The application works with:
- Personal Microsoft accounts (Hotmail, Outlook.com)
- Work or School accounts (Office 365, Microsoft 365)
- OneDrive for Business

### Does this replace the official OneDrive sync client?

This application is designed for users who need to sync multiple accounts or have more control over what syncs. It can be used alongside or instead of the official client, depending on your needs.

### How much space does the database use?

Typically 10-50 MB depending on:
- Number of files being synced
- Whether logging is enabled
- How many sync sessions are in history

### Can I sync to an external drive?

**Yes**, but with cautions:
- Ensure the drive is always connected when syncing
- Format the drive with a file system that supports modification times (NTFS on Windows, APFS on macOS, ext4 on Linux)
- Avoid FAT32 - it doesn't reliably store modification times

### What happens if I edit a file while it's being synced?

The sync will:
- Complete uploading the version that was current when sync started
- On the next sync, detect your new changes and upload them again

This is safe but may result in multiple versions in OneDrive's version history.

### How do I exclude certain files (like .tmp or .cache)?

Currently, the application syncs all files in selected folders. File filtering will be added in a future version. For now:
- Don't select folders containing files you don't want to sync
- Or, move files you don't want synced to unselected folders

### Can I sync to multiple computers?

**Yes**! You can:
1. Install the application on multiple computers
2. Add the same account on each
3. Select the same folders
4. Each computer maintains its own local copy

**Important**: Be careful editing the same file on multiple computers - this can create conflicts.

### What's the maximum file size I can sync?

The application supports files up to OneDrive's limit (currently 250 GB for OneDrive Personal, 15 GB for OneDrive for Business without special features).

**Practical considerations**:
- Very large files take a long time to transfer
- Ensure you have enough local disk space
- Large files are more likely to fail if your internet connection is unstable

### How do I know if automatic sync is working?

After running a manual sync successfully:
- The account status shows "Auto-sync enabled"
- When you save a file locally, a sync starts automatically after a few seconds
- If scheduled checks are configured, they run at the specified interval

### Can I pause automatic syncing temporarily?

**File watching**: No, but you can stop any running sync with "Cancel Sync"

**Scheduled checks**: Edit the account and clear the Auto-Sync Interval field, or close the application

---

## Getting Help

### Documentation

- **This Manual**: For general usage and troubleshooting
- **Sync Algorithm Overview** (`docs/sync-algorithm-overview.md`): Technical details about how syncing works
- **Debug Logging Guide** (`docs/debug-logging-usage-guide.md`): How to enable and use debug logging

### Support Channels

[Support information will be added here based on your organization's support structure]

### Providing Feedback

Your feedback helps improve the application! Please share:
- Features you'd like to see
- Usability issues
- Bugs or unexpected behavior
- Documentation improvements

---

## Version History

**Version 3.0** (January 2026)
- Multi-account support
- Selective folder sync
- Enhanced conflict resolution
- Automatic file watching
- Scheduled remote checks
- Detailed sync history and debug logging
- Performance improvements

---

**Thank you for using OneDrive Multi-Account Sync!**

This application is designed to make managing multiple OneDrive accounts simple and reliable. If you have any questions or suggestions, please don't hesitate to reach out.
