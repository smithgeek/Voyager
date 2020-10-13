using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Voyager
{
	internal class DynamicTypeBuilder
	{
		private readonly ModuleBuilder moduleBuilder;
		private readonly TypeBindingRepository typeBindingRepo;

		public DynamicTypeBuilder(TypeBindingRepository typeBindingRepo)
		{
			this.typeBindingRepo = typeBindingRepo;
			var assemblyName = new AssemblyName("Voyager.OpenApi.Types");
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			moduleBuilder = assemblyBuilder.DefineDynamicModule("Types");
		}

		public Type CreateBodyType(Type type)
		{
			var typeBuilder = GetRequestBodyTypeBuilder(type);
			typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			var hasProperties = false;
			foreach (var property in typeBindingRepo.GetProperties(type))
			{
				if (property.BindingSource == BindingSource.Body || property.BindingSource == null)
				{
					CreateProperty(typeBuilder, property.Name, property.Property.PropertyType);
					hasProperties = true;
				}
			}
			return hasProperties ? typeBuilder.CreateTypeInfo().AsType() : null;
		}

		private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
		{
			var fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

			var propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
			var getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
			var getIl = getPropMthdBldr.GetILGenerator();

			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, fieldBuilder);
			getIl.Emit(OpCodes.Ret);

			var setPropMthdBldr =
				tb.DefineMethod("set_" + propertyName,
				  MethodAttributes.Public |
				  MethodAttributes.SpecialName |
				  MethodAttributes.HideBySig,
				  null, new[] { propertyType });

			var setIl = setPropMthdBldr.GetILGenerator();
			var modifyProperty = setIl.DefineLabel();
			var exitSet = setIl.DefineLabel();

			setIl.MarkLabel(modifyProperty);
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, fieldBuilder);

			setIl.Emit(OpCodes.Nop);
			setIl.MarkLabel(exitSet);
			setIl.Emit(OpCodes.Ret);

			propertyBuilder.SetGetMethod(getPropMthdBldr);
			propertyBuilder.SetSetMethod(setPropMthdBldr);
		}

		private TypeBuilder GetRequestBodyTypeBuilder(Type type)
		{
			var typeSignature = $"{type.Name}RequestBody";
			var typeBuilder = moduleBuilder.DefineType(typeSignature, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);
			return typeBuilder;
		}
	}
}