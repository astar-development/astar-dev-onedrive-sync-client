using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

public class DeletionSyncServiceShould
{
    [Fact]
    public async Task ProcessRemoteToLocalDeletionsAsync_ShouldDeleteLocalFileAndDatabaseRecord()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var existingFiles = new List<DriveItemEntity>
        {
            new(accountId, "file1", "/Documents/file1.txt", "etag1", "ctag1", 100, 
                DateTimeOffset.UtcNow, false, false, true, null, "file1.txt", 
                @"C:\Sync\Documents\file1.txt", null, FileSyncStatus.Synced, SyncDirection.Download)
        }.AsReadOnly();
        
        var remotePathsSet = new HashSet<string>();
        var localPathsSet = new HashSet<string> { "/Documents/file1.txt" };
        
        // Act
        await service.ProcessRemoteToLocalDeletionsAsync(
            accountId, existingFiles, remotePathsSet, localPathsSet, CancellationToken.None);
        
        // Assert
        await driveItemsRepo.Received(1).DeleteAsync("file1", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ProcessRemoteToLocalDeletionsAsync_ShouldContinueOnError()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var existingFiles = new List<DriveItemEntity>
        {
            new(accountId, "file1", "/Documents/file1.txt", "etag1", "ctag1", 100, 
                DateTimeOffset.UtcNow, false, false, true, null, "file1.txt", 
                @"C:\Sync\Documents\file1.txt", null, FileSyncStatus.Synced, SyncDirection.Download),
            new(accountId, "file2", "/Documents/file2.txt", "etag2", "ctag2", 200, 
                DateTimeOffset.UtcNow, false, false, true, null, "file2.txt", 
                @"C:\Sync\Documents\file2.txt", null, FileSyncStatus.Synced, SyncDirection.Download)
        }.AsReadOnly();
        
        var remotePathsSet = new HashSet<string>();
        var localPathsSet = new HashSet<string> { "/Documents/file1.txt", "/Documents/file2.txt" };
        
        _ = driveItemsRepo.DeleteAsync("file1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Database error")));
        
        // Act
        await service.ProcessRemoteToLocalDeletionsAsync(
            accountId, existingFiles, remotePathsSet, localPathsSet, CancellationToken.None);
        
        // Assert - both deletions should be attempted despite first one failing
        await driveItemsRepo.Received(1).DeleteAsync("file1", Arg.Any<CancellationToken>());
        await driveItemsRepo.Received(1).DeleteAsync("file2", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ProcessRemoteToLocalDeletionsAsync_ShouldNotDeleteIfNotSynced()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var existingFiles = new List<DriveItemEntity>
        {
            new(accountId, "file1", "/Documents/file1.txt", "etag1", "ctag1", 100, 
                DateTimeOffset.UtcNow, false, false, true, null, "file1.txt", 
                @"C:\Sync\Documents\file1.txt", null, FileSyncStatus.PendingUpload, SyncDirection.None)
        }.AsReadOnly();
        
        var remotePathsSet = new HashSet<string>();
        var localPathsSet = new HashSet<string> { "/Documents/file1.txt" };
        
        // Act
        await service.ProcessRemoteToLocalDeletionsAsync(
            accountId, existingFiles, remotePathsSet, localPathsSet, CancellationToken.None);
        
        // Assert - should not delete because file is not in Synced state
        await driveItemsRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ProcessLocalToRemoteDeletionsAsync_ShouldDeleteRemoteFileAndDatabaseRecord()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var allLocalFiles = new List<FileMetadata>
        {
            new("file1", accountId, "file1.txt", "/Documents/file1.txt", 100, 
                DateTimeOffset.UtcNow, @"C:\Sync\Documents\file1.txt", false, false, true, 
                null, "ctag1", "etag1", null, FileSyncStatus.Synced, SyncDirection.Download)
        };
        
        var remotePathsSet = new HashSet<string> { "/Documents/file1.txt" };
        var localPathsSet = new HashSet<string>();
        
        // Act
        await service.ProcessLocalToRemoteDeletionsAsync(
            accountId, allLocalFiles, remotePathsSet, localPathsSet, CancellationToken.None);
        
        // Assert
        await graphApiClient.Received(1).DeleteFileAsync(accountId, "file1", Arg.Any<CancellationToken>());
        await driveItemsRepo.Received(1).DeleteAsync("file1", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ProcessLocalToRemoteDeletionsAsync_ShouldContinueOnError()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var allLocalFiles = new List<FileMetadata>
        {
            new("file1", accountId, "file1.txt", "/Documents/file1.txt", 100, 
                DateTimeOffset.UtcNow, @"C:\Sync\Documents\file1.txt", false, false, true, 
                null, "ctag1", "etag1", null, FileSyncStatus.Synced, SyncDirection.Download),
            new("file2", accountId, "file2.txt", "/Documents/file2.txt", 200, 
                DateTimeOffset.UtcNow, @"C:\Sync\Documents\file2.txt", false, false, true, 
                null, "ctag2", "etag2", null, FileSyncStatus.Synced, SyncDirection.Download)
        };
        
        var remotePathsSet = new HashSet<string> { "/Documents/file1.txt", "/Documents/file2.txt" };
        var localPathsSet = new HashSet<string>();
        
        _ = graphApiClient.DeleteFileAsync(accountId, "file1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Network error")));
        
        // Act
        await service.ProcessLocalToRemoteDeletionsAsync(
            accountId, allLocalFiles, remotePathsSet, localPathsSet, CancellationToken.None);
        
        // Assert - both deletions should be attempted despite first one failing
        await graphApiClient.Received(1).DeleteFileAsync(accountId, "file1", Arg.Any<CancellationToken>());
        await graphApiClient.Received(1).DeleteFileAsync(accountId, "file2", Arg.Any<CancellationToken>());
        // Only file2 deletion should succeed and update database
        await driveItemsRepo.Received(1).DeleteAsync("file2", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task ProcessLocalToRemoteDeletionsAsync_ShouldOnlyDeleteFilesWithDriveItemId()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var allLocalFiles = new List<FileMetadata>
        {
            new("", accountId, "file1.txt", "/Documents/file1.txt", 100, 
                DateTimeOffset.UtcNow, @"C:\Sync\Documents\file1.txt", false, false, true, 
                null, null, null, null, FileSyncStatus.PendingUpload, SyncDirection.None)
        };
        
        var remotePathsSet = new HashSet<string>();
        var localPathsSet = new HashSet<string>();
        
        // Act
        await service.ProcessLocalToRemoteDeletionsAsync(
            accountId, allLocalFiles, remotePathsSet, localPathsSet, CancellationToken.None);
        
        // Assert - should not delete because file has no DriveItemId
        await graphApiClient.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await driveItemsRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task CleanupDatabaseRecordsAsync_ShouldDeleteAllProvidedItems()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var itemsToDelete = new List<DriveItemEntity>
        {
            new(accountId, "file1", "/Documents/file1.txt", "etag1", "ctag1", 100, 
                DateTimeOffset.UtcNow, false, false, true),
            new(accountId, "file2", "/Documents/file2.txt", "etag2", "ctag2", 200, 
                DateTimeOffset.UtcNow, false, false, true)
        };
        
        // Act
        await service.CleanupDatabaseRecordsAsync(accountId, itemsToDelete, CancellationToken.None);
        
        // Assert
        await driveItemsRepo.Received(1).DeleteAsync("file1", Arg.Any<CancellationToken>());
        await driveItemsRepo.Received(1).DeleteAsync("file2", Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task CleanupDatabaseRecordsAsync_ShouldHandleEmptyList()
    {
        // Arrange
        const string accountId = "test-account-id";
        var driveItemsRepo = Substitute.For<IDriveItemsRepository>();
        var graphApiClient = Substitute.For<IGraphApiClient>();
        
        var service = new DeletionSyncService(driveItemsRepo, graphApiClient);
        
        var itemsToDelete = new List<DriveItemEntity>();
        
        // Act
        await service.CleanupDatabaseRecordsAsync(accountId, itemsToDelete, CancellationToken.None);
        
        // Assert
        await driveItemsRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
