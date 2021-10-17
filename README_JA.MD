# 概要 
 WPF + MVVM でツール開発を行う際にUndo / Redo 周りの実装をサポートします。

# 使い方

## 基本

```cs
var controller = new OperationController();
var person = new Person() { Name = "Venus" };
var people = new List<Person>();

// プロパティ値の変更を記録・実行
controller.ExecuteSetProperty(person, person => person.Name, "New Name");

// クラス変数の変更を記録・実行
controller.ExecuteSetStaticProperty(typeof(Person), nameof(Person.StaticValue), "ika");

// リストへの変更を記録・実行
controller.ExecuteAdd(people, new Person());
controller.ExecuteRemoveAt(people, 0);

// 変更の取り消し
controller.Undo();

// 取り消した変更の再実行
controller.Redo();
```

## 変更のグループ化

`OperationController.BeginRecord`を使用することで、複数の変更を一纏めにすることができます。
下記の例では、`Name`の変更と`Age`の変更を一纏めにしています。
`Undo`を呼び出すと、`Name`の変更と`Age`の変更がまとめて取り消されます。

```cs
var controller = new OperationController();
var person = new Person() { Name = "Venus", Age=13 };

using (controller.BeginRecord())
{
    controller.ExecuteSetProperty(person, nameof(Person.Name), "Changed");
    controller.ExecuteSetProperty(person, nameof(Person.Age), 14);
}

Assert.AreEqual("Changed", person.Name);
Assert.AreEqual(14, person.Age);

controller.Undo();

Assert.AreEqual("Venus", person.Name);
Assert.AreEqual(13, person.Age);
```

## NotifyPropertyChangedによる値変更の監視

以下のメソッドは、`PropertyChanged`イベントもしくは`CollectionChanged`イベント経由で値の変更を監視します。
値の変更があった場合は、OperationControllerへ記録を行います。

* `OperationController.BindPropertyChanged`
* `OperationController.BindListPropertyChanged`
* `OperationController.BindPropertyChanged2`
* `OperationController.BindPropertyChanged2Fast`

`BindPropertyChanged`, `BindListPropertyChanged`は指定のプロパティのみ監視します。
`INotifyPropertyChanged2`を実装したオブジェクトは`BindPropertyChanged2`, `BindPropertyChanged2Fast`を使用することができ、これらのメソッドはオブジェクトが持つすべてのプロパティを監視します。
また、オブジェクトが持つプロパティ値が`INotifyPropertyChanged2`を実装する場合は、そのオブジェクトのプロパティも監視対象とします。
`BindPropertyChanged2`はプロパティ値のインスタンスをもとに監視するか判断します(プロパティの型がobjectだったとしても、値が`BindPropertyChanged2`を実装するなら監視対象とします)。
一方、`BindPropertyChanged2Fast`はプロパティの型で判断を行います(プロパティの型がobjectだった場合、値が`BindPropertyChanged2`を実装していたとしてもプロパティ値は監視しません)。

```cs
var controller = new OperationController();

// INotifyPropertyChangedを実装したオブジェクト
var person = new Person() { Name = "Venus", Age=13 };
// Nameプロパティを監視する。
controller.BindPropertyChanged(person, nameof(Person.Name), false);
person.Name = "Yammada";
person.Name = "Tanaka";
Assert.AreEqual(2, controller.Undos.Count());
// Ageは監視していないため、変更しても記録されない
person.Age = 15;
Assert.AreEqual(2, controller.Undos.Count());


// INotifyPropertyChanged2を実装したオブジェクト
var hiperson = new HiPerson() { Name = "Venus", Age=13 };
// HiPersonがもつ全てのプロパティを監視する
controller.BindPropertyChanged2(hiperson);
hiperson.Name = "A002";
Assert.AreEqual(3, controller.Undos.Count());
hiperson.Age = 14;
Assert.AreEqual(4, controller.Undos.Count());
```