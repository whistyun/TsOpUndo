using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace TsOpUndo.Test.Parts
{
    internal class Holder : Bindable
    {
        private ObservableCollection<string> _children = new ObservableCollection<string>();

        public ObservableCollection<string> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }
    }
}
