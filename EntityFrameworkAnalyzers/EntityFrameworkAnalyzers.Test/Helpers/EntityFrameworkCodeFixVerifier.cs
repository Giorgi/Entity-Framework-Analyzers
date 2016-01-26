using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

using TestHelper;
using Microsoft.CodeAnalysis.Text;

namespace EntityFrameworkAnalyzers.Test.Helpers
{
    public class EntityFrameworkCodeFixVerifier : CodeFixVerifier
    {
        private static readonly MetadataReference EntityFrameworkReference = MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location);
        private static readonly MetadataReference IListSourceReference = MetadataReference.CreateFromFile(typeof(IListSource).Assembly.Location);

        protected override Project ExtendProject(Project project)
        {
            var newFileName = DefaultFilePathPrefix + project.Documents.Count() + "." + CSharpDefaultFileExt;

            return project.AddMetadataReference(IListSourceReference)
                          .AddMetadataReference(EntityFrameworkReference)
                          .AddDocument(newFileName, SourceText.From(Properties.Resources.Model)).Project;
        }

        protected string FormatSource(string source)
        {
            var syntaxNode = SyntaxFactory.ParseCompilationUnit(source);

            var formatted = Formatter.Format(syntaxNode, MSBuildWorkspace.Create());

            return formatted.ToString();
        }
    }
}