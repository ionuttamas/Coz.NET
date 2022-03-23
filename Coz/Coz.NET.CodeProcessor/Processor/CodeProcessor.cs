using System.IO; 
using Coz.NET.CodeProcessor.Rewriter;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis; 
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace Coz.NET.CodeProcessor.Processor
{
    public class CodeProcessor
    {
        public void RegenerateSolution(string existingFolder, string copiedFolder, string solutionFile)
        {
            MSBuildLocator.RegisterDefaults();
            var workspace = MSBuildWorkspace.Create();
            CopyFilesRecursively(existingFolder, copiedFolder);
            var copiedSolutionPath = Path.Combine(copiedFolder, solutionFile);
            var rewriter = new MethodVirtualSpeedupRewriter();
            var solution = workspace.OpenSolutionAsync(copiedSolutionPath).Result;

            foreach (ProjectId projectId in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects())
            {
                var project = solution.GetProject(projectId);

                foreach (Document document in project.Documents)
                {
                    if (document.SourceCodeKind != SourceCodeKind.Regular)
                        continue;

                    var root = document.GetSyntaxRootAsync().Result;
                    root = rewriter.Visit(root);
                    root = Formatter.Format(root, workspace);
                    var text = root.NormalizeWhitespace().SyntaxTree.GetText().ToString();
                    File.WriteAllText(document.FilePath, text);
                }
            }
        }

        public void BuildProjects(string solutionPath)
        {
            MSBuildLocator.RegisterDefaults();
            var workspace = MSBuildWorkspace.Create(); 
            var solution = workspace.OpenSolutionAsync(solutionPath).Result;

            foreach (ProjectId projectId in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects())
            {
                var project = solution.GetProject(projectId);
                var compilation = project.GetCompilationAsync().Result;
                compilation.Emit(project.CompilationOutputInfo.AssemblyPath);
            }
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
    }
}
