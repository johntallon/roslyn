﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.18326
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System

Namespace TestResources.SymbolsTests
    
    'This class was auto-generated by the StronglyTypedResourceBuilder
    'class via a tool like ResGen or Visual Studio.
    'To add or remove a member, edit your .ResX file then rerun ResGen
    'with the /str option, or rebuild your VS project.
    '''<summary>
    '''  A strongly-typed resource class, for looking up localized strings, etc.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
    Public Class MissingTypes
        
        Private Shared resourceMan As Global.System.Resources.ResourceManager
        
        Private Shared resourceCulture As Global.System.Globalization.CultureInfo
        
        <Global.System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")>  _
        Friend Sub New()
            MyBase.New
        End Sub
        
        '''<summary>
        '''  Returns the cached ResourceManager instance used by this class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Public Shared ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("MissingTypes", GetType(MissingTypes).Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property
        
        '''<summary>
        '''  Overrides the current thread's CurrentUICulture property for all
        '''  resource lookups using this strongly typed resource class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Public Shared Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property CL2() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("CL2", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property CL3() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("CL3", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to &apos;vbc /vbruntime- /t:library CL3.vb /r:CL2.dll /out:CL3.dll
        '''
        '''Public Class CL3_C1
        '''    Inherits CL2_C1
        '''
        '''
        '''    Public Shared Function Test1() As Object
        '''        Return Nothing
        '''    End Function
        '''
        '''    Public Shared Function Test2() As CL2_C1
        '''        Return Nothing
        '''    End Function
        '''
        '''    Public Function Test3() As CL2_C1
        '''        Return Nothing
        '''    End Function
        '''End Class
        '''
        '''Public Class CL3_C2
        '''    Public Shared Function Test1() As CL2_C1
        '''        Return Nothing
        '''    End Function
        '''
        '''    Public x As CL2 [rest of string was truncated]&quot;;.
        '''</summary>
        Public Shared ReadOnly Property CL3_VB() As String
            Get
                Return ResourceManager.GetString("CL3_VB", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property MDMissingType() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MDMissingType", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property MDMissingTypeLib() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MDMissingTypeLib", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property MDMissingTypeLib_New() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MDMissingTypeLib_New", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property MissingTypesEquality1() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MissingTypesEquality1", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized resource of type System.Byte[].
        '''</summary>
        Public Shared ReadOnly Property MissingTypesEquality2() As Byte()
            Get
                Dim obj As Object = ResourceManager.GetObject("MissingTypesEquality2", resourceCulture)
                Return CType(obj,Byte())
            End Get
        End Property
    End Class
End Namespace
