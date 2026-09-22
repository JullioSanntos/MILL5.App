using System;
using System.Collections.Generic;
using System.Text;
using MILL06.ViewModels.UIContracts;

namespace MILL06.ViewModels {
    public partial class ContactTypesViewModel {
        public override bool GetCanBeReplaced(RegionNode regionNode) {
            return true;
        }
    }
}
