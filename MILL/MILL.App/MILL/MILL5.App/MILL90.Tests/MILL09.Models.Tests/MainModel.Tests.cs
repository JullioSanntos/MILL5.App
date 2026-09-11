using MILL09.Models;

namespace MILL90.Tests.MILL09.Models.Tests {
    [TestClass]
    public class MainModelTests {
        [TestMethod]
        public void InstantiationTest() {
            var target = new MainModel();
            Assert.IsNotNull(target);
        }
    }
}
