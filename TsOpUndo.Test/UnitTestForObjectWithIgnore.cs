using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TsOpUndo.Test.Parts;

namespace TsOpUndo.Test
{
    public abstract class UnitTestForObjectWithIgnore
    {
        protected abstract ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop);

        [Test]
        public void PropertySet()
        {
            var controller = new OperationController();
            var person = new HiPerson2()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Name = "A002";
            Assert.AreEqual(0, controller.Undos.Count());

            person.Age = 3;
            Assert.AreEqual(1, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual(2, person.Age);

            Assert.AreEqual(0, controller.Undos.Count());
        }

        [Test]
        public void BindChildNotify()
        {
            var controller = new OperationController();
            var person = new HiPerson2()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Partner1 = new HiPerson2()
            {
                Name = "A002",
                Age = 3
            };
            Assert.AreEqual(0, controller.Undos.Count());

            person.Partner1.Name = "A003";
            Assert.AreEqual(0, controller.Undos.Count());
            person.Partner1.Age = 5;
            Assert.AreEqual(0, controller.Undos.Count());

            person.Partner2 = new HiPerson2()
            {
                Name = "A003",
                Age = 4
            };
            Assert.AreEqual(0, controller.Undos.Count());

            person.Partner2.Name = "A004";
            Assert.AreEqual(0, controller.Undos.Count());
            person.Partner2.Age = 5;
            Assert.AreEqual(1, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual(4, person.Partner2.Age);
        }

        [Test]
        public void BindChildNotify2()
        {
            var controller = new OperationController();
            var person = new HiPerson2()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Friend1 = new HiFriend()
            {
                NickName = "AAAA",
                Person = new HiPerson2()
                {
                    Name = "A002",
                    Age = 3
                }
            };
            Assert.AreEqual(0, controller.Undos.Count());

            person.Friend1.Person.Age = 5;
            Assert.AreEqual(0, controller.Undos.Count());

            person.Friend2 = new HiFriend()
            {
                NickName = "BBBB",
                Person = new HiPerson2()
                {
                    Name = "B002",
                    Age = 1
                }
            };
            Assert.AreEqual(0, controller.Undos.Count());

            person.Friend2.Person.Age = 5;
            Assert.AreEqual(1, controller.Undos.Count());
            controller.Undo();

            Assert.AreEqual(1, person.Friend2.Person.Age);
        }

        [Test]
        public void BindListNotify()
        {
            var controller = new OperationController();
            var person = new HiPerson2()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Friends1.Add(new HiFriend()
            {
                NickName = "AAAA",
                Person = new HiPerson2()
                {
                    Name = "A001",
                    Age = 3
                }
            });
            Assert.AreEqual(0, controller.Undos.Count());

            person.Friends1[0].Person.Age = 4;
            Assert.AreEqual(0, controller.Undos.Count());


            person.Friends2.Add(new HiFriend()
            {
                NickName = "BBBB",
                Person = new HiPerson2()
                {
                    Name = "B001",
                    Age = 3
                }
            });
            Assert.AreEqual(0, controller.Undos.Count());

            person.Friends2[0].Person.Age = 4;
            Assert.AreEqual(1, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual(3, person.Friends2[0].Person.Age);
        }
    }


    [TestFixture]
    public class UnitTestForObjectListenerWithIgnore : UnitTestForObjectWithIgnore
    {
        protected override ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop)
        {
            return controller.BindPropertyChanged2(prop);
        }
    }

    [TestFixture]
    public class UnitTestForFastObjectListenerWithIgnore : UnitTestForObjectWithIgnore
    {
        protected override ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop)
        {
            return controller.BindPropertyChanged2Fast(prop);
        }
    }
}
