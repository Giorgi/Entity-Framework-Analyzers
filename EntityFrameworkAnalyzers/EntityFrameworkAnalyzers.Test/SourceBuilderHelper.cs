using System;
using System.Linq;

namespace EntityFrameworkAnalyzers.Test
{
    static class SourceBuilderHelper
    {
        private const string MethodAndClass =
            @"namespace ConsoleApplication1
{{
    class TypeName
    {{
        public void Test()
        {{
            {0}
        }}
    }}
}}";
        private const string Class =
            @"namespace ConsoleApplication1
{{
    class TypeName
    {{   
        {0}
    }}
}}";
        public static string AddMethodAndClass(this string body, params string[] namespaces)
        {
            var source = string.Format(MethodAndClass, body);

            return AddUsings(namespaces, source);
        }

        public static string AddClass(this string body, params string[] namespaces)
        {
            var source = string.Format(Class, body);

            return AddUsings(namespaces, source);
        }

        public static string FormatWith(this string template, params string[] arguments)
        {
            return string.Format(template, arguments);
        }

        private static string AddUsings(string[] namespaces, string source)
        {
            foreach (var ns in namespaces.Reverse())
            {
                source = $"using {ns};{Environment.NewLine}{Environment.NewLine}{source}";
            }

            return source;
        }
    }
}