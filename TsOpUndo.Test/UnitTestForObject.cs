using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TsOpUndo.Test.Parts;

namespace TsOpUndo.Test
{
    public abstract class UnitTestForObject
    {
        protected abstract ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop);

        [Test]
        public void PropertySet()
        {
            var controller = new OperationController();
            var person = new HiPerson()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Name = "A002";

            Assert.AreEqual(1, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual("A001", person.Name);
            Assert.AreEqual(0, controller.Undos.Count());
        }

        [Test]
        public void BindChildNotify()
        {
            var controller = new OperationController();
            var person = new HiPerson()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Partner = new HiPerson()
            {
                Name = "B001",
                Age = 1
            };

            Assert.AreEqual(1, controller.Undos.Count());
            controller.Undo();
            Assert.AreEqual(null, person.Partner);

            controller.Redo();
            Assert.AreEqual(
                new HiPerson()
                {
                    Name = "B001",
                    Age = 1
                },
                person.Partner);


            person.Partner.Name = "B002";
            Assert.AreEqual(2, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual("B001", person.Partner.Name);

            controller.Redo();
            Assert.AreEqual(
                new HiPerson()
                {
                    Name = "B002",
                    Age = 1
                },
                person.Partner);

            var prevPartner = person.Partner;
            var newPartner = new HiPerson()
            {
                Name = "B003",
                Age = 2
            };

            person.Partner = newPartner;
            Assert.AreEqual(3, controller.Undos.Count());

            prevPartner.Age = 14;

            Assert.AreEqual(3, controller.Undos.Count());

            person.Partner.Age = 4;

            Assert.AreEqual(4, controller.Undos.Count());

            controller.Undo();
            controller.Undo();

            Assert.AreEqual(prevPartner, person.Partner);
            Assert.AreEqual(2, controller.Undos.Count());

            newPartner.Name = "C010";
            Assert.AreEqual(2, controller.Undos.Count());

            prevPartner.Name = "C011";
            Assert.AreEqual(3, controller.Undos.Count());
        }

        [Test]
        public void BindChildNotify2()
        {
            var controller = new OperationController();
            var person = new HiPerson()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.BFF = new Friend()
            {
                NickName = "A",
                Person = new HiPerson()
                {
                    Name = "A001",
                    Age = 1
                }
            };

            Assert.AreEqual(1, controller.Undos.Count());

            person.BFF.NickName = "A";
            Assert.AreEqual(1, controller.Undos.Count());

            person.BFF.Person.Name = "B001";
            Assert.AreEqual(2, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual("A001", person.BFF.Person.Name);

        }

        [Test]
        public void BindListNotify()
        {

            var controller = new OperationController();
            var person = new HiPerson()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Children.Add(new HiPerson()
            {
                Name = "B001",
                Age = 3
            });
            person.Children.Insert(0, new HiPerson()
            {
                Name = "B002",
                Age = 4
            });

            Assert.AreEqual(2, controller.Undos.Count());
            controller.Undo();
            Assert.AreEqual("B001", person.Children[0].Name);
            Assert.AreEqual(1, controller.Undos.Count());

            controller.Redo();
            Assert.AreEqual("B002", person.Children[0].Name);
            Assert.AreEqual("B001", person.Children[1].Name);
            Assert.AreEqual(2, controller.Undos.Count());

            person.Children[0].Name = "C010";
            person.Children[1].Name = "C011";
            Assert.AreEqual(4, controller.Undos.Count());

            controller.Undo();
            Assert.AreEqual(3, controller.Undos.Count());
            Assert.AreEqual("C010", person.Children[0].Name);
            Assert.AreEqual("B001", person.Children[1].Name);

            controller.Undo();
            Assert.AreEqual("B002", person.Children[0].Name);
            Assert.AreEqual("B001", person.Children[1].Name);
        }

        [Test]
        public void BindListNotify2()
        {

            var controller = new OperationController();
            var person = new HiPerson()
            {
                Name = "A001",
                Age = 2
            };

            BindProperty(controller, person);

            person.Friends.Add(new Friend()
            {
                NickName = "B1",
                Person = new HiPerson()
                {
                    Name = "B001",
                    Age = 3
                }
            });

            person.Friends.Insert(0, new Friend()
            {
                NickName = "B2",
                Person = new HiPerson()
                {
                    Name = "B002",
                    Age = 2
                }
            });

            Assert.AreEqual(2, controller.Undos.Count());
            controller.Undo();
            Assert.AreEqual("B001", person.Friends[0].Person.Name);

            controller.Redo();
            Assert.AreEqual("B002", person.Friends[0].Person.Name);
            Assert.AreEqual("B001", person.Friends[1].Person.Name);

            person.Friends[0].Person.Name = "C010";
            person.Friends[1].Person.Name = "C011";

            controller.Undo();
            Assert.AreEqual("C010", person.Friends[0].Person.Name);
            Assert.AreEqual("B001", person.Friends[1].Person.Name);

            controller.Undo();
            Assert.AreEqual("B002", person.Friends[0].Person.Name);
            Assert.AreEqual("B001", person.Friends[1].Person.Name);
        }
    }


    [TestFixture]
    public class UnitTestForObjectListener : UnitTestForObject
    {
        protected override ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop)
        {
            return controller.BindPropertyChanged2(prop);
        }
    }

    [TestFixture]
    public class UnitTestForFastObjectListener : UnitTestForObject
    {
        protected override ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop)
        {
            return controller.BindPropertyChanged2Fast(prop);
        }
    }
}
