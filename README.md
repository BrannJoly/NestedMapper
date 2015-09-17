# NestedMapper

## What is this and why do I need it?

If you're using some sort of light ORM (eg. Dapper) and if your data is flat in your database and hierarchical in your .net application, chances are you're either doing your mapping manually, or are using AutoMapper.

NestedMapper will take a sample object, your .net type, and turn that into a Func<dynamic, T> you can use to quickly transform one into the other.

Unlike AutoMapper, you won't have to maintain a mapping configuration; just give NestedMapper the .net type and a sample flat object, and it will return a lambda you can use immediately.

## Features

- can deal with any type of flat object, including DapperRows, classic ExpandoObjects, and user-defined .net types
- will manage to map a variety of fields even if the types don't trivially match (nullable types, enums, implicit conversions...)
- unlimited levels of nesting supported (even though you'll rarely use more than one)
- native-like performance thanks to Expression Trees
- helpful error messages when mapping fails.
- explicit-constructor aware (no need to have a default empty constructor)
- uses default constructor/object initializers if available
- bidirectional mapping (ie you can also use the mapper to generate a dynamic flat object, and possibly save it back to a database)

## Sample code

```csharp
public class Foo
{
    public string Name { get; set; }
    public Point Position { get; set; }
}

var flatFoos = connection.Query("select Name, X, Y from tbFoo"); 

var mapper = MapperFactory.GetMapper<Foo>(foos.First()); // can be done at init time and cached somewhere if needed

var foos = flatFoos.select(x=> mapper(x));
```


## How does this work?

The mapper will turn your target type into a tree and travel it. It then builds a big ExpressionTree matching the tree structure, compiles it and returns it. No magic involved!

In an ideal world, we would iterate over the entry types, and make sure we find all of them in the correct order while recursively traveling the target type hierarchy.
In practice, there are a couple of non-trivial issues that can arise:

### Type conversion gotchas

More often than not, your types won't exactly match. Typically, your database will contain an int and your code an Enum, or a nullable int. Or maybe there's an implicit conversion to apply (that's typically the case when you use a tool like stidgen)

Figuring out whether two types are compatible can be quite nasty, and NestedMapper supports the most common scenarios. 
If you stumble unto something that's not supported (eg array covariance), let me know and I'll try to implement it.

### null columns in the sample data

The sample object can be a dynamic containing nulls. In that case, the mapper has no idea what the type is. 
Let's say we've travelled to a Point in our target .net type. Is the source object going to contain a Point (in which case we should keep travelling horizontally), or is it going to contain an int (in which case we should recurse into the Point and set its x property). 
There's are two ways to address this:
- try both options and see if one of them yields a correct mapping function
- provide additional information to the mapper.

I went for the second (and easier) option, i.e. you can tell NestedMapper that your sample object is never going to contain Points, so we can safely assume we need to recurse on Points when we meet them in the target type. This is what AssumeNullWontBeMappedToThoseTypes is for.

## What if it doesn't work?

In theory, NestedMapper is plug-n-play. If it's not:
- If you get a "Name mismatch for property" exception, the problem should probably be fixed on your end. If you know what you're doing, you can use NamesMismatch.AlwaysAllow, but be aware it might prevent you from catching some bugs later on when you add fields to your object.
- If you have nulls in your sample object, you might need to use AssumeNullWontBeMappedToThoseTypes to help the mapper figure out how to travel your type hierarchy
- Maybe NestedMapper is right. it should provide helpful error messages to let you know why it won't map your stuff...
- ... Or maybe there's a bug. Please feel free to submit an issue and/or a pull request.