using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace Coz
{
    class Program
    {
        static void Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults();
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (sender, e) => Console.WriteLine($"[failed] {e.Diagnostic}");
            var solutionFile = @"TestApp.sln";
            var solutionFolder = @"C:\Users\tamas\Documents\Coz.NET\TestApp";
            var copiedSolutionFolder = @"C:\Users\tamas\Documents\Coz.NET\TestApp2";
            CopyFilesRecursively(solutionFolder, copiedSolutionFolder);

            var newSolutionPath = Path.Combine(copiedSolutionFolder, solutionFile);
            var solution = workspace.OpenSolutionAsync(newSolutionPath).Result;
            var rewriter = new MethodVirtualSpeedupRewriter();

            foreach (ProjectId projectId in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects())
            {
                var project = solution.GetProject(projectId);

                foreach (Document document in project.Documents)
                {
                    if(document.SourceCodeKind!=SourceCodeKind.Regular)
                        continue;

                    var root = document.GetSyntaxRootAsync().Result;
                    root = rewriter.Visit(root);
                    root = Formatter.Format(root, workspace);
                    var text = root.NormalizeWhitespace().SyntaxTree.GetText().ToString();
                    File.WriteAllText(document.FilePath, text);
                }
            }

            Console.WriteLine("Hello World!");
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public class MethodVirtualSpeedupRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                var statement = SyntaxFactory.ParseStatement(@"Console.WriteLine(""Injected"");"+Environment.NewLine);
                SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>(node.Body.Statements);
                statements = statements.Insert(0, statement);
                var blockSyntax = SyntaxFactory.Block(statements);
                node = node.WithBody(blockSyntax); 
                return base.VisitMethodDeclaration(node);
            }
        }
    }
}
