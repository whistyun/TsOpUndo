using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TsOpUndo.Internal.Listeners
{
    internal class PropertyListListener : ICancellable
    {
        private OperationController _controller;
        private INotifyPropertyChanged _owner;
        private string _propNm;
        private Func<IList> _getter;

        ListListener _listener;

        public PropertyListListener(OperationController controller, INotifyPropertyChanged owner, string propNm, Func<IList> getter)
            : this(controller, owner, getter.Invoke(), propNm, getter)
        { }

        public PropertyListListener(OperationController controller, INotifyPropertyChanged owner, IList ownerList, string propNm, Func<IList> getter)
        {
            _controller = controller;
            _owner = owner;
            _getter = getter;
            _listener = new ListListener(_controller, _getter.Invoke());

            _propNm = propNm;

            _owner.PropertyChanged += PropertyChanged;
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propNm || string.IsNullOrEmpty(e.PropertyName))
            {
                _listener.Cancel();
                _listener = new ListListener(_controller, _getter.Invoke());
            }
        }

        public void Cancel()
        {
            _owner.PropertyChanged -= PropertyChanged;
            _listener.Cancel();
        }
    }
}
