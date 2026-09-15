using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MILL06.ViewModels;

namespace MILL90.Tests.MILL06.ViewModels.Tests {
    [TestClass]
    public class MenuArchitectureTests : DITestBase {
        protected override void ConfigureServices(IServiceCollection services) {
            base.ConfigureServices(services);

            // Invoke the registration extension from the MILL06.ViewModels project,
            // which executes your AddManualRegistrations method under the hood.
            services.AddRegistrations();
        }

        [TestMethod]
        public void MenuRegistry_Builds_Expected_Schema_Hierarchy() {
            var registry = Provider.GetRequiredService<MenuRegistry>();
            var menuTree = registry.GetDefaultMenu();

            Assert.IsNotNull(menuTree, "Menu tree should not be null.");
            Assert.IsTrue(menuTree.Count > 0, "The generated menu should contain at least one schema group.");

            var dboGroup = menuTree.OfType<MenuGroupViewModel>().FirstOrDefault(g => g.Title == "dbo");
            Assert.IsNotNull(dboGroup, "Should have generated a 'dbo' group.");
            Assert.IsTrue(dboGroup.Children.Count > 0, "The dbo group should contain entity leaf nodes.");

            var customerLeaf = dboGroup.Children.OfType<MenuItemViewModel>().FirstOrDefault(leaf => leaf.Title == "Customers"); Assert.IsNotNull(customerLeaf, "Should have generated a 'Customers' menu item.");
            Assert.AreEqual("CustomersViewModel", customerLeaf.TargetViewModelName);
        }

        [TestMethod]
        public void MenuViewModel_LazyLoads_ObservableCollection_From_Registry() {
            var menuVm = new MenuViewModel();
            var observableItems = menuVm.MenuItems;

            Assert.IsNotNull(observableItems);
            Assert.IsTrue(observableItems.Count > 0, "MenuItems should lazy-load the hierarchy from the Registry.");

            var firstItem = observableItems.First();
            Assert.IsInstanceOfType(firstItem, typeof(MenuNodeViewModel));
            Assert.AreEqual(typeof(MenuGroupViewModel), firstItem.GetType());
        }
    }
}