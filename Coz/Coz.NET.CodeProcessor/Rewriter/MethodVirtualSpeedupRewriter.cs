using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Coz.NET.CodeProcessor.Rewriter
{
    public class MethodVirtualSpeedupRewriter : CSharpSyntaxRewriter
    {


        public MethodVirtualSpeedupRewriter()
        {

        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var statement = SyntaxFactory.ParseStatement(@"Console.WriteLine(""Injected"");" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>(node.Body.Statements);
            statements = statements.Insert(0, statement);
            var blockSyntax = SyntaxFactory.Block(statements);
            node = node.WithBody(blockSyntax);

            return base.VisitMethodDeclaration(node);
        }

        private static string GetVirtualSpeedupSnippet()
        {
            var builder = new StringBuilder();
            builder.AppendLine(@"Console.WriteLine(""Injected"");");

        }
    }
}
