using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace TsOpUndo.Test.Parts
{
    internal class Person : Bindable, IDisposable, IRestoreable
    {
        private string _name;

        public Person()
        {
        }

        public static string StaticValue { get; set; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _age;

        public int Age
        {
            get => _age;
            set => SetProperty(ref _age, value);
        }

        private Person _partner;
        public Person Partner
        {
            get => _partner;
            set => SetProperty(ref _partner, value);
        }

        public ReactivePropertySlim<string> RP { get; set; } = new ReactivePropertySlim<string>();

        private ObservableCollection<Person> _children = new ObservableCollection<Person>();
        private bool disposedValue;

        public ObservableCollection<Person> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    RP.Dispose();
                }

                _name = null;
                _children = null;
                RP = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Restore(Action restorePropertiesAction)
        {
            if (!disposedValue) return;

            disposedValue = false;
            _name = string.Empty;
            _age = 0;
            _children = new ObservableCollection<Person>();
            RP = new ReactivePropertySlim<string>();

            restorePropertiesAction.Invoke();
            GC.ReRegisterForFinalize(this);
        }

        public override int GetHashCode()
        {
            return Age + (Name ?? "").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Person p)
            {
                return Age == p.Age && Name == p.Name;
            }
            return false;
        }
    }
}
