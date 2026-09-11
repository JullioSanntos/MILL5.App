using MILL06.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MILL90.Tests.MILL06.Data.Tests {
    [TestClass]
    public class RepositoryTests {
        [TestMethod]
        public void InstantiationTests() {
            var target = new Repository();
            Assert.IsNotNull(target);
        }
    }
}
