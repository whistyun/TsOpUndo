# Abstract 
Undo / Redo library for WPF+MVVM

# How to use

## Basic

```cs
var controller = new OperationController();
var person = new Person() { Name = "Venus" };
var people = new List<Person>();

//  record & execite property setting.
controller.ExecuteSetProperty(person, person => person.Name, "New Name");

// record & execite class-variable setting.
controller.ExecuteSetStaticProperty(typeof(Person), nameof(Person.StaticValue), "ika");

// record & execite list changing.
controller.ExecuteAdd(people, new Person());
controller.ExecuteRemoveAt(people, 0);

controller.Undo();

controller.Redo();
```

## Grouping

You can group multiple changed with `OperationController.BeginRecord`.
In the example below, `Name` and `Age` change are grouped.
Calling `Undo` undoes the changes to` Name` and `Age`.

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

## Monitor property changes with NotifyPropertyChanged

Methods listed below monitor property changes with `PropertyChanged` or `CollectionChanged` events.
When property is changed, record it in OperationController.

* `OperationController.BindPropertyChanged`
* `OperationController.BindListPropertyChanged`
* `OperationController.BindPropertyChanged2`
* `OperationController.BindPropertyChanged2Fast`

`BindPropertyChanged` and` BindListPropertyChanged` methods requires the name of the property to monitor. 
The object that implements `INotifyPropertyChanged2` can be applied to `BindPropertyChanged2` and `BindPropertyChanged2Fast` methods.
These methods monitor all properties of the target object. 
In addition, if the property value implements `INotifyPropertyChanged2`, properties of this value is monitored. 

`BindPropertyChanged2` checks the property value.
Even if the property type is `System.Object`, if the value implements `INotifyPropertyChanged2`, it will be monitored. 

`BindPropertyChanged2Fast` checks the property type only. 
Even if the property value implements `INotifyPropertyChanged2`, if the property type is `System.Object`, it will not be monitored. 

```cs
var controller = new OperationController();

// the object that implements INotifyPropertyChanged
var person = new Person() { Name = "Venus", Age=13 };
// monitor Name property
controller.BindPropertyChanged(person, nameof(Person.Name), false);
person.Name = "Yammada";
person.Name = "Tanaka";
Assert.AreEqual(2, controller.Undos.Count());
// This is not recorded, because Age is not monitored
person.Age = 15;
Assert.AreEqual(2, controller.Undos.Count());


// the object that implements INotifyPropertyChanged2
var hiperson = new HiPerson() { Name = "Venus", Age=13 };
// All properties of HiPerson is monitord
controller.BindPropertyChanged2(hiperson);
hiperson.Name = "A002";
Assert.AreEqual(3, controller.Undos.Count());
hiperson.Age = 14;
Assert.AreEqual(4, controller.Undos.Count());
```

# Nuget
https://www.nuget.org/packages/TsOpUndo/

# Development

IDE: Visual Studio 2019  

## Build

1. Clone the repository
   ```cmd
   git clone https://github.com/whistyun/TsOpUndo.git
   ```

2. If you use VisualStudio, open TsOpUndo.git.
