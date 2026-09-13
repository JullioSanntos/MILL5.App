using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace MILL09.Models;

public abstract partial class ModelEntityBase : ObservableObject {
    [ObservableProperty] private bool _isDirty;

    private Dictionary<string, object?>? _snapshot;

    protected ModelEntityBase() {
        PropertyChanged += (_, e) => {
            if (e.PropertyName != nameof(IsDirty)) IsDirty = true;
        };
        TakeSnapshot();
    }

    public virtual async Task SaveAsync(CancellationToken ct = default) {
        await OnSaveAsync(ct);
        IsDirty = false;
        TakeSnapshot();
    }

    public virtual void Cancel() {
        if (_snapshot == null) return;
        foreach (var (propName, value) in _snapshot) {
            GetType().GetProperty(propName)?.SetValue(this, value);
        }
        IsDirty = false;
    }

    protected abstract Task OnSaveAsync(CancellationToken ct);

    private void TakeSnapshot() {
        _snapshot = GetType().GetProperties()
            .Where(p => p.CanRead && p.CanWrite && p.DeclaringType != typeof(ModelEntityBase))
            .ToDictionary(p => p.Name, p => p.GetValue(this));
    }
}
