# Bugs and potential updates

[x] Add AStar Icon - seems to be there but doesn't display - Do NOT use .ico, use .png instead!
[ ] Empty remote folders are not created locally
[ ] Not proven but imagine empty local folders are also not created
[ ] account.EnableDetailedSyncLogging is only checked once (and thus not exactly detailed) - in SyncEngine
[ ] update UI to include "Updating database with X new items from OneDrive" or similar
[x] batch db updates to reduce load / wasted time
[x] add scheduled clean-up of debug logs and sync logs - currently hardcoded to 14 days & runs every 12 hours
[ ] sync logs doesn't include a great deal of data - investigate
[x] sync view overlay can be closed without affecting the current downloads / uploads so tell the user that!
[ ] also tell them that selecting additional folders when a sync is running will not affect the current sync!
[x] there is no way to re-open the sync view overlay - as it has ETA etc. add one (especially as the files being down/uploaded could be large and therefore the x of y could sit on the same number for a while)
[x] also add ETA to the main sync tree view
[ ] add log for the OneDrive calls to see whether efficient or inefficient
[x] if account login is cancelled, the UI never resets / cancels the attempt
[ ] cancel function needs rethinking (again) as it can produce 100,000s of exceptions for a large sync!
[ ] conflict adds a record to the db but the download errors saying not found... also, the original, local file is not updated...
[x] need to disable the "Start Sync" button when the "loading folders" is running
