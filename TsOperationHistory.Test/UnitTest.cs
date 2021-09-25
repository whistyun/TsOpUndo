using NUnit.Framework;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TsOperationHistory.Extensions;

namespace TsOperationHistory.Test
{
    internal class Person : Bindable, IDisposable, IRestore
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

    internal class Holder : Bindable
    {
        private ObservableCollection<string> _children = new ObservableCollection<string>();

        public ObservableCollection<string> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }
    }

    [TestFixture]
    public class UnitTest
    {
        /// <summary>
        /// 基本的なUndoRedoのテスト
        /// </summary>
        [Test]
        public void BasicTest()
        {
            IOperationController controller = new OperationController();
            var person = new Person()
            {
                Name = "Venus",
            };

            controller.Execute(person.GenerateSetPropertyOperation(x => x.Name, "Yamada"));
            Assert.AreEqual("Yamada", person.Name);

            controller.Execute(person.GenerateSetPropertyOperation(x => x.Name, "Tanaka"));
            Assert.AreEqual("Tanaka", person.Name);

            controller.Undo();
            Assert.AreEqual("Yamada", person.Name);

            controller.Undo();
            Assert.AreEqual("Venus", person.Name);
        }

        /// <summary>
        /// スタティックプロパティのUndoRedoのテスト
        /// </summary>
        [Test]
        public async Task StaticPropertyTest()
        {
            IOperationController controller = new OperationController();

            // デフォルトのマージ時間を 70msに設定
            Operation.DefaultMergeSpan = TimeSpan.FromMilliseconds(70);

            Person.StaticValue = "Geso";

            controller.ExecuteSetStaticProperty(typeof(Person), "StaticValue", "ika");
            Assert.AreEqual("ika", Person.StaticValue);

            await Task.Delay(75);

            controller.ExecuteSetStaticProperty(typeof(Person), "StaticValue", "tako");
            Assert.AreEqual("tako", Person.StaticValue);

            await Task.Delay(75);

            controller.Undo();
            Assert.AreEqual("ika", Person.StaticValue);

            controller.Undo();
            Assert.AreEqual("Geso", Person.StaticValue);
        }

        /// <summary>
        /// Operationの自動結合テスト
        /// </summary>
        [Test]
        public async Task MergedTest()
        {
            IOperationController controller = new OperationController();

            var person = new Person()
            {
                Age = 14,
            };

            // デフォルトのマージ時間を 70msに設定
            Operation.DefaultMergeSpan = TimeSpan.FromMilliseconds(70);

            //Age = 30
            controller.ExecuteSetProperty(person, nameof(Person.Age), 30);
            Assert.AreEqual(30, person.Age);

            //10 ms待つ
            await Task.Delay(10);

            //Age = 100
            controller.ExecuteSetProperty(person, nameof(Person.Age), 100);
            Assert.AreEqual(100, person.Age);

            //100ms 待つ
            await Task.Delay(75);

            //Age = 150
            controller.ExecuteSetProperty(person, nameof(Person.Age), 150);
            Assert.AreEqual(150, person.Age);

            //Age = 100
            controller.Undo();
            Assert.AreEqual(100, person.Age);

            // マージされているので 30には戻らずそのまま14に戻る
            // Age = 14
            controller.Undo();
            Assert.AreEqual(14, person.Age);
        }

        /// <summary>
        /// リスト操作のテスト
        /// </summary>
        [Test]
        public void ListTest()
        {
            IOperationController controller = new OperationController();

            var person = new Person()
            {
                Name = "Root"
            };

            controller.ExecuteAdd(person.Children,
                new Person()
                {
                    Name = "Child1"
                });

            controller.ExecuteAdd(person.Children,
                new Person()
                {
                    Name = "Child2"
                });

            Assert.AreEqual(2, person.Children.Count);

            controller.ExecuteRemoveAt(person.Children, 0);
            Assert.That(person.Children.Count, Is.EqualTo(1));

            controller.Undo();
            Assert.AreEqual(2, person.Children.Count);

            controller.Undo();
            Assert.That(person.Children.Count, Is.EqualTo(1));

            controller.Undo();
            Assert.IsEmpty(person.Children);
        }

        [Test]
        public void ObserveCollectionChangedTest()
        {
            IOperationController controller = new OperationController();

            var person = new Holder();
            person.Children.Add("A");
            person.Children.Add("B");
            person.Children.Add("C");
            person.Children.Add("D");

            var watcher = controller.BindListPropertyChanged<ObservableCollection<string>, string>(person, nameof(Person.Children));

            // add

            person.Children.Add("E");

            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D", "E" }, person.Children));
            controller.Undo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D" }, person.Children));
            controller.Redo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D", "E" }, person.Children));
            controller.Undo();

            // insert

            person.Children.Insert(2, "E");

            controller.Undo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D" }, person.Children));
            controller.Redo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "E", "C", "D" }, person.Children));
            controller.Undo();

            // replace

            person.Children[2] = "E";

            controller.Undo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D" }, person.Children));
            controller.Redo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "E", "D" }, person.Children));
            controller.Undo();

            // remove

            person.Children.RemoveAt(person.Children.Count - 1);

            controller.Undo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D" }, person.Children));
            controller.Redo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C" }, person.Children));
            controller.Undo();

            // move

            person.Children.Move(0, 3);

            controller.Undo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D" }, person.Children));
            controller.Redo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "B", "C", "D", "A" }, person.Children));
            controller.Undo();

            // clear

            person.Children.Clear();

            controller.Undo();
            Assert.IsTrue(Enumerable.SequenceEqual(new[] { "A", "B", "C", "D" }, person.Children));
            controller.Redo();
            Assert.IsTrue(Enumerable.SequenceEqual(new string[0], person.Children));
            controller.Undo();
        }

        /// <summary>
        /// PropertyChangedを自動的にOperation化するテスト
        /// </summary>
        [Test]
        public void ObservePropertyChangedTest()
        {
            IOperationController controller = new OperationController();

            var person = new Person()
            {
                Name = "First",
                Age = 0,
                Partner = new Person()
                {
                    Name = "Second",
                    Age = 20,
                }
            };

            var nameChangedWatcher = controller.BindPropertyChanged<string>(person, nameof(Person.Name), false);
            var ageChangedWatcher = controller.BindPropertyChanged<int>(person, nameof(Person.Age));
            var partnerNameChangedWatcher = controller.BindPropertyChanged<string>(person, nameof(Person.Partner) + "." + nameof(Person.Name), false);

            // 変更通知から自動的に Undo / Redo が可能なOperationをスタックに積む
            {
                person.Name = "Yammada";
                person.Name = "Tanaka";

                Assert.True(controller.CanUndo);

                controller.Undo();
                Assert.AreEqual("Yammada", person.Name);

                controller.Undo();
                Assert.AreEqual("First", person.Name);

                person.Name = "Yamaguchi";
                person.Name = "Taniguchi";
                controller.Undo();
                Assert.AreEqual("Yamaguchi", person.Name);
                controller.Undo();
                Assert.AreEqual("First", person.Name);
            }
            {
                person.Partner.Name = "Sato";
                person.Partner.Name = "Goto";

                controller.Undo();
                Assert.AreEqual("Sato", person.Partner.Name);

                controller.Undo();
                Assert.AreEqual("Second", person.Partner.Name);
            }

            // Dispose後は変更通知が自動的にOperationに変更されないことを確認
            {
                nameChangedWatcher.Dispose();
                person.Name = "Tanaka";
                Assert.False(controller.CanUndo);

                controller.Undo();
                Assert.AreEqual("Tanaka", person.Name);
            }
            {
                partnerNameChangedWatcher.Dispose();
                person.Partner.Name = "Goto";

                controller.Undo();
                Assert.AreEqual("Goto", person.Partner.Name);
            }

            // Ageは自動マージ有効なため1回のUndoで初期値に戻ることを確認
            {
                for (int i = 1; i < 30; ++i)
                {
                    person.Age = i;
                }

                Assert.AreEqual(29, person.Age);

                controller.Undo();
                Assert.AreEqual(0, person.Age);

                ageChangedWatcher.Dispose();
            }
        }


        [Test]
        public void RecorderTest()
        {
            IOperationController controller = new OperationController();

            var person = new Person()
            {
                Name = "Default",
                Age = 5,
            };

            var recorder = new OperationRecorder(controller);

            // 操作の記録開始
            recorder.BeginRecode();
            {
                recorder.Current.ExecuteAdd(person.Children, new Person()
                {
                    Name = "Child1",
                });

                recorder.Current.ExecuteSetProperty(person, nameof(Person.Age), 14);

                recorder.Current.ExecuteSetProperty(person, nameof(Person.Name), "Changed");
            }
            // 操作の記録完了
            recorder.EndRecode();

            // 1回のUndoでレコード前のデータが復元される
            controller.Undo();
            Assert.AreEqual("Default", person.Name);
            Assert.AreEqual(5, person.Age);
            Assert.IsEmpty(person.Children);

            // Redoでレコード終了後のデータが復元される
            controller.Redo();
            Assert.AreEqual("Changed", person.Name);
            Assert.AreEqual(14, person.Age);
            Assert.That(person.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void RecorderTest2()
        {
            IOperationController controller = new OperationController();

            var person = new Person()
            {
                Name = "Default",
                Age = 5,
            };

            // 操作の記録開始
            using (var recorder = new OperationRecorder(controller).Begin())
            {
                recorder.Current.ExecuteAdd(person.Children, new Person()
                {
                    Name = "Child1",
                });

                recorder.Current.ExecuteSetProperty(person, nameof(Person.Age), 14);

                recorder.Current.ExecuteSetProperty(person, nameof(Person.Name), "Changed");
            }

            // 1回のUndoでレコード前のデータが復元される
            controller.Undo();
            Assert.AreEqual("Default", person.Name);
            Assert.AreEqual(5, person.Age);
            Assert.IsEmpty(person.Children);

            // Redoでレコード終了後のデータが復元される
            controller.Redo();
            Assert.AreEqual("Changed", person.Name);
            Assert.AreEqual(14, person.Age);
            Assert.That(person.Children.Count, Is.EqualTo(1));
        }



        [Test]
        public void DisposeTest()
        {
            IOperationController controller = new OperationController();
            var person = new Person()
            {
                Name = "Venus",
            };

            controller.Execute(person.GenerateSetPropertyOperation(x => x.Name, "Yamada"));
            Assert.AreEqual("Yamada", person.Name);

            controller.Execute(person.GenerateSetPropertyOperation(x => x.Name, "Tanaka"));
            Assert.AreEqual("Tanaka", person.Name);

            controller.ExecuteDispose(person, () => person.Restore(() => person.Name = "Tanaka"));
            Assert.That(person.Name, Is.Null);

            controller.Undo();
            Assert.AreEqual("Tanaka", person.Name);

            controller.Undo();
            Assert.AreEqual("Yamada", person.Name);

            controller.Undo();
            Assert.AreEqual("Venus", person.Name);
        }

        [Test]
        public async Task MultiLayeredPropertyTest()
        {
            IOperationController controller = new OperationController();
            var person = new Person();
            person.RP.Value = "Value1";

            // デフォルトのマージ時間を 70msに設定
            Operation.DefaultMergeSpan = TimeSpan.FromMilliseconds(70);

            //75ms 待つ
            await Task.Delay(75);

            controller.ExecuteSetProperty(person, "RP.Value", "Value2");
            Assert.AreEqual("Value2", person.RP.Value);

            //75ms 待つ
            await Task.Delay(75);

            controller.ExecuteSetProperty(person, "RP.Value", "Value3");
            Assert.AreEqual("Value3", person.RP.Value);

            controller.Undo();
            Assert.AreEqual("Value2", person.RP.Value);

            controller.Undo();
            Assert.AreEqual("Value1", person.RP.Value);

            Assert.That(controller.CanUndo, Is.False);
        }

        [Test]
        public void MultiLayeredPropertyTest2()
        {
            IOperationController controller = new OperationController();
            var person = new Person();
            person.RP.Value = "Value1";

            using (var watcher = controller.BindPropertyChanged<string>(person, "RP.Value", false))
            {
                person.RP.Value = "Value2";

                person.RP.Value = "Value3";

                controller.Undo();
                Assert.AreEqual("Value2", person.RP.Value);

                controller.Undo();
                Assert.AreEqual("Value1", person.RP.Value);
            }

            Assert.That(controller.CanUndo, Is.False);
        }
    }
}