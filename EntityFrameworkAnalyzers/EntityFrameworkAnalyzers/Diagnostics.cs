namespace EntityFrameworkAnalyzers
{
    public static class Diagnostics
    {
        public const string UseIncludeWithLambdaAnalyzerDiagnosticId = "EF1000";
        public const string ConstructorCallAnalyzerDiagnosticId = "EF1001";
        public const string UsePaginationWithLambdaOverloadAnalyzerDiagnosticId = "EF1002";
    }

    static class Namespaces
    {
        internal static class System
        {
            internal static class Data
             {
                 internal const string Entity = "System.Data.Entity";
             }
        }
    }

    static class Categories
    {
        public const string Usage = "Usage";
        public const string Performance = "Performance";
    }
}