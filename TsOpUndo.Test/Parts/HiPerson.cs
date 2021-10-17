using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TsOpUndo.Test.Parts
{
    class HiPerson : GenericNotifyPropertyChanged2
    {
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

        public HiPerson Partner
        {
            set => SetValue(value);
            get => GetValue<HiPerson>();
        }

        public Friend BFF
        {
            set => SetValue(value);
            get => GetValue<Friend>();
        }

        public ObservableCollection<Friend> Friends
        {
            set => SetValue(value);
            get => GetValue<ObservableCollection<Friend>>();
        }

        public ObservableCollection<HiPerson> Children
        {
            set => SetValue(value);
            get => GetValue<ObservableCollection<HiPerson>>();
        }


        public HiPerson()
        {
            Friends = new ObservableCollection<Friend>();
            Children = new ObservableCollection<HiPerson>();
        }

        public override int GetHashCode()
        {
            return Age + (Name ?? "").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is HiPerson p)
            {
                return
                    Age == p.Age
                 && Name == p.Name
                 && Partner == p.Partner // FIXME: It causes infinity recursive loop!
                 && BFF == p.BFF
                 && Enumerable.SequenceEqual(Children, p.Children);
            }
            return false;
        }

        public static bool operator ==(HiPerson left, HiPerson right) => Object.Equals(left, right);
        public static bool operator !=(HiPerson left, HiPerson right) => !(left == right);
    }

    class Friend
    {
        public string NickName { get; set; }
        public HiPerson Person { get; set; }

        public override int GetHashCode()
        {
            return Person.GetHashCode() + (NickName ?? "").GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Friend p)
            {
                return
                    NickName == p.NickName
                 && Person == p.Person;
            }
            return false;
        }

        public static bool operator ==(Friend left, Friend right) => Object.Equals(left, right);
        public static bool operator !=(Friend left, Friend right) => !(left == right);
    }
}
