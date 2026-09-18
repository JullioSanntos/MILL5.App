using System;
using System.Collections.Generic;
using System.Text;

namespace MILL90.Tests.MILL06.ViewModels.Tests {
    using global::MILL06.ViewModels.UIContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;


    [TestClass]
    public class RegionNodeTests {

        [TestMethod]
        public void NewNode_IsLeafAndNotOccupied() {
            var node = new RegionNode();

            Assert.IsFalse(node.IsSplit);
            Assert.IsFalse(node.IsOccupied);
            Assert.IsNull(node.Parent);
            Assert.IsNull(node.FirstChild);
            Assert.IsNull(node.SecondChild);
        }

        [TestMethod]
        public void AssigningFirstChild_SetsParent() {
            var parent = new RegionNode();
            var child = new RegionNode();

            parent.FirstChild = child;

            Assert.AreSame(parent, child.Parent);
        }

        [TestMethod]
        public void AssigningSecondChild_SetsParent() {
            var parent = new RegionNode();
            var child = new RegionNode();

            parent.SecondChild = child;

            Assert.AreSame(parent, child.Parent);
        }

        [TestMethod]
        public void NodeIsSplit_WhenOrientationAndBothChildrenExist() {
            var node = new RegionNode {
                FirstChild = new RegionNode(),
                SecondChild = new RegionNode()
            };

            Assert.IsFalse(node.IsSplit);

            node.Orientation = SplitOrientation.Vertical;

            Assert.IsTrue(node.IsSplit);
            Assert.IsTrue(node.IsOccupied);
        }

        [TestMethod]
        public void FirstChildChange_NotifiesDerivedProperties() {
            var node = new RegionNode();
            var notifications = new List<string?>();

            node.PropertyChanged += (sender, e) => notifications.Add(e.PropertyName);

            node.FirstChild = new RegionNode();

            CollectionAssert.Contains(notifications, nameof(RegionNode.FirstChild));
            CollectionAssert.Contains(notifications, nameof(RegionNode.IsSplit));
            CollectionAssert.Contains(notifications, nameof(RegionNode.IsOccupied));
        }

        [TestMethod]
        public void OrientationChange_NotifiesDerivedProperties() {
            var node = new RegionNode();
            var notifications = new List<string?>();

            node.PropertyChanged += (sender, e) => notifications.Add(e.PropertyName);

            node.Orientation = SplitOrientation.Horizontal;

            CollectionAssert.Contains(notifications, nameof(RegionNode.Orientation));
            CollectionAssert.Contains(notifications, nameof(RegionNode.IsSplit));
            CollectionAssert.Contains(notifications, nameof(RegionNode.IsOccupied));
        }

        [TestMethod]
        public void NodeChanging_IsRaisedOnCurrentNode() {
            var node = new RegionNode();
            RegionNodeChangingEventArgs? received = null;

            node.NodeChanging += (sender, e) => received = e;

            bool accepted = node.RaiseNodeChanging(node.Id, NodeAction.Splitting);

            Assert.IsTrue(accepted);
            Assert.IsNotNull(received);
            Assert.AreEqual(node.Id, received.NodeId);
            Assert.AreEqual(NodeAction.Splitting, received.Action);
        }

        [TestMethod]
        public void NodeChanging_BubblesToRoot() {
            var root = new RegionNode();
            var child = new RegionNode();
            var grandchild = new RegionNode();

            root.FirstChild = child;
            child.FirstChild = grandchild;

            RegionNodeChangingEventArgs? received = null;

            root.NodeChanging += (sender, e) => received = e;

            grandchild.RaiseNodeChanging(grandchild.Id, NodeAction.Closing);

            Assert.IsNotNull(received);
            Assert.AreEqual(grandchild.Id, received.NodeId);
            Assert.AreEqual(NodeAction.Closing, received.Action);
        }
        [TestMethod]
        public void NodeChanging_BubblesToRoot2() {
            var root = new RegionNode();
            var child = new RegionNode();
            var grandchild = new RegionNode();

            root.FirstChild = child;
            child.FirstChild = grandchild;

            RegionNodeChangingEventArgs? received = null;
            root.NodeChanging += (_, e) => received = e;

            grandchild.RaiseNodeChanging(grandchild.Id, NodeAction.Closing);

            Assert.IsNotNull(received);
            Assert.AreEqual(grandchild.Id, received.NodeId);
        }

        [TestMethod]
        public void NodeChanging_CanBeCancelledByRoot() {
            var root = new RegionNode();
            var child = new RegionNode();

            root.FirstChild = child;

            root.NodeChanging += (sender, e) => e.Cancel = true;

            bool accepted = child.RaiseNodeChanging(child.Id, NodeAction.Closing);

            Assert.IsFalse(accepted);
        }

        [TestMethod]
        public void ReplacingChild_ClearsOldChildParent() {
            var parent = new RegionNode();
            var oldChild = new RegionNode();
            var newChild = new RegionNode();

            parent.FirstChild = oldChild;
            parent.FirstChild = newChild;

            Assert.IsNull(oldChild.Parent);
            Assert.AreSame(parent, newChild.Parent);
        }

        [TestMethod]
        public void NodeChanging_IsRaisedByNode() {
            var node = new RegionNode();
            RegionNodeChangingEventArgs? received = null;

            node.NodeChanging += (_, e) => received = e;

            var accepted = node.RaiseNodeChanging(node.Id, NodeAction.Splitting);

            Assert.IsTrue(accepted);
            Assert.IsNotNull(received);
            Assert.AreEqual(node.Id, received.NodeId);
            Assert.AreEqual(NodeAction.Splitting, received.Action);
        }



        [TestMethod]
        public void NodeChanging_CanBeCancelledAtRoot() {
            var root = new RegionNode();
            var child = new RegionNode();

            root.FirstChild = child;
            root.NodeChanging += (_, e) => e.Cancel = true;

            var accepted = child.RaiseNodeChanging(child.Id, NodeAction.Closing);

            Assert.IsFalse(accepted);
        }

        [TestMethod]
        public void ReplacingChild_DetachesOldParent() {
            var parent = new RegionNode();
            var oldChild = new RegionNode();
            var newChild = new RegionNode();

            parent.FirstChild = oldChild;
            parent.FirstChild = newChild;

            Assert.IsNull(oldChild.Parent);
            Assert.AreSame(parent, newChild.Parent);
        }
    }
}
