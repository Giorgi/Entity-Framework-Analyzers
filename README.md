# Entity Framework Analyzers [![NuGet Version](https://buildstats.info/nuget/EntityFrameworkAnalyzers)](https://www.nuget.org/packages/EntityFrameworkAnalyzers/) [![Build status](https://ci.appveyor.com/api/projects/status/jh5ula20jdqsser3?svg=true)](https://ci.appveyor.com/project/Giorgi/entity-framework-analyzers)


Code Analyzers and fixes for common Entity Framework issues built by [.NET Compiler Platform ("Roslyn")](https://github.com/dotnet/roslyn)

## Available Analyzers

**Use Include method with lambda expression overload**
> Use Include method with lambda expression overload. This provides compile time validation and refactoring support


**Use Skip/Take with lambda expression**
> Using Skip/Take overload which takes lambda expression will generate parameterized sql statement. Query execution plan for the statement can be reused for different values of parameters.


**Calling class constructor on Queryable will throw an exception**
> Calling class constructor on Queryable will throw an exception because it cannot be translated to sql statement


##How to get it

Entity Framework Analyzers is available on Nuget. To add the analyzers to your project run the following command in the Visual Studio Package Manager Console:

``` 
PM> Install-Package EntityFrameworkAnalyzers 
```

