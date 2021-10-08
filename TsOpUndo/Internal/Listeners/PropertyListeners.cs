using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TsOpUndo.Operations;

namespace TsOpUndo.Internal.Listeners
{
    internal class PropertyListeners : ICancellable
    {
        private OperationController _controller;
        private INotifyPropertyChanged _target;
        private string _targetPropertyName;
        private object _prevValue;
        private bool _autoMerge;

        public PropertyListeners(OperationController controller, INotifyPropertyChanged target, string propName, object prevValue, bool autoMerge)
        {
            _controller = controller;
            _target = target;
            _targetPropertyName = propName;
            _prevValue = prevValue;
            _autoMerge = autoMerge;

            _target.PropertyChanged += PropertyChanged;
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != _targetPropertyName)
                return;

            object newValue = FastReflection.GetProperty<object>(_target, _targetPropertyName);

            if (_controller.IsOperating)
            {
                _prevValue = newValue;
                return;
            }

            var operation = new PropertyOperation(_target, _targetPropertyName, _prevValue, newValue);
            _prevValue = newValue;

            if (_autoMerge)
            {
                _controller.Push(operation);
            }
            else
            {
                _controller.PushWithoutMerge(operation);
            }
        }

        public void Cancel()
        {
            _target.PropertyChanged -= PropertyChanged;
        }
    }
}
