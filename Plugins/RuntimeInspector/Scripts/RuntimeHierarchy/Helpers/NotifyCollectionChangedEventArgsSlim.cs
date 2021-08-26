using System.Collections.Generic;
using System.Collections.Specialized;

namespace RuntimeInspectorNamespace
{
    public class NotifyCollectionChangedEventArgsSlim<T>
    {
        public NotifyCollectionChangedEventArgsSlim( NotifyCollectionChangedAction action, IList<T> items )
        {
            this.action = action;
            this.items = items;
        }

        private readonly NotifyCollectionChangedAction action;
        public NotifyCollectionChangedAction Action => action;

        private readonly IList<T> items;
        public IList<T> Items => items;
    }
}
