using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MILL80.Infrastructure;
using System;

namespace MILL90.Tests;

public abstract class DITestBase {
    public IServiceProvider Provider { get; private set; } = null!;

    [TestInitialize]
    public void TestInitialize() {
        RebuildProviderWithOverrides(_ => { });
    }

    /// <summary>
    /// Applies baseline registrations for the test environment.
    /// Override this in test classes to apply class-wide service replacements.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services) {
        // Example: Trigger your scanner to load the default application registrations
        // global::MILL06.ViewModels.RegistrationScanner.ApplyRegistrations(services, typeof(MainViewModel).Assembly);
    }

    /// <summary>
    /// Rebuilds the DI container mid-test for highly specific service overrides.
    /// </summary>
    protected void RebuildProviderWithOverrides(Action<IServiceCollection> overrides) {
        var services = new ServiceCollection();

        ConfigureServices(services);
        overrides(services);

        Provider = services.BuildServiceProvider();
        ServiceLocator.Initialize(Provider);
    }
}