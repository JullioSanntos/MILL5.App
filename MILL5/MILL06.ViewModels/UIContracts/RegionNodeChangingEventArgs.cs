namespace MILL06.ViewModels.UIContracts;

public class RegionNodeChangingEventArgs(string nodeId, NodeAction action) : EventArgs {
    public string NodeId { get; } = nodeId;
    public NodeAction Action { get; } = action;
    public bool Cancel { get; set; }
}

public enum NodeAction {
    Splitting,
    Closing,
    PayloadChanging
}