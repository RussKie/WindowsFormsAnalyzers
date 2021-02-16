#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace WindowsForms.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ControlTabOrderAnalyzer : DiagnosticAnalyzer
    {
        public static class DiagnosticIds
        {

            public const string NonNumericTabIndexValue = "SWFA0001";

            public const string InconsistentTabIndex = "SWFA0010";

        }

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        private const string Category = "Accessibility";

        private static readonly DiagnosticDescriptor NonNumericTabIndexValueRule
            = new(DiagnosticIds.NonNumericTabIndexValue,
                  "Ensure numeric controls tab order value",
                  "Control '{0}' has unexpected TabIndex value.",
                  Category,
                  DiagnosticSeverity.Warning,
                  isEnabledByDefault: true,
                  "Avoid manually editing \"InitializeComponent()\" method.");

        private static readonly DiagnosticDescriptor InconsistentTabIndexRule
            = new(DiagnosticIds.InconsistentTabIndex,
                  "Verify correct controls tab order",
                  "Control '{0}' has a different TabIndex value to its order in the parent's control collection.",
                  Category,
                  DiagnosticSeverity.Warning,
                  isEnabledByDefault: true,
                  "Remove TabIndex assignments and re-order controls in the parent's control collection.");

        // Contains the list of fields and local controls that we need to check for TabIndex property value.
        private readonly List<string> _controls = new();
        private readonly Dictionary<string, int> _controlsIndex = new();


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(InconsistentTabIndexRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterOperationBlockAction(CodeBlockAction);
        }

        private void CodeBlockAction(OperationBlockAnalysisContext context)
        {
            // We only care about "InitializeComponent" method.
            if (context.OwningSymbol is
                    not { Kind: SymbolKind.Method, Name: "InitializeComponent" } and
                    not { Kind: SymbolKind.Field }) // TODO: fields contained in the same class as InitializeComponent
            {
                return;
            }

            foreach (IOperation operationBlock in context.OperationBlocks)
            {
                if (operationBlock.Kind == OperationKind.FieldInitializer)
                {
                    // "        private System.Windows.Forms.TreeView treeView1;"
                    IFieldInitializerOperation fieldInitializerOperation = (IFieldInitializerOperation)operationBlock;

                    // "treeView1", etc.
                    _controls.AddRange(fieldInitializerOperation.InitializedFields.Select(field => field.Name));

                    continue;
                }

                if (operationBlock is not IBlockOperation blockOperation)
                {
                    continue;
                }

                foreach (IOperation operation in blockOperation.Operations)
                {
                    switch (operation.Kind)
                    {
                        case OperationKind.VariableDeclarationGroup:
                            {
                                // "            System.Windows.Forms.Button button3 = new System.Windows.Forms.Button();"
                                var variableDeclarationGroupOperation = (IVariableDeclarationGroupOperation)operation;
                                IVariableDeclarationOperation variableDeclarationOperation = variableDeclarationGroupOperation.Declarations.First(op => op.Kind == OperationKind.VariableDeclaration);
                                IVariableDeclaratorOperation variableDeclaratorOperation = variableDeclarationOperation.Declarators.First(op => op.Kind == OperationKind.VariableDeclarator);
                                var variableDeclaratorSyntax = (VariableDeclaratorSyntax)variableDeclaratorOperation.Syntax;

                                // "button3"
                                _controls.Add(variableDeclaratorSyntax.Identifier.ValueText);
                            }
                            break;

                        case OperationKind.ExpressionStatement:
                            {
                                var expressionStatementOperation = (IExpressionStatementOperation)operation;

                                // Look for ".Controls.Add"
                                if (expressionStatementOperation.Operation is IOperation invocationOperation &&
                                    invocationOperation.Syntax is InvocationExpressionSyntax expressionSyntax)
                                {
                                    ParseControlAddStatements(context, expressionSyntax);
                                    continue;
                                }

                                // Look for ".TabIndex = <x>"
                                if (expressionStatementOperation.Operation is IAssignmentOperation assignmentOperation)
                                {
                                    ParseTabIndexAssignments(context, (AssignmentExpressionSyntax)assignmentOperation.Syntax);
                                    continue;
                                }

                            }
                            break;

                        default:
                            break;
                    }
                }

                Diagnostic diagnostic = Diagnostic.Create(InconsistentTabIndexRule, Location.None, operationBlock.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void ParseControlAddStatements(OperationBlockAnalysisContext context, InvocationExpressionSyntax expressionSyntax)
        {
            if (!expressionSyntax.Expression.ToString().EndsWith(".Controls.Add"))
            {
                return;
            }

            var syntax = expressionSyntax.Expression;
        }

        private void ParseTabIndexAssignments(OperationBlockAnalysisContext context, AssignmentExpressionSyntax expressionSyntax)
        {
            var propertyNameExpressionSyntax = (MemberAccessExpressionSyntax)expressionSyntax.Left;
            SimpleNameSyntax propertyNameSyntax = propertyNameExpressionSyntax.Name;

            if (propertyNameSyntax.Identifier.ValueText != "TabIndex")
            {
                return;
            }

            string controlName;
            if (propertyNameExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax)
            {
                // local variable, e.g. button3.TabIndex = 0;
                controlName = identifierNameSyntax.Identifier.ValueText;
            }
            else if (propertyNameExpressionSyntax.Expression is MemberAccessExpressionSyntax controlNameExpressionSyntax)
            {
                // field, e.g. this.button1.TabIndex = 1;
                controlName = controlNameExpressionSyntax.Name.Identifier.ValueText;
            }
            else
            {
                Debug.Fail("How did we get here?");
                return;
            }

            if (expressionSyntax.Right.Kind() != Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression)
            {
                Diagnostic diagnostic1 = Diagnostic.Create(NonNumericTabIndexValueRule,
                    Location.Create(expressionSyntax.Right.SyntaxTree, expressionSyntax.Right.Span),
                    controlName);
                context.ReportDiagnostic(diagnostic1);
                return;
            }

            var propertyValueExpressionSyntax = (LiteralExpressionSyntax)expressionSyntax.Right;
            int tabIndexValue = (int)propertyValueExpressionSyntax.Token.Value;

            // "button3:0"
            _controlsIndex[controlName] = tabIndexValue;
        }
    }
}
