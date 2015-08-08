# NestedMapper
NestedMapper aims at automating the task of mapping structurally different objects representing the same information.

Let's say for example you have a type defined as such :

```
public class FooPosition
      {
          public string Name { get; set; }

          public Point Position { get; set; }
      }
```

It wouldn't be unusual for this data to be stored in a database in a Foo(Name, x, y) table

If you try using Dapper to get Foo objects, it won't magically work, and you'll have to manually write the mapping between Dapper's expando object and your own class, which is boring and error-prone

NestedMapper is a small library that automatically creates this mapping code. And since it compiles the mapping code, the performance is similar to the code you would have written manually.

Here is a sample call to the library :

```
        dynamic flatfoo = new ExpandoObject();
        flatfoo.Name = "Foo";
        flatfoo.x = 45;
        flatfoo.y = 200;

        var foo = MapperFactory.GetMapper<FooPosition>(MapperFactory.PropertyNameEnforcement.InNestedTypesOnly, flatfoo).Map(flatfoo);

```
