using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TsOpUndo.Test.Parts
{
    class HiPerson2 : GenericNotifyPropertyChanged2
    {
        [NoBindHistory]
        public string Name
        {
            set => SetValue(value);
            get => GetValue<string>();
        }

        public int Age
        {
            set => SetValue(value);
            get => GetValue<int>();
        }

        [NoBindHistory]
        public HiPerson2 Partner1
        {
            set => SetValue(value);
            get => GetValue<HiPerson2>();
        }

        [NoBindHistory(AllowBindChild = true)]
        public HiPerson2 Partner2
        {
            set => SetValue(value);
            get => GetValue<HiPerson2>();
        }

        [NoBindHistory]
        public ObservableCollection<HiPerson2> Children1
        {
            set => SetValue(value);
            get => GetValue<ObservableCollection<HiPerson2>>();
        }


        [NoBindHistory(AllowBindChild = true)]
        public ObservableCollection<HiPerson2> Children2
        {
            set => SetValue(value);
            get => GetValue<ObservableCollection<HiPerson2>>();
        }

        public HiPerson2()
        {
            Children1 = new ObservableCollection<HiPerson2>();
            Children2 = new ObservableCollection<HiPerson2>();
        }

        public override int GetHashCode()
        {
            return Age + (Name ?? "").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is HiPerson2 p)
            {
                return
                    Age == p.Age
                 && Name == p.Name;
            }
            return false;
        }

        public static bool operator ==(HiPerson2 left, HiPerson2 right) => Object.Equals(left, right);
        public static bool operator !=(HiPerson2 left, HiPerson2 right) => !(left == right);
    }
}
