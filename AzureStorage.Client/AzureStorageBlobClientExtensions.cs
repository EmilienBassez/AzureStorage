using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureStorage.Client;

internal class AzureStorageClientRegistrationStore
{
	public Dictionary<Type, HashSet<string>> Registrations { get; } = new();
}

public static class AzureStorageClientExtensions
{
	public static IServiceCollection AddAzureStorageBlobClient(this IServiceCollection services, Action<AzureStorageClientOptions> configure, BlobClientOptions? blobClientOptions = null)
	{
		return services.AddAzureStorageClient<BlobServiceClient, AzureStorageClientOptions, AzureStorageClientOptionsValidator>(
			configure,
			(options, credential) => new BlobServiceClient(new Uri($"https://{options.StorageAccountName}.blob.core.windows.net"), credential, blobClientOptions)
		);
	}

	public static IServiceCollection AddAzureStorageTableClient(this IServiceCollection services, Action<AzureStorageClientOptions> configure, TableClientOptions? tableClientOptions = null)
	{
		return services.AddAzureStorageClient<TableServiceClient, AzureStorageClientOptions, AzureStorageClientOptionsValidator>(
			configure,
			(options, credential) => new TableServiceClient(new Uri($"https://{options.StorageAccountName}.table.core.windows.net"), credential, tableClientOptions)
        );
	}

	private static IServiceCollection AddAzureStorageClient<TClient, TOptions, TValidator>(
		this IServiceCollection services,
		Action<TOptions> configure,
		Func<TOptions, TokenCredential, TClient> createClient)
		where TClient : class
		where TOptions : AzureStorageClientOptions, new()
		where TValidator : AbstractValidator<TOptions>, new()
	{
		var clientOptions = new TOptions();
		configure(clientOptions);

		var validator = new TValidator();
		validator.ValidateAndThrow(clientOptions);

		var credentialOptions = new DefaultAzureCredentialOptions();
		if(!string.IsNullOrWhiteSpace(clientOptions.ManagedIdentityClientId))
		{
			credentialOptions.ManagedIdentityClientId = clientOptions.ManagedIdentityClientId;
		}

		var credential = new DefaultAzureCredential(credentialOptions);

		var clientType = typeof(TClient);
		var clientKey = string.IsNullOrWhiteSpace(clientOptions.Key) ? null : clientOptions.Key;

		var store = GetOrCreateStore(services);

		if(!store.Registrations.TryGetValue(clientType, out var keys))
		{
			keys = new HashSet<string?>();
			store.Registrations[clientType] = keys;
		}

		if(!keys.Add(clientKey))
		{
			if(clientKey == null)
			{
				throw new InvalidOperationException($"Cannot register a second {clientType.Name} without a key.");
			}

			throw new InvalidOperationException($"A {clientType.Name} is already registered with the key '{clientKey}'.");
		}

		if(clientKey == null)
		{
			var existingNonKeyed = services.FirstOrDefault(sd => sd.ServiceType == clientType && sd.ImplementationFactory != null);
			if(existingNonKeyed != null)
			{
				throw new InvalidOperationException($"Cannot register a second {clientType.Name} without a key.");
			}

			services.AddSingleton(_ => createClient(clientOptions, credential));
		}
		else
		{
			services.AddKeyedSingleton(
				clientKey,
				(_, _) => createClient(clientOptions, credential));
		}
		return services;
	}

	private static AzureStorageClientRegistrationStore GetOrCreateStore(IServiceCollection services)
	{
		var store = services.FirstOrDefault(sd => sd.ServiceType == typeof(AzureStorageClientRegistrationStore))?.ImplementationInstance as AzureStorageClientRegistrationStore;
		if(store == null)
		{
			store = new AzureStorageClientRegistrationStore();
			services.AddSingleton(store);
		}
		return store;
	}
}