# NestedMapper

NestedMapper aims at automating the task of mapping structurally different objects representing the same information.
This is typically the case when you want to map a flat object you retrieved from a database to a .net object containing nested classes.


## Typical use case
Let's say for example you have a type defined as such:

```
public class NestedType
    {

        public int X { get; set; }
        public int Y { get; set; }
    }

public class Foo
    {
        public string Name { get; set; }
        public NestedType Position { get; set; }
    }
```

It wouldn't be unusual for this data to be stored in a table defined this way:

```
CREATE TABLE Foo(
	Name [varchar](50) NOT NULL,
	X integer NOT NULL,
	y integer NOT NULL
)
```

And dapper will give you an object looking like this:

```
dynamic flatfoo = new ExpandoObject();
flatfoo.Name = "Foo";
flatfoo.x = 45;
flatfoo.y = 200;

```


If you try using Dapper to get Foo objects, it won't magically work, and you'll have to manually write the mapping between Dapper's expando object and your own class, which is boring and error-prone, eg:

```
var foo = new Foo
{
    Name = flatfoo.Name,
    Position =
    {
        X = flatfoo.x,
        Y = flatfoo.y
    }
};
````

Using NestedMapper, here's what you would do:

```
        var mapper = MapperFactory.GetMapper<Foo>(MapperFactory.PropertyNameEnforcement.InNestedTypesOnly, flatfoo);
        var foo = mapper.Map(flatfoo);

```

## How does this work ?

The mapper will flatten the target object, and then check that source and target objects have the same number of properties, and that their types perfectly match.

The way names mismatch are handled can be configured using the NamesMismatch enum:
-AlwaysAllow will ignore names checks completely
-NeverAllow will enforce strict member name equality everywhere
-AllowInNestedTypesOnly will enforce matching field names only at the root of the mapped object.

## default constructors requirement

If there's a default constructor available for nested objects, the mapper will call it. If there's not, the mapper will just hope the parents take care of initializing their child objects propertly. If they don't, you'll get a nullref.

## performance

NestedMapper code performance is quite similar to a manual implementation like the one below. This is enforced in the unit tests.
