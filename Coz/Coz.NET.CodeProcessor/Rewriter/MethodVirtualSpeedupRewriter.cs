using System;
using System.Collections.Generic;
using System.Linq;
using Coz.NET.Profiler.Profile;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Coz.NET.CodeProcessor.Rewriter
{
    public class MethodVirtualSpeedupRewriter : CSharpSyntaxRewriter
    {
        private readonly List<Compilation> compilations;

        public MethodVirtualSpeedupRewriter(List<Compilation> compilations)
        {
            this.compilations = compilations;
        }

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var usingSyntax = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("Coz.NET.Profiler"),  SyntaxFactory.IdentifierName("Profile"));
            node = node.AddUsings(SyntaxFactory.UsingDirective(usingSyntax).NormalizeWhitespace());

            return base.VisitCompilationUnit(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax) base.VisitMethodDeclaration(node);

            var slowdownStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.Slowdown)}();" + Environment.NewLine);
            var startRecordStatement = SyntaxFactory.ParseStatement($"var callId = {nameof(ProfileMarker)}.{nameof(ProfileMarker.Start)}();" + Environment.NewLine);
            var endRecordStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.End)}(callId);" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>(node.Body.Statements);
            statements = statements.Insert(0, startRecordStatement);
            statements = statements.Insert(0, slowdownStatement);
            
            if (!node.DescendantNodes().Any(x => x is ReturnStatementSyntax))
            {
                statements = statements.Add(endRecordStatement);
                var blockSyntax = SyntaxFactory.Block(statements);
                node = node.WithBody(blockSyntax);
            }
            else
            {
                var blockSyntax = SyntaxFactory.Block(statements);
                node = node.WithBody(blockSyntax);
            }

            return node;
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            var endRecordStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.End)}(callId);" + Environment.NewLine);
            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>();
            statements = statements.Add(endRecordStatement);
            statements = statements.Add(node);
            var blockSyntax = SyntaxFactory.Block(statements);

            return blockSyntax;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var invocationExpression = node.Expression as InvocationExpressionSyntax;

            if (!(invocationExpression?.Expression is IdentifierNameSyntax))
                return base.VisitExpressionStatement(node);

            var methodName = ((IdentifierNameSyntax)invocationExpression.Expression).Identifier.ValueText;

            if (!compilations.Any(x => x.ContainsSymbolsWithName(methodName, SymbolFilter.Member)))
                return base.VisitExpressionStatement(node);

            var resumeRecordStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.Resume)}(callId);" + Environment.NewLine); 
            var pauseRecordStatement = SyntaxFactory.ParseStatement($"{nameof(ProfileMarker)}.{nameof(ProfileMarker.Pause)}(callId);" + Environment.NewLine);

            SyntaxList<StatementSyntax> statements = new SyntaxList<StatementSyntax>(node);
            statements = statements.Add(resumeRecordStatement);
            statements = statements.Insert(0, pauseRecordStatement);
            var blockSyntax = SyntaxFactory.Block(statements);

            return blockSyntax;
        }
    }
}
