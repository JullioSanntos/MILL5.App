using System;
using System.Collections.Generic;
using System.Text;

namespace MILL90.Tests.MILL80.Infrastructure.Tests {
    [TestClass]
    public class Class1Tests {
        [TestMethod]
        public void InstantiationTests() {
            var target = new global::MILL80.Infrastructure.Class1();
            Assert.IsNotNull(target);
        }
    }
}
