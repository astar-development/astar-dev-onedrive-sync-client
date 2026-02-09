namespace AStar.Dev.OneDrive.Client.Core;

/// <summary>
///   Contains metadata related to the admin account used for syncing OneDrive data. This class provides a centralized location for storing constants and other relevant information about the admin account, such as its unique identifier. By using a dedicated class for this purpose, we can ensure that any references to the admin account's metadata are consistent throughout the codebase and can be easily updated if necessary.
/// </summary>
public static class AdminAccountMetadata
{
    /// <summary>
    ///  The unique identifier for the admin account used for syncing OneDrive data. This should be a hashed value to avoid storing personally identifiable information. This constant can be used throughout the codebase whenever we need to reference the admin account, ensuring consistency and making it easier to update if the identifier ever needs to change.
    /// </summary>
    public const string AccountId = "e29a2798-c836-4854-ac90-a3f2d37aae26";
}
