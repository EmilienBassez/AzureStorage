using FluentValidation;

namespace AzureStorage.Client;

public  class AzureStorageClientOptions
{
	public string StorageAccountName { get; set; } = string.Empty;
	public string? Key { get; set; }
	public string? ManagedIdentityClientId { get; set; }
}

internal class AzureStorageClientOptionsValidator : AbstractValidator<AzureStorageClientOptions>
{
	public AzureStorageClientOptionsValidator()
	{
		RuleFor(x => x.StorageAccountName)
			.NotEmpty()
			.WithMessage("Storage account name cannot be empty.");

		RuleFor(x => x.StorageAccountName)
			.Matches("^[a-z0-9]{3,24}$")
			.When(x => !string.IsNullOrEmpty(x.StorageAccountName))
			.WithMessage("Storage account name must be 3 to 24 lowercase alphanumeric characters.");
	}
}