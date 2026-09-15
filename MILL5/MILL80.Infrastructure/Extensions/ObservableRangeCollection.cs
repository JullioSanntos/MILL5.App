using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MILL80.Infrastructure {
    public class ObservableRangeCollection<T> : ObservableCollection<T> {
        public ObservableRangeCollection() : base() { }
        public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

        public void ReplaceRange(IEnumerable<T> items) {
            // Modifying the underlying Items collection bypasses the individual CollectionChanged events
            this.Items.Clear();
            foreach (var item in items) {
                this.Items.Add(item);
            }

            // Fire ONE event telling the UI to redraw once
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}