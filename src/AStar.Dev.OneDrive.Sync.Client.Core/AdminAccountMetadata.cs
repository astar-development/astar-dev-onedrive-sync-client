namespace AStar.Dev.OneDrive.Sync.Client.Core;

/// <summary>
///   Contains metadata related to the admin account used for syncing OneDrive data. This class provides a centralized location for storing constants and other relevant information about the admin account, such as its unique identifier. By using a dedicated class for this purpose, we can ensure that any references to the admin account's metadata are consistent throughout the codebase and can be easily updated if necessary.
/// </summary>
public static class AdminAccountMetadata
{
    /// <summary>
    ///  The unique identifier for the admin account used for syncing OneDrive data. This constant can be used throughout the codebase whenever we need to reference the admin account, ensuring consistency and making it easier to update if the identifier ever needs to change.
    /// </summary>
    public const string HashedAccountId = "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9";
}
