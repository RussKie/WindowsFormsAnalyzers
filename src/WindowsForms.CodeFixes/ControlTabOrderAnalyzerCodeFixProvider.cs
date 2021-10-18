// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WindowsForms.Analyzers;

namespace WindowsForms
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ControlTabOrderAnalyzerCodeFixProvider)), Shared]
    public class ControlTabOrderAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIds.InconsistentTabIndexRuleId); }
        }

        public sealed override FixAllProvider? GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            // For now disable 'Fix All'
            return null;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();

            if (diagnostic.AdditionalLocations is null || !diagnostic.AdditionalLocations.Any())
            {
                // The code has likely been modified by hand, we can't auto-fix it.
                return Task.CompletedTask;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.ControlTabOrderAnalyzerCodeFixTitle,
                    createChangedDocument: cancellationToken => ReorderControlsOrderAsync(diagnostic, context.Document, cancellationToken),
                    equivalenceKey: nameof(CodeFixResources.ControlTabOrderAnalyzerCodeFixTitle)),
                diagnostic);

            return Task.CompletedTask;
        }

        private async Task<Document> ReorderControlsOrderAsync(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
        {
            if (diagnostic.AdditionalLocations is null || !diagnostic.AdditionalLocations.Any())
            {
                throw new InvalidOperationException();
            }

            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not SyntaxNode root)
            {
                throw new InvalidOperationException();
            }

            // This is our statement that generated the dianostics: 'this.Controls.Add(this.button1);'
            // NB: this statement will contain _all_ preceding trivia, such as comments, e.g.:
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button1);
            StatementSyntax errorStatement = (StatementSyntax)root.FindNode(diagnostic.Location.SourceSpan);

            // Get InitializeComponent method's syntax
            if (errorStatement.Parent is not BlockSyntax originalBlock)
            {
                // Something is wrong, not going to fix anything.
                throw new InvalidOperationException();
            }

            // Reconcile Z-order and TabIndex and decide which direction we need to move the statement
            int zorder = int.Parse(diagnostic.Properties["ZOrder"]);
            int tabIndex = int.Parse(diagnostic.Properties["TabIndex"]);
            int offset = tabIndex - zorder;

            // Find the location of the statement that generated the dianostics
            SyntaxList<StatementSyntax> originalStatements = originalBlock.Statements;
            int errorStatementLocation = originalStatements.IndexOf(errorStatement);
            SyntaxList<StatementSyntax> updatedStatements = offset switch
            {
                1 => MoveDownOne(originalStatements, errorStatement, errorStatementLocation),
                > 1 => MoveDown(originalStatements, errorStatement, errorStatementLocation, offset),
                < 0 => MoveUp(originalStatements, errorStatement, errorStatementLocation, offset),
                _ => /* We shouldn't be here */ throw new InvalidOperationException(),
            };

            // Create the new InitializeComponent method's syntax
            BlockSyntax newBlock = originalBlock.WithStatements(updatedStatements);

            // Rewrite the document
            return document.WithSyntaxRoot(root.ReplaceNode(originalBlock, newBlock));
        }

        private SyntaxList<StatementSyntax> MoveDownOne(in SyntaxList<StatementSyntax> originalStatements, in StatementSyntax errorStatement, int errorStatementLocation)
        {
            const int offset = 1;

            // Now get the statment that follows our statement, so we can swap the leading trivia.
            StatementSyntax nextStatement = originalStatements[errorStatementLocation + offset];

            // Update statements swapping the leading trivia, e.g.:
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button1);
            //   this.Controls.Add(this.button2);
            //
            // will turn into
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button2);
            //   this.Controls.Add(this.button1);
            //
            StatementSyntax updatedErrorStatement = errorStatement.WithLeadingTrivia(nextStatement.GetLeadingTrivia());
            StatementSyntax updatedNextStatement = nextStatement.WithLeadingTrivia(errorStatement.GetLeadingTrivia());

            // Perform the re-order
            return originalStatements
                     // Remove the following statement first...
                     .RemoveAt(errorStatementLocation + offset)
                     // ...then remove the statement we're moving.
                     .RemoveAt(errorStatementLocation)
                     // Now insert the following statement with the leading trivia of the statement we're moving...
                     .Insert(errorStatementLocation, updatedNextStatement)
                     // ...and insert the statement we're moving with the leading trivia of the original next statement.
                     .Insert(errorStatementLocation + offset, updatedErrorStatement);
        }

        private SyntaxList<StatementSyntax> MoveDown(in SyntaxList<StatementSyntax> originalStatements, in StatementSyntax errorStatement, int errorStatementLocation, int offset)
        {
            // Now get the statment that follows our statement, so we can swap the leading trivia.
            StatementSyntax nextStatement = originalStatements[errorStatementLocation + 1];

            // Update the next statements the leading trivia of the statement we move, e.g.:
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button3);
            //   this.Controls.Add(this.button1);
            //   this.Controls.Add(this.button2);
            //
            // will turn into
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button1);
            //   this.Controls.Add(this.button2);
            //
            StatementSyntax updatedNextStatement = nextStatement.WithLeadingTrivia(errorStatement.GetLeadingTrivia());

            // Remove the error statement and shift next statement in its place
            SyntaxList<StatementSyntax> statements = originalStatements
                                                          // Remove the next statement first...
                                                          .RemoveAt(errorStatementLocation + 1)
                                                          // ...then remove the statement we're moving.
                                                          .RemoveAt(errorStatementLocation)
                                                          // Now insert the following statement with the leading trivia of the statement we're moving...
                                                          .Insert(errorStatementLocation, updatedNextStatement);

            // This is the statement at the target location that will preceed the statement we move
            StatementSyntax targetStatement = originalStatements[errorStatementLocation + offset];

            StatementSyntax updatedErrorStatement = errorStatement.WithLeadingTrivia(targetStatement.GetLeadingTrivia());

            // Insert the error statement at the new location
            // i.e. turn into
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button1);
            //   this.Controls.Add(this.button2);
            //   this.Controls.Add(this.button3);
            //
            statements = statements.Insert(errorStatementLocation + offset, updatedErrorStatement);

            return statements;
        }

        private SyntaxList<StatementSyntax> MoveUp(in SyntaxList<StatementSyntax> originalStatements, in StatementSyntax errorStatement, int errorStatementLocation, int offset)
        {
            // Now get the statment that follows our statement, so we can swap the leading trivia.
            StatementSyntax targetStatement = originalStatements[errorStatementLocation + offset];

            // Update statements swapping the leading trivia, e.g.:
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button2);
            //   this.Controls.Add(this.button3);
            //   this.Controls.Add(this.button1);
            //
            // will turn into
            //
            //   // 
            //   // Form1
            //   // 
            //   this.Controls.Add(this.button1);
            //   this.Controls.Add(this.button2);
            //   this.Controls.Add(this.button3);
            //
            StatementSyntax updatedErrorStatement = errorStatement.WithLeadingTrivia(targetStatement.GetLeadingTrivia());
            StatementSyntax updatedNextStatement = targetStatement.WithLeadingTrivia(errorStatement.GetLeadingTrivia());

            // Perform the re-order
            return originalStatements
                     // Remove the statement we're moving first...
                     .RemoveAt(errorStatementLocation)
                     // ...then remove the target statement.
                     .RemoveAt(errorStatementLocation + offset)
                     // Now insert the next statement with the leading trivia of the statement we're moving...
                     .Insert(errorStatementLocation + offset, updatedNextStatement)
                     // ...and insert the statement we're moving with the leading trivia of the original next statement.
                     .Insert(errorStatementLocation + offset, updatedErrorStatement);
        }
    }
}
