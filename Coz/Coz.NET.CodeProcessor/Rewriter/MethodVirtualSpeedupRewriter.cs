using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Coz.NET.CodeProcessor.Rewriter
{
    public class MethodVirtualSpeedupRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var statement = SyntaxFactory.ParseStatement(@"Console.WriteLine(""Injected"");" + Environment.NewLine);
            var endStatement = SyntaxFactory.ParseStatement(@"Console.WriteLine(""Before Return"");" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>(node.Body.Statements);
            statements = statements.Insert(0, statement);

            if (!node.DescendantNodes().Any(x => x is ReturnStatementSyntax))
            {
                statements = statements.Add(endStatement);
                var blockSyntax = SyntaxFactory.Block(statements);
                node = node.WithBody(blockSyntax);

                return node;
            }
            else
            {
                var blockSyntax = SyntaxFactory.Block(statements);
                node = node.WithBody(blockSyntax);
                
                return base.VisitMethodDeclaration(node);
            }
        }

        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            var statement = SyntaxFactory.ParseStatement(@"Console.WriteLine(""Before Return"");" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>();
            statements = statements.Add(statement);
            statements = statements.Add(node);
            var blockSyntax = SyntaxFactory.Block(statements);

            return blockSyntax;
        }

        private static string GetVirtualSpeedupSnippet()
        {
            var builder = new StringBuilder();
            builder.AppendLine(@"Console.WriteLine(""Injected"");");
            return "";
        }
    }
}
