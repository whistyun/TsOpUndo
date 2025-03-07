﻿using NUnit.Framework;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TsOpUndo;
using TsOpUndo.Test.Parts;

namespace TsOpUndo.Test
{
    [TestFixture]
    public class UnitTest
    {
        /// <summary>
        /// 基本的なUndoRedoのテスト
        /// </summary>
        [Test]
        public void BasicTest()
        {
            var controller = new OperationController();
            var person = new Person()
            {
                Name = "Venus",
            };

            controller.ExecuteSetProperty(person, nameof(Person.Name), "Yamada");
            Assert.AreEqual("Yamada", person.Name);

            controller.ExecuteSetProperty(person, p => p.Name, "Tanaka");
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
            var controller = new OperationController();

            // マージ時間を 70msに設定
            controller.MergeSpan = TimeSpan.FromMilliseconds(70);

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
            var controller = new OperationController();

            var person = new Person()
            {
                Age = 14,
            };

            // マージ時間を 70msに設定
            controller.MergeSpan = TimeSpan.FromMilliseconds(70);

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
            var controller = new OperationController();

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
            var controller = new OperationController();

            var person = new Holder();
            person.Children.Add("A");
            person.Children.Add("B");
            person.Children.Add("C");
            person.Children.Add("D");

            var watcher = controller.BindListPropertyChanged(person, nameof(Person.Children));

            Test();

            person.Children = new ObservableCollection<string>();
            person.Children.Add("A");
            person.Children.Add("B");
            person.Children.Add("C");
            person.Children.Add("D");

            Test();

            void Test()
            {
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
        }

        /// <summary>
        /// PropertyChangedを自動的にOperation化するテスト
        /// </summary>
        [Test]
        public void ObservePropertyChangedTest()
        {
            var controller = new OperationController();
            controller.MergeSpan = TimeSpan.FromMilliseconds(70);

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

            var nameChangedWatcher = controller.BindPropertyChanged(person, nameof(Person.Name), false);
            var ageChangedWatcher = controller.BindPropertyChanged(person, nameof(Person.Age));
            var partnerNameChangedWatcher = controller.BindPropertyChanged(person, nameof(Person.Partner) + "." + nameof(Person.Name), false);

            // 変更通知から自動的に Undo / Redo が可能なOperationをスタックに積む
            {
                person.Name = "Yammada";
                person.Name = "Tanaka";

                Assert.True(controller.HasUndo);

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
                nameChangedWatcher.Cancel();
                person.Name = "Tanaka";
                Assert.False(controller.HasUndo);

                controller.Undo();
                Assert.AreEqual("Tanaka", person.Name);
            }
            {
                partnerNameChangedWatcher.Cancel();
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

                ageChangedWatcher.Cancel();
            }
        }


        [Test]
        public void RecorderTest()
        {
            var controller = new OperationController();

            var person = new Person()
            {
                Name = "Default",
                Age = 5,
            };

            // 操作の記録開始
            using (controller.BeginRecord())
            {
                controller.ExecuteAdd(person.Children, new Person()
                {
                    Name = "Child1",
                });

                controller.ExecuteSetProperty(person, nameof(Person.Age), 14);

                controller.ExecuteSetProperty(person, nameof(Person.Name), "Changed");
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
        public void RecorderTest2()
        {
            var controller = new OperationController();

            var person = new Person()
            {
                Name = "Default",
                Age = 5,
            };

            // 操作の記録開始
            using (controller.BeginRecord())
            {
                controller.ExecuteAdd(person.Children, new Person()
                {
                    Name = "Child1",
                });

                controller.ExecuteSetProperty(person, nameof(Person.Age), 14);

                controller.ExecuteSetProperty(person, nameof(Person.Name), "Changed");
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
            var controller = new OperationController();
            var person = new Person()
            {
                Name = "Venus",
            };

            controller.ExecuteSetProperty(person, x => x.Name, "Yamada");
            Assert.AreEqual("Yamada", person.Name);

            controller.ExecuteSetProperty(person, x => x.Name, "Tanaka");
            Assert.AreEqual("Tanaka", person.Name);

            controller.ExecuteDispose(person, () => person.Name = "Tanaka");
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
            var controller = new OperationController();
            var person = new Person();
            person.RP.Value = "Value1";

            // マージ時間を 70msに設定
            controller.MergeSpan = TimeSpan.FromMilliseconds(70);

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

            Assert.That(controller.HasUndo, Is.False);
        }

        [Test]
        public void MultiLayeredPropertyTest2()
        {
            var controller = new OperationController();
            var person = new Person();
            person.RP.Value = "Value1";

            using (var watcher = controller.BindPropertyChangedDisposable(person, "RP.Value", false))
            {
                person.RP.Value = "Value2";

                person.RP.Value = "Value3";

                controller.Undo();
                Assert.AreEqual("Value2", person.RP.Value);

                controller.Undo();
                Assert.AreEqual("Value1", person.RP.Value);
            }

            Assert.That(controller.HasUndo, Is.False);
        }
    }
}