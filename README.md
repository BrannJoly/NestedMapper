# NestedMapper

NestedMapper aims at automating the task of mapping structurally different objects representing the same information.

## Typical use case
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

## Sample call

Here is a sample call to the library to map to the above object:

```
        dynamic flatfoo = new ExpandoObject();
        flatfoo.Name = "Foo";
        flatfoo.x = 45;
        flatfoo.y = 200;

        var mapper = MapperFactory.GetMapper<FooPosition>(MapperFactory.PropertyNameEnforcement.InNestedTypesOnly, flatfoo);
        var foo = mapper.Map(flatfoo);

```

## Mapper configuration

The mapper will flatten the target object, and then check that source and target objects have the same number of properties, and that their types perfectly match.
the PropertyNameEnforcement enum controls whether mismatching names are allowed.
Always and Never are self explanatory, and InNestedTypesOnly will only enforce perfectly matching names for properties defined at the top of the target object
