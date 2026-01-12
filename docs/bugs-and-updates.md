# Bugs and potential updates

[ ] Add AStar Icon - seems to be there but doesn't display
[ ] Empty remote folders are not created locally
[ ] Not proven but imagine empty local folders are also not created
[ ] account.EnableDetailedSyncLogging is only checked once (and thus not exactly detailed) - in SyncEngine
[ ] update UI to include "Updating database with X new items from OneDrive" or similar
[ ] batch db updates to reduce load / wasted time
[ ] add scheduled clean-up of debug logs and sync logs
[ ] sync logs doesn't include a great deal of data - investigate
[ ] sync view overlay can be closed without affecting the current downloads / uploads so tell the user that!
[ ] also tell them that selecting additional folders when a sync is running will not affect the current sync!
[ ] there is no way to re-open the sync view overlay - as it has ETA etc. add one (especially as the files being down/uploaded could be large and therefore the x of y could sit on the same number for a while)
[ ] also add ETA to the main sync tree view
[ ] add log for the OneDrive calls to see whether efficient or inefficient
[x] if account login is cancelled, the UI never resets / cancels the attempt
[ ] cancel function needs rethinking (again) as it can produce 100,000s of exceptions for a large sync!
[ ] conflict adds a record to the db but the download errors saying not found... also, the original, local file is not updated...
[x] need to disable the "Start Sync" button when the "loading folders" is running
