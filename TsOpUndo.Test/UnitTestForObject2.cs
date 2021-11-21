using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TsOpUndo.Test.Parts;

namespace TsOpUndo.Test
{
    public abstract class UnitTestForObject2
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
    }


    [TestFixture]
    public class UnitTestForObjectListener2 : UnitTestForObject2
    {
        protected override ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop)
        {
            return controller.BindPropertyChanged2(prop);
        }
    }

    [TestFixture]
    public class UnitTestForFastObjectListener2 : UnitTestForObject2
    {
        protected override ICancellable BindProperty(OperationController controller, INotifyPropertyChanged2 prop)
        {
            return controller.BindPropertyChanged2Fast(prop);
        }
    }
}
