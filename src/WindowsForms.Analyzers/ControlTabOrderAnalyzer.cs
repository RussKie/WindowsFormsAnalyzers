using System.Collections.Generic;
using System.Collections.Immutable;
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
        public const string DiagnosticId = "SWFA0001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private const string Title = "Verify correct controls tab order";
        public const string MessageFormat = "Control '{0}' has a different TabIndex value to its order in the parent's control collection.";
        private const string Description = "Remove TabIndex assignments and re-order controls in the parent's control collection.";

        private const string Category = "Accessibility";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
                                                                                     Title,
                                                                                     MessageFormat,
                                                                                     Category,
                                                                                     DiagnosticSeverity.Warning,
                                                                                     isEnabledByDefault: true,
                                                                                     description: Description);

        // Contains the list of fields and local controls that we need to check for TabIndex property value.
        private readonly List<string> _controls = new();


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
                        //case OperationKind.ExpressionStatement:
                        //    {
                        //        // "            this.treeView1 = new System.Windows.Forms.TreeView();"
                        //        IExpressionStatementOperation expressionStatementOperation = (IExpressionStatementOperation)operation;
                        //        if (expressionStatementOperation.Operation is not IAssignmentOperation assignmentOperation)
                        //        {
                        //            continue;
                        //        }

                        //        AssignmentExpressionSyntax syntax = (AssignmentExpressionSyntax)assignmentOperation.Syntax;
                        //        MemberAccessExpressionSyntax memberAccessExpressionSyntax = (MemberAccessExpressionSyntax)syntax.Left;
                        //        SimpleNameSyntax simpleNameSyntax = (SimpleNameSyntax)memberAccessExpressionSyntax.Name;

                        //        // "this.treeView1"
                        //        _controls.Add(simpleNameSyntax.Identifier.ValueText);
                        //    }
                        //    break;

                        case OperationKind.VariableDeclarationGroup:
                            {
                                // "            System.Windows.Forms.Button button3 = new System.Windows.Forms.Button();"
                                IVariableDeclarationGroupOperation variableDeclarationGroupOperation = (IVariableDeclarationGroupOperation)operation;
                                IVariableDeclarationOperation variableDeclarationOperation = variableDeclarationGroupOperation.Declarations.First(op => op.Kind == OperationKind.VariableDeclaration);
                                IVariableDeclaratorOperation variableDeclaratorOperation = variableDeclarationOperation.Declarators.First(op => op.Kind == OperationKind.VariableDeclarator);
                                VariableDeclaratorSyntax variableDeclaratorSyntax = (VariableDeclaratorSyntax)variableDeclaratorOperation.Syntax;


                                // "button3"
                                _controls.Add(variableDeclaratorSyntax.Identifier.ValueText);
                            }
                            break;

                        default: break;
                    }
                }

                Diagnostic diagnostic = Diagnostic.Create(Rule, Location.None, operationBlock.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
