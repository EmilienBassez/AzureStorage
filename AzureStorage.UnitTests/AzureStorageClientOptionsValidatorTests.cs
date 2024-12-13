using AzureStorage.Client;
using FluentAssertions;

namespace AzureStorage.UnitTests;

public class AzureStorageClientOptionsValidatorTests
{
	private readonly AzureStorageClientOptionsValidator _validator;

	public AzureStorageClientOptionsValidatorTests()
	{
		_validator = new AzureStorageClientOptionsValidator();
	}

	[Fact]
	public void Validate_ShouldSucceed_WhenStorageAccountNameIsValid()
	{
		// Arrange
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "mystorageacct1" // Respecte la regex : 13 caractères, minuscules et chiffres
		};

		// Act
		var result = _validator.Validate(options);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_ShouldFail_WhenStorageAccountNameIsEmpty()
	{
		// Arrange
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = ""
		};

		// Act
		var result = _validator.Validate(options);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle()
		      .Which.ErrorMessage.Should().Contain("Storage account name cannot be empty.");
	}

	[Fact]
	public void Validate_ShouldSucceed_WhenOptionalPropertiesAreProvided()
	{
		// Arrange
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "validaccount123",
			Key = "SomeKey",
			ManagedIdentityClientId = "SomeClientId"
		};

		// Act
		var result = _validator.Validate(options);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_ShouldFail_WhenNameTooShort()
	{
		// Nom trop court: 2 caractères
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "ab"
		};

		var result = _validator.Validate(options);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle()
		      .Which.ErrorMessage.Should().Contain("must be 3 to 24");
	}

	[Fact]
	public void Validate_ShouldFail_WhenNameTooLong()
	{
		// Nom trop long : 25 caractères
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "abcdefghijklmnopqrstuvwxyz" // 26 chars
		};

		var result = _validator.Validate(options);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle()
		      .Which.ErrorMessage.Should().Contain("must be 3 to 24");
	}

	[Fact]
	public void Validate_ShouldFail_WhenNameHasInvalidChars()
	{
		// Contient des majuscules et un tiret
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "MyStorage-Account1"
		};

		var result = _validator.Validate(options);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle()
		      .Which.ErrorMessage.Should().Contain("must be 3 to 24 lowercase alphanumeric characters");
	}

	[Fact]
	public void Validate_ShouldFail_WhenNameHasUppercase()
	{
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "MYSTORAGE"
		};

		var result = _validator.Validate(options);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle()
		      .Which.ErrorMessage.Should().Contain("must be 3 to 24 lowercase alphanumeric characters");
	}

	[Fact]
	public void Validate_ShouldFail_WhenNameHasSpecialChars()
	{
		var options = new AzureStorageClientOptions
		{
			StorageAccountName = "storage_acc!"
		};

		var result = _validator.Validate(options);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle()
		      .Which.ErrorMessage.Should().Contain("must be 3 to 24 lowercase alphanumeric characters");
	}
}