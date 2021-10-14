// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace WindowsForms.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AccessiblePropertiesAnalyzer : DiagnosticAnalyzer
    {
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        public static readonly DiagnosticDescriptor AccessibleNameNotSetRuleIdDescriptor
            = new(DiagnosticIds.AccessibleNameNotSetRuleId,
                  "Ensure accessible properties are defined",
                  "'{0}.AccessibleName' property must be set",
                  DiagnosticCategory.AccessibilityCategory,
                  DiagnosticSeverity.Warning,
                  isEnabledByDefault: true,
                  "Avoid manually editing \"InitializeComponent()\" method.");
        public static readonly DiagnosticDescriptor AccessibleNameEmptyRuleIdDescriptor
            = new(DiagnosticIds.AccessibleNameEmptyRuleId,
                  "Ensure accessible properties are defined",
                  "'{0}.AccessibleName' property must not be empty",
                  DiagnosticCategory.AccessibilityCategory,
                  DiagnosticSeverity.Warning,
                  isEnabledByDefault: true,
                  "Avoid manually editing \"InitializeComponent()\" method.");

        // Contains the list of fields and local controls that we inspected with flags whether these controls set AccessibleName property.
        private readonly Dictionary<string, bool> _controls = new();
        // Contains the list of fields and local controls that we inspected with their first location.
        private readonly Dictionary<string, Location> _controlsLocations = new();


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(AccessibleNameNotSetRuleIdDescriptor, AccessibleNameEmptyRuleIdDescriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterOperationBlockAction(CodeBlockAction);
        }

        private void CodeBlockAction(OperationBlockAnalysisContext context)
        {
            // We only care about "InitializeComponent" method.
            if (context.OwningSymbol is not { Kind: SymbolKind.Method, Name: "InitializeComponent" })
            {
                return;
            }

            foreach (IOperation operationBlock in context.OperationBlocks)
            {
                if (operationBlock is not IBlockOperation blockOperation)
                {
                    continue;
                }

                foreach (IOperation operation in blockOperation.Operations)
                {
                    Debug.WriteLine(operation.Syntax.ToString());

                    switch (operation.Kind)
                    {
                        case OperationKind.ExpressionStatement:
                            {
                                var expressionStatementOperation = (IExpressionStatementOperation)operation;

                                // Look for ".AccessibleName = <x>"
                                if (expressionStatementOperation.Operation is IAssignmentOperation assignmentOperation)
                                {
                                    ParseTabIndexAssignments((AssignmentExpressionSyntax)assignmentOperation.Syntax, context);
                                    continue;
                                }

                            }
                            break;

                        default:
                            break;
                    }
                }

                if (_controls.Count < 1)
                {
                    // No controls - all good
                    return;
                }

                foreach (string controlName in _controls.Keys)
                {
                    if (_controls[controlName])
                    {
                        // AccessibleName is set to a non-empty string - all good.
                        continue;
                    }

                    var diagnostic = Diagnostic.Create(
                        descriptor: AccessibleNameNotSetRuleIdDescriptor,
                        location: _controlsLocations[controlName],
                        controlName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void ParseTabIndexAssignments(AssignmentExpressionSyntax expressionSyntax, OperationBlockAnalysisContext context)
        {
            var propertyNameExpressionSyntax = (MemberAccessExpressionSyntax)expressionSyntax.Left;
            SimpleNameSyntax propertyNameSyntax = propertyNameExpressionSyntax.Name;

            string? controlName = GetControlName(propertyNameExpressionSyntax.Expression);
            if (controlName is null)
            {
                //Debug.Fail("How did we get here?");
                return;
            }

            if (!_controls.ContainsKey(controlName))
            {
                _controls[controlName] = false;
                _controlsLocations[controlName] = expressionSyntax.Left.GetLocation();
            }

            if (propertyNameSyntax.Identifier.ValueText != "AccessibleName")
            {
                return;
            }

            if (expressionSyntax.Right is not LiteralExpressionSyntax propertyValueExpressionSyntax)
            {
                // Presume the value is supplied.
                return;
            }

            string? value = (string?)propertyValueExpressionSyntax.Token.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: AccessibleNameEmptyRuleIdDescriptor,
                    location: expressionSyntax.Right.GetLocation(),
                    controlName,
                    expressionSyntax.Right.ToString());
                context.ReportDiagnostic(diagnostic);
            }

            _controls[controlName] = true;
        }

        private static string? GetControlName(ExpressionSyntax expressionSyntax)
            => expressionSyntax switch
            {
                // TODO: this causes AD0001
                //ThisExpressionSyntax thisExpressionSyntax => thisExpressionSyntax.ToString(),

                // local variable, e.g. "button3.AccessibleName = ''" --> "button3";
                IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.ValueText,

                // field, e.g. "this.button1.AccessibleName = ''" --> "button1";
                MemberAccessExpressionSyntax controlNameExpressionSyntax => controlNameExpressionSyntax.ToString(),

                _ => null,
            };
    }
}
