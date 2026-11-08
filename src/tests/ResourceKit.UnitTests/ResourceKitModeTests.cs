using Aspire.Hosting;

namespace Purview.Aspire.ResourceKit;

public sealed class ResourceKitModeTests
{
	[Test]
	public async Task IsEnabledFor_GivenAll_WhenPublishing_ReturnsTrue()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Publish);

		// Act
		var isEnabled = ResourceKitMode.All.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsTrue();
	}

	[Test]
	public async Task IsEnabledFor_GivenAll_WhenRunningInProduction_ReturnsTrue()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(
			DistributedApplicationOperation.Run,
			environmentName: "Production"
		);

		// Act
		var isEnabled = ResourceKitMode.All.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsTrue();
	}

	[Test]
	public async Task IsEnabledFor_GivenPublish_WhenPublishing_ReturnsTrue()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Publish);

		// Act
		var isEnabled = ResourceKitMode.Publish.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsTrue();
	}

	[Test]
	public async Task IsEnabledFor_GivenPublish_WhenRunningLocally_ReturnsFalse()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Run);

		// Act
		var isEnabled = ResourceKitMode.Publish.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsFalse();
	}

	[Test]
	public async Task IsEnabledFor_GivenRun_WhenRunningInProduction_ReturnsTrue()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(
			DistributedApplicationOperation.Run,
			environmentName: "Production"
		);

		// Act
		var isEnabled = ResourceKitMode.Run.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsTrue();
	}

	[Test]
	public async Task IsEnabledFor_GivenRun_WhenPublishing_ReturnsFalse()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Publish);

		// Act
		var isEnabled = ResourceKitMode.Run.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsFalse();
	}

	[Test]
	public async Task IsEnabledFor_GivenLocalDevelopment_WhenRunningInDevelopment_ReturnsTrue()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(
			DistributedApplicationOperation.Run,
			environmentName: "Development"
		);

		// Act
		var isEnabled = ResourceKitMode.LocalDevelopment.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsTrue();
	}

	[Test]
	public async Task IsEnabledFor_GivenLocalDevelopment_WhenRunningInProduction_ReturnsFalse()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(
			DistributedApplicationOperation.Run,
			environmentName: "Production"
		);

		// Act
		var isEnabled = ResourceKitMode.LocalDevelopment.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsFalse();
	}

	[Test]
	public async Task IsEnabledFor_GivenLocalDevelopment_WhenPublishing_ReturnsFalse()
	{
		// Arrange
		var builder = TestApplicationBuilderFactory.Create(DistributedApplicationOperation.Publish);

		// Act
		var isEnabled = ResourceKitMode.LocalDevelopment.IsEnabledFor(builder);

		// Assert
		await Assert.That(isEnabled).IsFalse();
	}

	[Test]
	public async Task IsEnabledFor_GivenNullBuilder_ThrowsArgumentNullException()
	{
		// Arrange
		IDistributedApplicationBuilder builder = null!;

		// Act
		var exception = await Assert
			.That(() => ResourceKitMode.All.IsEnabledFor(builder))
			.ThrowsExactly<ArgumentNullException>();

		// Assert
		var argumentNullException = await Assert.That(exception).IsNotNull();
		await Assert.That(argumentNullException.ParamName).IsEqualTo("builder");
	}
}
