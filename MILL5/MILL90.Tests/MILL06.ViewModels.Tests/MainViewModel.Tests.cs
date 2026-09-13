using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MILL06.ViewModels;

namespace MILL90.Tests.MILL06.ViewModels.Tests;

[TestClass]
public class MainDITests : DITestBase {
    // CLASS-LEVEL OVERRIDE: Applies to all tests in this class
    protected override void ConfigureServices(IServiceCollection services) {
        base.ConfigureServices(services);
        services.Replace(ServiceDescriptor.Transient<MainViewModel>(_ => new MainViewModel()));
    }

    [TestMethod]
    public void Test_Using_Standard_Folder_And_Class_Rules() {
        // Arrange: Provider is already built by [TestInitialize]
        var vm1 = Provider.GetRequiredService<MainViewModel>();

        // Act & Assert
        Assert.IsNotNull(vm1);

        var vm2 = Provider.GetRequiredService<MainViewModel>();
        Assert.IsNotNull(vm2);

        Assert.AreNotEqual(vm1, vm2);
    }

    [TestMethod]
    public void Test_Using_Highly_Specific_Unit_Test_Override() {
        // Arrange: Override a specific service just for this single test
        RebuildProviderWithOverrides(services => {
            // Remove the mock logger from the class level, add a failing logger
            // var descriptor = services.First(d => d.ServiceType == typeof(ILogger));
            // services.Remove(descriptor);
            // services.AddTransient<ILogger, FailingLogger>();
        });

        // Act: This ViewModel now uses the FailingLogger
        var vm = Provider.GetRequiredService<MainViewModel>();

        // Assert
        Assert.IsNotNull(vm);
    }
}