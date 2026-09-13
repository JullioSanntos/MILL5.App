using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MILL09.Models;

namespace MILL90.Tests.MILL09.Models.Tests;

[TestClass]
public class MainModelTests : DITestBase {
    // CLASS-LEVEL OVERRIDE: Applies to all tests in this class
    protected override void ConfigureServices(IServiceCollection services) {
        base.ConfigureServices(services);

        // Force MainModel to be Transient for tests so state doesn't leak between test runs
        services.Replace(ServiceDescriptor.Transient<MainModel>(_ => new MainModel()));
    }

    [TestMethod]
    public void Test_Using_Standard_Folder_And_Class_Rules() {
        // Arrange
        var model1 = Provider.GetRequiredService<MainModel>();

        // Act & Assert
        Assert.IsNotNull(model1);

        var model2 = Provider.GetRequiredService<MainModel>();
        Assert.IsNotNull(model2);

        Assert.AreNotEqual(model1, model2);
    }

    [TestMethod]
    public void Test_Initial_State_And_Modification() {
        // Arrange
        var model = Provider.GetRequiredService<MainModel>();

        // Assert Initial
        Assert.AreEqual("DefaultContext", model.CurrentContext);

        // Act
        model.CurrentContext = "TestContext";

        // Assert Modified
        Assert.AreEqual("TestContext", model.CurrentContext);
    }

    [TestMethod]
    public void Test_Static_Instance_Routes_Through_Locator() {
        // Arrange: Rebuild specifically as a Singleton for this single test 
        // to verify the static property behavior aligns with the container.
        RebuildProviderWithOverrides(services => {
            services.Replace(ServiceDescriptor.Singleton<MainModel>(_ => new MainModel()));
        });

        // Act
        var directResolve = Provider.GetRequiredService<MainModel>();
        var staticResolve = MainModel.Instance;

        // Assert
        Assert.IsNotNull(staticResolve);
        Assert.AreEqual(directResolve, staticResolve, "The static Instance property did not route to the expected Singleton in the ServiceLocator.");
    }
}