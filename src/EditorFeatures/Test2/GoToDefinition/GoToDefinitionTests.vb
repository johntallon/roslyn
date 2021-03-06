' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
Imports Microsoft.CodeAnalysis.Editor.Navigation
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Utilities
Imports Microsoft.CodeAnalysis.GeneratedCodeRecognition
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    Public Class GoToDefinitionTests
        Private Sub Test(workspaceDefinition As XElement, Optional expectedResult As Boolean = True)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition, exportProvider:=ExportProvider)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim items As IList(Of INavigableItem) = Nothing

                ' Set up mocks. The IDocumentNavigationService should be called if there is one,
                ' location and the INavigableItemsPresenter should be called if there are 
                ' multiple locations.

                ' prepare a notification listener
                Dim textView = cursorDocument.GetTextView()
                Dim textBuffer = textView.TextBuffer
                textView.Caret.MoveTo(New SnapshotPoint(textBuffer.CurrentSnapshot, cursorPosition))

                Dim cursorBuffer = cursorDocument.TextBuffer
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim mockDocumentNavigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationService)

                Dim presenter = New MockNavigableItemsPresenter(Sub(i) items = i)
                Dim presenters = {New Lazy(Of INavigableItemsPresenter)(Function() presenter)}

                Dim goToDefService = If(document.Project.Language = LanguageNames.CSharp,
                    DirectCast(New CSharpGoToDefinitionService(presenters), IGoToDefinitionService),
                    New VisualBasicGoToDefinitionService(presenters))

                Dim actualResult = goToDefService.TryGoToDefinition(document, cursorPosition, CancellationToken.None)

                If expectedResult Then
                    If mockDocumentNavigationService._triedNavigationToSpan Then
                        Dim definitionDocument = workspace.GetTestDocument(mockDocumentNavigationService._documentId)
                        Assert.Equal(1, definitionDocument.SelectedSpans.Count)
                        Assert.Equal(definitionDocument.SelectedSpans.Single(), mockDocumentNavigationService._span)

                        ' The INavigableItemsPresenter should not have been called
                        Assert.Null(items)
                    Else
                        Assert.False(mockDocumentNavigationService._triedNavigationToPosition)
                        Assert.False(mockDocumentNavigationService._triedNavigationToLineAndOffset)
                        Assert.NotNull(items)

                        For Each location In items
                            Dim definitionDocument = workspace.GetTestDocument(location.Document.Id)
                            Assert.True(definitionDocument.SelectedSpans.Contains(location.SourceSpan))
                        Next

                        ' The IDocumentNavigationService should not have been called
                        Assert.Null(mockDocumentNavigationService._documentId)
                    End If
                Else
                    Assert.Null(mockDocumentNavigationService._documentId)
                    Assert.True(items Is Nothing OrElse items.Count = 0)
                End If

                Assert.Equal(expectedResult, actualResult)
            End Using
        End Sub

        Friend Shared ReadOnly Catalog As ComposableCatalog = TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                        GetType(MockDocumentNavigationServiceFactory),
                        GetType(DefaultSymbolNavigationServiceFactory),
                        GetType(GeneratedCodeRecognitionServiceFactory))

        Friend Shared ReadOnly ExportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(Catalog)

#Region "P2P Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestP2PClassReference()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <Document>
        using N;

        class CSharpClass
        {
            VBCl$$ass vb
        }
        </Document>
    </Project>
    <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
        <Document>
        namespace N
            public class [|VBClass|]
            End Class
        End Namespace
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

#End Region

#Region "Normal CSharp Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGoToDefinitionSameClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGoToDefinitionNestedClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class Outer
            {
              class [|Inner|]
              {
              }

              In$$ner someObj;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionDifferentFiles()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class OtherClass { SomeClass obj; }
        </Document>
        <Document>
            class OtherClass2 { Some$$Class obj2; };
        </Document>
        <Document>
            class [|SomeClass|] { }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionPartialClasses()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class nothing { };
        </Document>
        <Document>
            partial class [|OtherClass|] { int a; }
        </Document>
        <Document>
            partial class [|OtherClass|] { int b; };
        </Document>
        <Document>
            class ConsumingClass { Other$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionMethod()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|] { int x; };
        </Document>
        <Document>
            class ConsumingClass
            {
                void foo()
                {
                    Some$$Class x;
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(900438)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionPartialMethod()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            partial class Test
            {
                partial void M();
            }
        </Document>
        <Document>
            partial class Test
            {
                void Foo()
                {
                    var t = new Test();
                    t.M$$();
                }

                partial void [|M|]()
                {
                    throw new NotImplementedException();
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnMethodCall1()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void [|M|]() { }
                void M(int i) { }
                void M(int i, string s) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M();
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnMethodCall2()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void [|M|](int i, string s) { }
                void M(int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0, "text");
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnMethodCall3()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void [|M|](int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnMethodCall4()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void M(int i) { }
                void [|M|](string s, int i) { }

                void Call()
                {
                    $$M("text", 0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnConstructor1()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|]
            {
                C() { }

                $$C c = new C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(3376, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnConstructor2()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                [|C|]() { }

                C c = new $$C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionWithoutExplicitConstruct()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|C|]
            {
                void Method()
                {
                    C c = new $$C();
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnLocalVariable1()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void method()
                {
                    int [|x|] = 2, y, z = $$x * 2;
                    y = 10;
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnLocalVariable2()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                void method()
                {
                    int x = 2, [|y|], z = x * 2;
                    $$y = 10;
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnLocalField()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                int [|_X|] = 1, _Y;
                void method()
                {
                    _$$X = 8;
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnAttributeClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            [FlagsAttribute]
            class [|C|]
            {
                $$C c;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionTouchLeft()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|]
            {
                $$SomeClass c;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionTouchRight()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class [|SomeClass|]
            {
                SomeClass$$ c;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionOnGenericTypeParameterInPresenceOfInheritedNestedTypeWithSameName()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class B
            {
                public class T { }
            }
            class C<[|T|]> : B
            {
                $$T x;
            }]]>
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(538765)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGotoDefinitionThroughOddlyNamedType()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            class [|dynamic|] { }
            class C : dy$$namic { }
        ]]></Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGoToDefinitionOnConstructorInitializer1()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private int v;
    public Program() : $$this(4)
    {
    }

    public [|Program|](int v)
    {
        this.v = v;
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGoToDefinitionOnExtensionMethod()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
           class Program
           {
               static void Main(string[] args)
               {
                    "1".$$TestExt();
               }
           }

           public static class Ex
           {
              public static void TestExt<T>(this T ex) { }
              public static void [|TestExt|](this string ex) { }
           }]]>
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542004)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpTestLambdaParameter()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    delegate int D2(int i, int j);
    static void Main()
    {
        D2 d = (int [|i1|], int i2) => { return $$i1 + i2; };
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpTestLabel()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
class C
{
    void M()
    {
    [|Foo|]:
        int Foo;
        goto $$Foo;
    }
}]]>
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpGoToDefinitionFromCref()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document><![CDATA[
            /// <see cref="$$SomeClass"/>
            class [|SomeClass|] 
            { 
            }]]>
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

#End Region

#Region "CSharp Venus Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpVenusGotoDefinition()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            #line 1 "CSForm1.aspx"
            public class [|_Default|]
            {
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545324)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpFilterGotoDefResultsFromHiddenCodeForUIPresenters()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public class [|_Default|]
            {
            #line 1 "CSForm1.aspx"
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545324)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpDoNotFilterGotoDefResultsFromHiddenCodeForApis()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            public class [|_Default|]
            {
            #line 1 "CSForm1.aspx"
               _Defa$$ult a;
            #line default
            #line hidden
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

#End Region

#Region "CSharp Script Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { }
            class OtherClass { Some$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGoToDefinitionSameClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { Some$$Class someObject; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGoToDefinitionNestedClass()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class Outer
            {
              class [|Inner|]
              {
              }

              In$$ner someObj;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionDifferentFiles()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class OtherClass { SomeClass obj; }
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class OtherClass2 { Some$$Class obj2; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionPartialClasses()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            partial class nothing { };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            partial class [|OtherClass|] { int a; }
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            partial class [|OtherClass|] { int b; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class ConsumingClass { Other$$Class obj; }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionMethod()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class [|SomeClass|] { int x; };
        </Document>
        <Document>
            <ParseOptions Kind="Script"/>
            class ConsumingClass
            {
                void foo()
                {
                    Some$$Class x;
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionOnMethodCall1()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void [|M|]() { }
                void M(int i) { }
                void M(int i, string s) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M();
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionOnMethodCall2()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void [|M|](int i, string s) { }
                void M(int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0, "text");
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionOnMethodCall3()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void [|M|](int i) { }
                void M(string s, int i) { }

                void Call()
                {
                    $$M(0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpScriptGotoDefinitionOnMethodCall4()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            <ParseOptions Kind="Script"/>
            class C
            {
                void M() { }
                void M(int i, string s) { }
                void M(int i) { }
                void [|M|](string s, int i) { }

                void Call()
                {
                    $$M("text", 0);
                }
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(989476)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpPreferNongeneratedSourceLocations()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Nongenerated.cs">
partial class [|C|]
{
    void M() 
    { 
        $$C c;
    }
}
        </Document>
        <Document FilePath="Generated.g.i.cs">
partial class C
{
}
        </Document>
    </Project>
</Workspace>
            Test(workspace)
        End Sub

        <WorkItem(989476)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpUseGeneratedSourceLocationsIfNoNongeneratedLocationsAvailable()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Generated.g.i.cs">
class [|C|]
{
}
        </Document>
        <Document FilePath="Nongenerated.g.i.cs">
class D
{
    void M()
    {
        $$C c;
    }
}
        </Document>
    </Project>
</Workspace>
            Test(workspace)
        End Sub

#End Region

#Region "Normal Visual Basic Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
            End Class
            Class OtherClass
                Dim obj As Some$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(541105)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicPropertyBackingField()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Property [|P|] As Integer
    Sub M()
          Me.$$_P = 10
    End Sub
End Class 
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGoToDefinitionSameClass()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim obj As Some$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGoToDefinitionNestedClass()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Outer
                Class [|Inner|]
                End Class
                Dim obj as In$$ner
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGotoDefinitionDifferentFiles()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class OtherClass
                Dim obj As SomeClass 
            End Class
        </Document>
        <Document>
            Class OtherClass2
                Dim obj As Some$$Class
            End Class
        </Document>
        <Document>
            Class [|SomeClass|] 
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGotoDefinitionPartialClasses()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            DummyClass 
            End Class
        </Document>
        <Document>
            Partial Class [|OtherClass|]
                Dim a As Integer 
            End Class
        </Document>
        <Document>
            Partial Class [|OtherClass|]
                Dim b As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Dim obj As Other$$Class 
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGotoDefinitionMethod()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As Some$$Class
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(900438)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGotoDefinitionPartialMethod()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Partial Class Customer
                Private Sub [|OnNameChanged|]()

                End Sub
            End Class
        </Document>
        <Document>
            Partial Class Customer
                Sub New()
                    Dim x As New Customer()
                    x.OnNameChanged$$()
                End Sub
                Partial Private Sub OnNameChanged()

                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicTouchLeft()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As $$SomeClass
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicTouchRight()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As SomeClass$$
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542872)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicMe()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Sub New()
    End Sub
End Class
 
Class [|C|]
    Inherits B
 
    Sub New()
        MyBase.New()
        MyClass.Foo()
        $$Me.Bar()
    End Sub
 
    Private Sub Bar()
    End Sub
 
    Private Sub Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542872)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicMyClass()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Sub New()
    End Sub
End Class
 
Class [|C|]
    Inherits B
 
    Sub New()
        MyBase.New()
        $$MyClass.Foo()
        Me.Bar()
    End Sub
 
    Private Sub Bar()
    End Sub
 
    Private Sub Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542872)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicMyBase()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class [|B|]
    Sub New()
    End Sub
End Class
 
Class C
    Inherits B
 
    Sub New()
        $$MyBase.New()
        MyClass.Foo()
        Me.Bar()
    End Sub
 
    Private Sub Bar()
    End Sub
 
    Private Sub Foo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

#End Region

#Region "Venus Visual Basic Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicVenusGotoDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            #ExternalSource ("Default.aspx", 1)
            Class [|Program|]
                Sub Main(args As String())
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545324)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicFilterGotoDefResultsFromHiddenCodeForUIPresenters()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|Program|]
                Sub Main(args As String())
            #ExternalSource ("Default.aspx", 1)
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545324)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicDoNotFilterGotoDefResultsFromHiddenCodeForApis()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|Program|]
                Sub Main(args As String())
            #ExternalSource ("Default.aspx", 1)
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
#End Region

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicTestThroughExecuteCommand()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer 
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub foo()
                    Dim obj As SomeClass$$
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGoToDefinitionOnExtensionMethod()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim i As String = "1"
        i.Test$$Ext()
    End Sub
End Class

Module Ex
    <System.Runtime.CompilerServices.Extension()>
    Public Sub TestExt(Of T)(ex As T)
    End Sub
    <System.Runtime.CompilerServices.Extension()>
    Public Sub [|TestExt|](ex As string)
    End Sub
End Module]]>]
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542220)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpTestAliasAndTarget1()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using [|AliasedSomething|] = X.Something;
 
namespace X
{
    class Something { public Something() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        $$AliasedSomething x = new AliasedSomething();
        X.Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542220)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpTestAliasAndTarget2()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using [|AliasedSomething|] = X.Something;
 
namespace X
{
    class Something { public Something() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new $$AliasedSomething();
        X.Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542220)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpTestAliasAndTarget3()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;
 
namespace X
{
    class [|Something|] { public Something() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new AliasedSomething();
        X.$$Something y = new X.Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542220)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub CSharpTestAliasAndTarget4()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
using AliasedSomething = X.Something;
 
namespace X
{
    class Something { public [|Something|]() { } }
}
 
class Program
{
    static void Main(string[] args)
    {
        AliasedSomething x = new AliasedSomething();
        X.Something y = new X.$$Something();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(543218)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicQueryRangeVariable()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim arr = New Integer() {4, 5}
        Dim q3 = From [|num|] In arr Select $$num
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(529060)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub VisualBasicGotoConstant()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main()
lable1: GoTo $$200
[|200|]:    GoTo lable1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545661)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCrossLanguageParameterizedPropertyOverride()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
        <Document>
Public Class A
    Public Overridable ReadOnly Property X(y As Integer) As Integer
        [|Get|]
        End Get
    End Property
End Class
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBProj</ProjectReference>
        <Document>
class B : A
{
    public override int get_X(int y)
    {
        return base.$$get_X(y);
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(866094)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCrossLanguageNavigationToVBModuleMember()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
        <Document>
Public Module A
    Public Sub [|M|]()
    End Sub
End Module
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBProj</ProjectReference>
        <Document>
class C
{
    static void N()
    {
        A.$$M();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

#Region "Show notification tests"

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub ShowNotificationVB()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class SomeClass
            End Class
            Cl$$ass OtherClass
                Dim obj As SomeClass
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace, expectedResult:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub ShowNotificationCS()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class SomeClass { }
            cl$$ass OtherClass
            {
                SomeClass obj;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace, expectedResult:=False)
        End Sub

        <WorkItem(546341)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub GoToDefinitionOnGlobalKeyword()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
            class C
            {
                gl$$obal::System.String s;
            }
        </Document>
    </Project>
</Workspace>

            Test(workspace, expectedResult:=False)
        End Sub

        <WorkItem(902119)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub GoToDefinitionOnInferredFieldInitializer()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class Class2
    Sub Test()
        Dim var1 = New With {Key .var2 = "Bob", Class2.va$$r3}
    End Sub
 
    Shared Property [|var3|]() As Integer
        Get
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class

        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(885151)>
        <Fact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub GoToDefinitionGlobalImportAlias()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <CompilationOptions>
            <GlobalImport>Foo = Importable.ImportMe</GlobalImport>
        </CompilationOptions>
        <Document>
Public Class Class2
    Sub Test()
        Dim x as Fo$$o
    End Sub
End Class

        </Document>
    </Project>

    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly">
        <Document>
Namespace Importable
    Public Class [|ImportMe|]
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
#End Region

    End Class
End Namespace
