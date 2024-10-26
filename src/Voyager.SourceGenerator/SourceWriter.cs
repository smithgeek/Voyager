using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Voyager.SourceGenerator;

internal class SourceWriter
{
}

public class SourceBuilder()
{
	public List<string> Directives { get; } = [];
	public List<string> Usings { get; } = [];
	public List<NamespaceBuilder> Namespaces { get; } = [];

	public SourceBuilder AddDirective(string directive)
	{
		Directives.Add(directive);
		return this;
	}

	public NamespaceBuilder AddNamespace(string @namespace)
	{
		var builder = new NamespaceBuilder(@namespace);
		Namespaces.Add(builder);
		return builder;
	}

	public SourceBuilder AddUsing(string @using)
	{
		Usings.Add(@using);
		return this;
	}

	public string Build()
	{
		var code = new IndentedTextWriter(new StringWriter());
		foreach (var directive in Directives)
		{
			code.WriteLine(directive);
		}
		foreach (var @using in Usings)
		{
			code.WriteLine($"using {@using};");
		}
		foreach (var @namespace in Namespaces)
		{
			code.WriteLine();
			@namespace.Build(code, Namespaces.Count > 1);
		}
		return code.InnerWriter.ToString();
	}
}

public class NamespaceBuilder(string name)
{
	public List<ClassBuilder> Classes { get; } = [];
	public string Name { get; } = name;

	public ClassBuilder AddClass(ClassBuilder classBuilder)
	{
		Classes.Add(classBuilder);
		return classBuilder;
	}

	public void Build(IndentedTextWriter code, bool indentContent)
	{
		if (indentContent)
		{
			code.WriteLine($"namespace {Name}");
			code.WriteLine("{");
			code.Indent++;
		}
		else
		{
			code.WriteLine($"namespace {Name};");
			code.WriteLine();
		}

		foreach (var @class in Classes)
		{
			@class.Build(code);
		}

		if (indentContent)
		{
			code.Indent--;
			code.WriteLine("}");
		}
	}
}

public enum Access
{
	Public,
	Private,
	Internal
}

public class ClassBuilder(string name, Access access = Access.Internal, bool isStatic = false) : ICodeBuilder
{
	public string Name { get; } = name;
	public Access Access { get; } = access;
	public List<string> BaseList { get; } = [];
	public List<MethodBuilder> Methods { get; } = [];
	public List<PropertyBuilder> Properties { get; } = [];
	public bool IsStatic { get; } = isStatic;
	public List<ClassBuilder> Classes { get; } = [];
	private readonly List<RegionBuilder> regions = [];
	private readonly List<string> startDirectives = [];
	private readonly List<string> endDirectives = [];

	public ClassBuilder AddBase(string @interface)
	{
		BaseList.Add(@interface);
		return this;
	}

	public RegionBuilder AddRegion()
	{
		var region = new RegionBuilder();
		regions.Add(region);
		return region;
	}

	public ClassBuilder AddClass(ClassBuilder classBuilder)
	{
		Classes.Add(classBuilder);
		return classBuilder;
	}

	public ClassBuilder AddDirective(string startDirective, string? endDirective = null)
	{
		startDirectives.Add(startDirective);
		if (endDirective != null)
		{
			endDirectives.Add(endDirective);
		}
		return this;
	}

	public void Build(IndentedTextWriter code)
	{
		foreach (var startDirective in startDirectives)
		{
			code.WriteLine(startDirective);
		}
		code.WriteLine($"{Access.ToCode()} {(IsStatic ? "static " : "")}class {Name}{GetInterfaces()}");
		code.WriteLine("{");
		code.Indent++;
		foreach (var property in Properties)
		{
			property.Build(code);
		}
		foreach (var method in Methods)
		{
			method.Build(code);
		}
		foreach (var @class in Classes)
		{
			@class.Build(code);
		}
		foreach (var region in regions)
		{
			region.Build(code);
		}
		code.Indent--;
		code.WriteLine("}");
		foreach (var endDirective in endDirectives)
		{
			code.WriteLine(endDirective);
		}
	}

	public MethodBuilder AddMethod(MethodBuilder builder)
	{
		Methods.Add(builder);
		return builder;
	}

	public PropertyBuilder AddProperty(PropertyBuilder builder)
	{
		Properties.Add(builder);
		return builder;
	}

	private string GetInterfaces()
	{
		if (BaseList.Any())
		{
			return $" : {string.Join(", ", BaseList)}";
		}
		return "";
	}
}

public class PropertyBuilder(string type, string name) : ICodeBuilder
{
	public List<string> Attributes { get; } = [];
	public Access Access { get; } = Access.Public;
	public string Type { get; } = type;
	public string Name { get; } = name;

	public void Build(IndentedTextWriter code)
	{
		foreach (var attribute in Attributes)
		{
			code.WriteLine($"[{attribute}]");
		}
		code.WriteLine($"{Access.ToCode()} {Type} {Name} {{ get; set; }} ");
	}
}

public class MethodBuilder(string name, string returnType = "void", Access access = Access.Private, bool isStatic = false)
	: CodeBuilder
{
	public string Name { get; } = name;
	public string ReturnType { get; } = returnType;
	public Access Access { get; } = access;
	public bool IsStatic { get; } = isStatic;
	public List<string> Parameters { get; } = [];

	public MethodBuilder AddParameter(string parameter)
	{
		Parameters.Add(parameter);
		return this;
	}

	public override void Build(IndentedTextWriter code)
	{
		code.WriteLine($"{Access.ToCode()} {(IsStatic ? "static " : "")}{ReturnType} {Name}({string.Join(", ", Parameters)})");
		code.WriteLine("{");
		code.Indent++;
		foreach (var child in Children)
		{
			child.Build(code);
		}
		code.Indent--;
		code.WriteLine("}");
	}
}

public interface ICodeBuilder
{
	public void Build(IndentedTextWriter code);
}

public abstract class CodeBuilder : ICodeBuilder
{
	public List<ICodeBuilder> Children { get; } = [];
	private readonly StringBuilder partialStatement = new();

	public abstract void Build(IndentedTextWriter code);

	public CodeBuilder AddStatement(string statement)
	{
		if (partialStatement.Length > 0)
		{
			partialStatement.AppendLine(statement);
			Children.Add(new StatementBuilder(partialStatement.ToString()));
		}
		else
		{
			Children.Add(new StatementBuilder(statement));
		}
		partialStatement.Clear();
		return this;
	}

	public CodeBuilder AddPartialStatement(string statement)
	{
		partialStatement.Append(statement);
		return this;
	}

	public CodeBuilder AddScope(string? initial = null, string? suffix = null)
	{
		var scope = new ScopeBuilder(initial, suffix);
		Children.Add(scope);
		return scope;
	}

	public CodeBuilder AddRegion()
	{
		var region = new RegionBuilder();
		Children.Add(region);
		return region;
	}

	public CodeBuilder AddIf(string conditional)
	{
		var scope = new ScopeBuilder($"if({conditional})");
		Children.Add(scope);
		return scope;
	}
}

public class StatementBuilder(string statement) : ICodeBuilder
{
	private readonly string statement = statement;

	public void Build(IndentedTextWriter code)
	{
		code.WriteLine(statement);
	}
}

public class ScopeBuilder(string? initial = null, string? suffix = null) : CodeBuilder, ICodeBuilder
{
	private readonly string? initial = initial;

	public override void Build(IndentedTextWriter code)
	{
		if (initial != null)
		{
			code.WriteLine(initial);
		}
		code.WriteLine("{");
		code.Indent++;
		foreach (var child in Children)
		{
			child.Build(code);
		}
		code.Indent--;
		code.WriteLine($"}}{suffix ?? string.Empty}");
	}
}

public class RegionBuilder() : CodeBuilder, ICodeBuilder
{
	public List<ClassBuilder> Classes { get; } = [];
	private readonly List<string> startDirectives = [];
	private readonly List<string> endDirectives = [];

	public RegionBuilder AddDirective(string startDirective, string? endDirective = null)
	{
		startDirectives.Add(startDirective);
		if (endDirective != null)
		{
			endDirectives.Add(endDirective);
		}
		return this;
	}

	public ClassBuilder AddClass(ClassBuilder classBuilder)
	{
		Classes.Add(classBuilder);
		Children.Add(classBuilder);
		return classBuilder;
	}

	public override void Build(IndentedTextWriter code)
	{
		foreach (var directive in startDirectives)
		{
			code.WriteLine(directive);
		}
		foreach (var child in Children)
		{
			child.Build(code);
		}
		foreach (var directive in endDirectives)
		{
			code.WriteLine(directive);
		}
	}
}

public static class Extensions
{
	public static string ToCode(this Access access)
	{
		return access switch
		{
			Access.Internal => "internal",
			Access.Public => "public",
			Access.Private => "private",
			_ => throw new NotImplementedException()
		};
	}
}