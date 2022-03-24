using System;
using System.Linq;
using Coz.NET.Profiler.Profile;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Coz.NET.CodeProcessor.Rewriter
{
    public class MethodVirtualSpeedupRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var usingSyntax = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("Coz.NET.Profiler"),  SyntaxFactory.IdentifierName("Profile"));
            node = node.AddUsings(SyntaxFactory.UsingDirective(usingSyntax).NormalizeWhitespace());

            return base.VisitCompilationUnit(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var slowdownStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.Slowdown)}();" + Environment.NewLine);
            //TODO: this may conflict with other variables
            var startRecordStatement = SyntaxFactory.ParseStatement($"var callId = {nameof(ProfileMarker)}.{nameof(ProfileMarker.StartRecord)}();" + Environment.NewLine);
            var endRecordStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.EndRecord)}(callId);" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>(node.Body.Statements);
            statements = statements.Insert(0, startRecordStatement);
            statements = statements.Insert(0, slowdownStatement);

            if (!node.DescendantNodes().Any(x => x is ReturnStatementSyntax))
            {
                statements = statements.Add(endRecordStatement);
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
            var endRecordStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.EndRecord)}(callId);" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>();
            statements = statements.Add(endRecordStatement);
            statements = statements.Add(node);
            var blockSyntax = SyntaxFactory.Block(statements);

            return blockSyntax;
        }
    }
}
