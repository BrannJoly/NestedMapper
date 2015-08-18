# NestedMapper

## What is this and why do I need it?

If you're using some sort of light ORM (eg. Dapper) and if your data is flat in the database and hierarchical in your .net application, chances are you're either doing your mapping manually, or are using AutoMapper.

NestedMapper will take a sample object, your .net type, and turn that into a Func<dynamic,T> you can use to quickly transform one into the other.

Unlike AutoMapper, you won't have to maintain a mapping configuration, just give NestedMapper the .net type and a sample flat object, and it will return a lambda you can use immediately.

## Features:

- can deal with any type of flat object, including DapperRows, classic ExpandoObjects, and user-defined .net types
- will manage to map a variety of fields even if the types don't trivially match (nullable types, enums, implicit converstions...)
- unlimited levels of nesting supported (eventhough you'll rarely use more than one)
- native-like performance thanks to Expression Trees
- Helpfull error messages when mapping fails.
- explicit-constructor aware (no need to have a default empty constructor)
- uses default constructor/object initializers if available

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



## How does this work ?

The mapper will turn your target type into a tree and travel it. It then builds a big ExpressionTree matching the tree stucture, compiles it and returns it. No magic involved!

In an ideal world, we would iterate over the entry types, and make sure we find all of them in the correct order while recursively traveling the type.
In practice, there are a couple of non-trivial issues that can arise:
- Maybe the types don't exactly match (ie Int->Enum, of Int -> Int? or Int -> Decimal. Figuring out whether two types are compatible can be quite nasty.
- The sample object can be a dynamic containing nulls. In that case, we have no idea what the type is. Let's say we've travelled to a Point in our .net object. Is the source object going to containg a Point (in which case we should keep travelling honrizontally), or is it going to contain an int (in which case we should recurse into the Point). In that case, you can tell NestedMapper that your sample object is never going to contain Points, so we can safely assume we need to recurse on Points when we meet them. This is what AssumeNullWontBeMappedToThoseTypes is for.


## What if it doesn't work?

If the mapper wasn't plug-n-play:
- If you get a "Name mismatch for property" exception, the problem should probably be fixed on your end. If you know what you're doing, you can use NamesMismatch.AlwaysAllow, but be aware it might prevent you from catching some bugs later on when you add fields to your object.
- If you have nulls in your sample object, you might need to use AssumeNullWontBeMappedToThoseTypes to help the mapper figure out how to travel your type hierarchy
- Maybe the mapper is right. it should provide helpful error messages to let you know if won't map.
- Or maybe there's a bug. Please feel free to submit an issue and/or a pull request.