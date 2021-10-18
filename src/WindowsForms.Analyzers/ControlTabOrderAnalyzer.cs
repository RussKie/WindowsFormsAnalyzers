// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public sealed class ControlTabOrderAnalyzer : DiagnosticAnalyzer
    {
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization

        public static readonly DiagnosticDescriptor NonNumericTabIndexValueRuleIdDescriptor
            = new(DiagnosticIds.NonNumericTabIndexValueRuleId,
                  "Ensure numeric controls tab order value",
                  "Control '{0}' has unexpected TabIndex value: '{1}'",
                  DiagnosticCategory.AccessibilityCategory,
                  DiagnosticSeverity.Warning,
                  isEnabledByDefault: true,
                  "Avoid manually editing \"InitializeComponent()\" method.");

        public static readonly DiagnosticDescriptor InconsistentTabIndexRuleIdDescriptor
            = new(DiagnosticIds.InconsistentTabIndexRuleId,
                  "Verify correct controls tab order",
                  "Control '{0}' has ordinal index of {1} but sets a different TabIndex of {2}",
                  DiagnosticCategory.AccessibilityCategory,
                  DiagnosticSeverity.Warning,
                  isEnabledByDefault: true,
                  "Remove TabIndex assignments and re-order controls in the parent's control collection.");


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(InconsistentTabIndexRuleIdDescriptor, NonNumericTabIndexValueRuleIdDescriptor);

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

                CalculatedAnalysisContext calculatedContext = new();
                foreach (IOperation operation in blockOperation.Operations)
                {
                    switch (operation.Kind)
                    {
                        case OperationKind.ExpressionStatement:
                            {
                                var expressionStatementOperation = (IExpressionStatementOperation)operation;

                                // Look for ".Controls.Add"
                                if (expressionStatementOperation.Operation is IOperation invocationOperation &&
                                    invocationOperation.Syntax is InvocationExpressionSyntax expressionSyntax)
                                {
                                    ParseControlAddStatements(expressionSyntax, calculatedContext);
                                    continue;
                                }

                                // Look for ".TabIndex = <x>"
                                if (expressionStatementOperation.Operation is IAssignmentOperation assignmentOperation)
                                {
                                    ParseTabIndexAssignments((AssignmentExpressionSyntax)assignmentOperation.Syntax, context, calculatedContext);
                                    continue;
                                }

                            }
                            break;

                        default:
                            break;
                    }
                }

                if (calculatedContext.ControlsTabIndex.Count < 1)
                {
                    // No controls explicitly set TabIndex - all good
                    return;
                }

                Dictionary<string, List<Location>> containerProperties = BuildContainerAddLocations(calculatedContext.ContainerAddLocations);

                // _controlsAddIndex dictionary, which looks something like this:
                //
                //      [this.Controls.Add]   : new List { button3, this.button1 }
                //      [panel1.Controls.Add] : new List { label2 }
                //
                // Flatten to look like:
                //
                //      [button3:0]
                //      [this.button1:1]
                //      [label2:0]
                Dictionary<string, int> flatControlsAddIndex = new();
                Dictionary<string, string> containersByControl = new();
                foreach (string containerName in calculatedContext.ControlsAddIndex.Keys)
                {
                    for (int i = 0; i < calculatedContext.ControlsAddIndex[containerName].Count; i++)
                    {
                        string controlName = calculatedContext.ControlsAddIndex[containerName][i];
                        flatControlsAddIndex[controlName] = i;

                        containersByControl[controlName] = containerName;
                    }
                }

                // Verify explicit TabIndex is the same as the "add order"
                foreach (string controlName in calculatedContext.ControlsTabIndex.Keys)
                {
                    if (!flatControlsAddIndex.ContainsKey(controlName))
                    {
                        // TODO: assert, diagnostics, etc.
                        continue;
                    }

                    int tabIndex = calculatedContext.ControlsTabIndex[controlName];
                    int addIndex = flatControlsAddIndex[controlName];

                    if (tabIndex == addIndex)
                    {
                        continue;
                    }

                    string containerName = containersByControl[controlName];
                    Dictionary<string, string?> properties = new();
                    properties["ZOrder"] = addIndex.ToString();
                    properties["TabIndex"] = tabIndex.ToString();

                    var diagnostic = Diagnostic.Create(
                        descriptor: InconsistentTabIndexRuleIdDescriptor,
                        location: calculatedContext.ControlsAddIndexLocations[controlName],
                        additionalLocations: containerProperties[containerName],
                        properties.ToImmutableDictionary(),
                        controlName, addIndex, tabIndex);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static Dictionary<string, List<Location>> BuildContainerAddLocations(in Dictionary<string, List<Location>> containerAddLocations)
        {
            Dictionary<string, List<Location>> containerProperties = new();

            // Check that 'container.Controls.Add(...)' statements are consequitive.
            // If not - the code has been manually modified, we won't be able to provide an auto-fix.
            foreach (string containerName in containerAddLocations.Keys)
            {
                containerProperties[containerName] = new();

                Location startLine = Location.None;
                Location endLine = Location.None;
                List<int> lines = new();
                foreach (Location location in containerAddLocations[containerName])
                {
                    if (startLine == Location.None || startLine.GetLineSpan().StartLinePosition.Line > location.GetLineSpan().StartLinePosition.Line)
                    {
                        startLine = location;
                    }

                    if (endLine.GetLineSpan().StartLinePosition.Line < location.GetLineSpan().StartLinePosition.Line)
                    {
                        endLine = location;
                    }

                    lines.Add(location.GetLineSpan().StartLinePosition.Line);
                }

                Debug.Assert(startLine != Location.None);
                Debug.Assert(endLine != Location.None);

                if (startLine == endLine)
                {
                    // A single control with an invalid TabIndex
                }
                else if (Enumerable.Range(startLine.GetLineSpan().StartLinePosition.Line, endLine.GetLineSpan().StartLinePosition.Line - startLine.GetLineSpan().StartLinePosition.Line).Except(lines).Any())
                {
                    // 'container.Controls.Add(...)' statements aren't consequitive.
                }
                else
                {
                    containerProperties[containerName] = containerAddLocations[containerName];
                }
            }

            return containerProperties;
        }

        private void ParseControlAddStatements(InvocationExpressionSyntax expressionSyntax, CalculatedAnalysisContext calculatedContext)
        {
            if (!expressionSyntax.Expression.ToString().EndsWith(".Controls.Add"))
            {
                return;
            }

            // this.Controls.Add(this.button2) --> this.button2
            ArgumentSyntax? argumentSyntax = expressionSyntax.ArgumentList.Arguments.FirstOrDefault();
            if (argumentSyntax is null)
            {
                return;
            }

            string? controlName = GetControlName(argumentSyntax.Expression);
            if (controlName is null)
            {
                return;
            }

            // this is something like "this.Controls.Add" or "panel1.Controls.Add", but good enough for our intents and purposes
            ExpressionSyntax? syntax = expressionSyntax.Expression;
            string containerName = syntax.ToString();

            // Transform "Controls.Add" statements into a map. E.g.:
            //
            //      this.Controls.Add(button3);
            //      panel1.Controls.Add(label2);
            //      this.Controls.Add(this.button1);
            //
            // ...will become:
            //
            //      [this.Controls.Add]   : new List { button3, this.button1 }
            //      [panel1.Controls.Add] : new List { label2 }

            if (!calculatedContext.ControlsAddIndex.ContainsKey(containerName))
            {
                calculatedContext.ControlsAddIndex[containerName] = new List<string>();
            }

            calculatedContext.ControlsAddIndex[containerName].Add(controlName);
            calculatedContext.ControlsAddIndexLocations[controlName] = syntax.Parent!.Parent!.GetLocation(); // e.g.: 'this.Controls.Add(button3);'

            if (!calculatedContext.ContainerAddLocations.ContainsKey(containerName))
            {
                calculatedContext.ContainerAddLocations[containerName] = new();
            }

            calculatedContext.ContainerAddLocations[containerName].Add(calculatedContext.ControlsAddIndexLocations[controlName]);
        }

        private void ParseTabIndexAssignments(AssignmentExpressionSyntax expressionSyntax, OperationBlockAnalysisContext context, CalculatedAnalysisContext calculatedContext)
        {
            var propertyNameExpressionSyntax = (MemberAccessExpressionSyntax)expressionSyntax.Left;
            SimpleNameSyntax propertyNameSyntax = propertyNameExpressionSyntax.Name;

            if (propertyNameSyntax.Identifier.ValueText != "TabIndex")
            {
                return;
            }

            string? controlName = GetControlName(propertyNameExpressionSyntax.Expression);
            if (controlName is null)
            {
                Debug.Fail("How did we get here?");
                return;
            }

            if (expressionSyntax.Right is not LiteralExpressionSyntax propertyValueExpressionSyntax)
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: NonNumericTabIndexValueRuleIdDescriptor,
                    location: expressionSyntax.Right.GetLocation(),
                    controlName,
                    expressionSyntax.Right.ToString());
                context.ReportDiagnostic(diagnostic);
                return;
            }

#pragma warning disable CS8605 // Unboxing a possibly null value.
            int tabIndexValue = (int)propertyValueExpressionSyntax.Token.Value;
#pragma warning restore CS8605 // Unboxing a possibly null value.

            // "button3:0"
            calculatedContext.ControlsTabIndex[controlName] = tabIndexValue;
        }

        private static string? GetControlName(ExpressionSyntax expressionSyntax)
            => expressionSyntax switch
            {
                // local variable, e.g. "button3.TabIndex = 0" --> "button3";
                IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.ValueText,

                // field, e.g. "this.button1.TabIndex = 1" --> "button1";
                MemberAccessExpressionSyntax controlNameExpressionSyntax => controlNameExpressionSyntax.ToString(),

                _ => null,
            };

        private sealed class CalculatedAnalysisContext
        {
            // Contains the list of fields and local controls that explicitly set TabIndex properties.
            public Dictionary<string, int> ControlsTabIndex { get; } = new();
            // Contains the list of fields and local controls in order those are added to parent controls.
            public Dictionary<string, List<string>> ControlsAddIndex { get; } = new();
            public Dictionary<string, Location> ControlsAddIndexLocations { get; } = new();

            public Dictionary<string, List<Location>> ContainerAddLocations { get; } = new();
        }
    }
}
