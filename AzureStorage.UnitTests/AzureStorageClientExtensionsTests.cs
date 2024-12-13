using Azure.Data.Tables;
using Azure.Storage.Blobs;
using AzureStorage.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AzureStorage.UnitTests;

public class AzureStorageClientExtensionsTests
{
    [Fact]
    public void AddAzureStorageClient_WithoutKey_ShouldRegisterSingletonBlobServiceClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAzureStorageBlobClient(options =>
        {
            options.StorageAccountName = "mystorageaccount";
        });

        var provider = services.BuildServiceProvider();
        var blobClient = provider.GetService<BlobServiceClient>();

        // Assert
        blobClient.Should().NotBeNull("a BlobServiceClient should be registered");

        // Ensure singleton behavior
        var blobClient2 = provider.GetService<BlobServiceClient>();
        blobClient.Should().BeSameAs(blobClient2, "it should be a singleton registration");
    }

    [Fact]
    public void AddAzureStorageClient_WithKey_ShouldRegisterKeyedBlobServiceClient()
    {
        // Arrange
        var services = new ServiceCollection();
        const string key = "AccountA";

        // Act
        services.AddAzureStorageBlobClient(options =>
        {
            options.StorageAccountName = "mystorageaccount";
            options.Key = key;
        });

        var provider = services.BuildServiceProvider();
        var blobClient = provider.GetRequiredKeyedService<BlobServiceClient>(key);

        // Assert
        blobClient.Should().NotBeNull("a keyed BlobServiceClient should be registered for the given key");

        // Attempting to resolve a non-existing key should fail
        Action act = () => provider.GetRequiredKeyedService<BlobServiceClient>("NonExistentKey");
        act.Should().Throw<InvalidOperationException>("no BlobServiceClient is registered for that key");
    }

    [Fact]
    public void AddAzureStorageClient_WithManagedIdentityClientId_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddAzureStorageBlobClient(options =>
        {
            options.StorageAccountName = "mystorageaccount";
            options.ManagedIdentityClientId = "SomeClientId";
        });

        // Assert
        act.Should().NotThrow();

        var provider = services.BuildServiceProvider();
        var blobClient = provider.GetService<BlobServiceClient>();
        blobClient.Should().NotBeNull("it should still register a BlobServiceClient normally");
    }

    [Fact]
    public void AddAzureStorageClient_BlobAndTable_WithKeys_ShouldRegisterAndResolveBoth()
    {
        // Arrange
        var services = new ServiceCollection();
        const string blobKey = "BlobAccount";
        const string tableKey = "TableAccount";

        // Register blob client with a key
        services.AddAzureStorageBlobClient(options =>
        {
            options.StorageAccountName = "blobaccountname";
            options.Key = blobKey;
        });

        // Register table client with a key
        services.AddAzureStorageTableClient(options =>
        {
            options.StorageAccountName = "tableaccountname";
            options.Key = tableKey;
        });

        var provider = services.BuildServiceProvider();

        // Act
        var keyedBlobClient = provider.GetRequiredKeyedService<BlobServiceClient>(blobKey);
        var keyedTableClient = provider.GetRequiredKeyedService<TableServiceClient>(tableKey);

        // Assert
        keyedBlobClient.Should().NotBeNull("A keyed BlobServiceClient should be registered for the blob key");
        keyedTableClient.Should().NotBeNull("A keyed TableServiceClient should be registered for the table key");

        keyedBlobClient.Uri.ToString().Should().Be("https://blobaccountname.blob.core.windows.net/");
        keyedTableClient.Uri.ToString().Should().Be("https://tableaccountname.table.core.windows.net/");
    }

    [Fact]
    public void AddAzureStorageClient_MultipleBlobAndTableClients_WithKeys_ShouldAllResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        const string blobKey1 = "BlobAccountA";
        const string blobKey2 = "BlobAccountB";
        const string tableKey1 = "TableAccountA";
        const string tableKey2 = "TableAccountB";

        // Register multiple blob clients
        services.AddAzureStorageBlobClient(options =>
        {
            options.StorageAccountName = "blobaccounta";
            options.Key = blobKey1;
        });
        services.AddAzureStorageBlobClient(options =>
        {
            options.StorageAccountName = "blobaccountb";
            options.Key = blobKey2;
        });

        // Register multiple table clients
        services.AddAzureStorageTableClient(options =>
        {
            options.StorageAccountName = "tableaccounta";
            options.Key = tableKey1;
        });
        services.AddAzureStorageTableClient(options =>
        {
            options.StorageAccountName = "tableaccountb";
            options.Key = tableKey2;
        });

        var provider = services.BuildServiceProvider();

        // Act
        var blobClientA = provider.GetRequiredKeyedService<BlobServiceClient>(blobKey1);
        var blobClientB = provider.GetRequiredKeyedService<BlobServiceClient>(blobKey2);
        var tableClientA = provider.GetRequiredKeyedService<TableServiceClient>(tableKey1);
        var tableClientB = provider.GetRequiredKeyedService<TableServiceClient>(tableKey2);

        // Assert
        blobClientA.Should().NotBeNull("A BlobServiceClient should be registered for blobKey1");
        blobClientB.Should().NotBeNull("A BlobServiceClient should be registered for blobKey2");
        tableClientA.Should().NotBeNull("A TableServiceClient should be registered for tableKey1");
        tableClientB.Should().NotBeNull("A TableServiceClient should be registered for tableKey2");

        blobClientA.Uri.ToString().Should().Be("https://blobaccounta.blob.core.windows.net/");
        blobClientB.Uri.ToString().Should().Be("https://blobaccountb.blob.core.windows.net/");
        tableClientA.Uri.ToString().Should().Be("https://tableaccounta.table.core.windows.net/");
        tableClientB.Uri.ToString().Should().Be("https://tableaccountb.table.core.windows.net/");
    }

    private ServiceCollection CreateServices() => new ServiceCollection();

    private void RegisterBlobClient(IServiceCollection services, string accountName, string? key = null)
    {
	    services.AddAzureStorageBlobClient(options =>
	    {
		    options.StorageAccountName = accountName;
		    options.Key = key;
	    });
    }

    private void RegisterTableClient(IServiceCollection services, string accountName, string? key = null)
    {
	    services.AddAzureStorageTableClient(options =>
	    {
		    options.StorageAccountName = accountName;
		    options.Key = key;
	    });
    }

    [Fact]
    public void AddAzureStorageClient_BlobNonKeyedSecondRegistration_ShouldThrow()
    {
        // Arrange
        var services = CreateServices();
        RegisterBlobClient(services, "mystorageaccount");

        // Act
        var act = () => RegisterBlobClient(services, "anotherstorageaccount");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot register a second BlobServiceClient without a key*");
    }

    [Fact]
    public void Table_NonKeyedSecondRegistration_ShouldThrow()
    {
        // Arrange
        var services = CreateServices();
        RegisterTableClient(services, "mystorageaccount");

        // Act
        var act = () => RegisterTableClient(services, "anotherstorageaccount");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot register a second TableServiceClient without a key*");
    }

    [Fact]
    public void AddAzureStorageClient_BlobSameKeyTwice_ShouldThrow()
    {
        // Arrange
        var services = CreateServices();
        RegisterBlobClient(services, "mystorageaccount", "MyKey");

        // Act
        var act = () => RegisterBlobClient(services, "anotherstorageaccount", "MyKey");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already registered with the key 'MyKey'*");
    }

    [Fact]
    public void AddAzureStorageClient_TableSameKeyTwice_ShouldThrow()
    {
        // Arrange
        var services = CreateServices();
        RegisterTableClient(services, "mystorageaccount", "TableKey");

        // Act
        var act = () => RegisterTableClient(services, "anotherstorageaccount", "TableKey");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already registered with the key 'TableKey'*");
    }

    [Fact]
    public void AddAzureStorageClient_BlobFirstNoKeySecondWithKey_ShouldSucceed()
    {
        // Arrange
        var services = CreateServices();
        RegisterBlobClient(services, "mystorageaccount");
        RegisterBlobClient(services, "anotherstorageaccount", "AnotherKey");

        // Act
        var provider = services.BuildServiceProvider();
        var blobClient = provider.GetService<BlobServiceClient>();
        var blobClientWithKey = provider.GetRequiredKeyedService<BlobServiceClient>("AnotherKey");

        // Assert
        blobClient.Should().NotBeNull("the first non-keyed BlobServiceClient should exist");
        blobClientWithKey.Should().NotBeNull("the keyed BlobServiceClient should exist");
        blobClientWithKey.Should().NotBeSameAs(blobClient, "they should be distinct instances");
    }

    [Fact]
    public void AddAzureStorageClient_TableFirstNoKeySecondWithKey_ShouldSucceed()
    {
        // Arrange
        var services = CreateServices();
        RegisterTableClient(services, "mystorageaccount");
        RegisterTableClient(services, "anotherstorageaccount", "AnotherKey");

        // Act
        var provider = services.BuildServiceProvider();
        var tableClient = provider.GetService<TableServiceClient>();
        var tableClientWithKey = provider.GetRequiredKeyedService<TableServiceClient>("AnotherKey");

        // Assert
        tableClient.Should().NotBeNull("the first non-keyed TableServiceClient should exist");
        tableClientWithKey.Should().NotBeNull("the keyed TableServiceClient should exist");
        tableClientWithKey.Should().NotBeSameAs(tableClient, "they should be distinct instances");
    }

    [Fact]
    public void AddAzureStorageClient_BlobDifferentKeys_ShouldSucceed()
    {
        // Arrange
        var services = CreateServices();
        RegisterBlobClient(services, "storageaccounta", "KeyA");
        RegisterBlobClient(services, "storageaccountb", "KeyB");

        // Act
        var provider = services.BuildServiceProvider();
        var blobClientA = provider.GetRequiredKeyedService<BlobServiceClient>("KeyA");
        var blobClientB = provider.GetRequiredKeyedService<BlobServiceClient>("KeyB");

        // Assert
        blobClientA.Should().NotBeNull("BlobServiceClient with KeyA should exist");
        blobClientB.Should().NotBeNull("BlobServiceClient with KeyB should exist");
        blobClientA.Should().NotBeSameAs(blobClientB, "Keyed clients should be distinct");
    }

    [Fact]
    public void AddAzureStorageClient_TableDifferentKeys_ShouldSucceed()
    {
        // Arrange
        var services = CreateServices();
        RegisterTableClient(services, "tableaccounta", "TableKeyA");
        RegisterTableClient(services, "tableaccountb", "TableKeyB");

        // Act
        var provider = services.BuildServiceProvider();
        var tableClientA = provider.GetRequiredKeyedService<TableServiceClient>("TableKeyA");
        var tableClientB = provider.GetRequiredKeyedService<TableServiceClient>("TableKeyB");

        // Assert
        tableClientA.Should().NotBeNull("TableServiceClient with TableKeyA should exist");
        tableClientB.Should().NotBeNull("TableServiceClient with TableKeyB should exist");
        tableClientA.Should().NotBeSameAs(tableClientB, "Keyed clients should be distinct");
    }
}