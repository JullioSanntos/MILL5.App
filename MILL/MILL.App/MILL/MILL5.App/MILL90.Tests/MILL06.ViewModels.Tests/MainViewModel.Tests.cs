using MILL06.ViewModels;
using MILL09.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MILL90.Tests.MILL06.ViewModels.Tests {
    [TestClass]
    public class MainViewModelTests {
        [TestMethod]
        public void InstantiationTests() {
            var target = new MainViewModel();
            Assert.IsNotNull(target);
        }
    }
}
