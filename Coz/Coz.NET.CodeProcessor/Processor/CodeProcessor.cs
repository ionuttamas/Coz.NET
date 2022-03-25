using System.IO;
using System.Linq;
using Coz.NET.CodeProcessor.Rewriter;
using Coz.NET.Profiler.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis; 
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace Coz.NET.CodeProcessor.Processor
{
    public class CodeProcessor
    { 
        public void RegenerateSolution(CodeLocation codeLocation)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            var workspace = MSBuildWorkspace.Create();
            CopyFilesRecursively(codeLocation.SolutionFolder, codeLocation.GeneratedSolutionFolder);
            var solution = workspace.OpenSolutionAsync(codeLocation.GeneratedSolutionPath).Result;
            var compilations = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects()
                .Select(x => solution.GetProject(x))
                .Where(x => x.SupportsCompilation)
                .Select(x => x.GetCompilationAsync().Result)
                .ToList();
            var rewriter = new MethodVirtualSpeedupRewriter(compilations);

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

            MSBuildLocator.Unregister();
        }

        public void BuildProjects(CodeLocation codeLocation)
        {
            //TODO: not working
            ProcessUtils.StartProcess("dotnet", $@"build ""{codeLocation.GeneratedSolutionPath}"" /p:Configuration=Release");
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

    public class CodeLocation
    {
        public string SolutionFolder { get; set; }
        public string GeneratedSolutionFolder { get; set; }
        public string SolutionFilename { get; set; }
        public string GeneratedSolutionPath => Path.Combine(GeneratedSolutionFolder, SolutionFilename);
    }
}
