using System;
using System.Collections.Generic;
using System.Text;

namespace MILL90.Tests.MILL12.Services.Tests {
    [TestClass]
    public class Class1Tests {
        [TestMethod]
        public void InstantiationTests() {
            var target = new global::MILL12.Services.Class1();
            Assert.IsNotNull(target);
        }
    }
}
