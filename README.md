# Entity Framework Analyzers [![NuGet Version](https://img.shields.io/nuget/v/EntityFrameworkAnalyzers.svg?style=flat)](https://www.nuget.org/packages/EntityFrameworkAnalyzers/)

Code Analyzers and Fixers for Common Entity Framework Issues built by [.NET Compiler Platform ("Roslyn")](https://github.com/dotnet/roslyn)

Currently there is only one analyzer available: 

**Use Include with lambda**
> Use Include method with lambda overload. This provides compile time validation and refactoring support

![Use Include with lambda](Docs/IncludeWithLambda.PNG)

###How to get it

Entity Framework Analyzers is available on Nuget. To add the analyzers to your project run the following command in the Visual Studio Package Manager Console:

``` 
PM> Install-Package EntityFrameworkAnalyzers 
```

