namespace MILL09.Models; 
public class MainModel : ModelBase {

    #region singleton
    private static readonly Lazy<MainModel> _instance =
        new Lazy<MainModel>(() => new MainModel());

    // Global access point
    public static MainModel Instance => _instance.Value;

    protected internal MainModel() { }
    #endregion singleton

}
