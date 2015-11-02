﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using SiliconStudio.AssemblyProcessor.Serializers;

namespace SiliconStudio.AssemblyProcessor
{
    internal partial class UpdateEngineProcessor
    {
        private void ProcessCoreAssembly(CecilSerializerContext context)
        {
            var assembly = context.Assembly;

            // Check "#if IL" directly in the source to easily see what is generated
            GenerateUpdateEngineHelperCode(assembly);
            GenerateUpdatableFieldCode(assembly);
            new UpdatablePropertyCodeGenerator(assembly).GenerateUpdatablePropertyCode();
            new UpdatableListCodeGenerator(assembly).GenerateUpdatablePropertyCode();
        }

        private static void GenerateUpdateEngineHelperCode(AssemblyDefinition assembly)
        {
            var updateEngineHelperType = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdateEngineHelper");

            // UpdateEngineHelper.ObjectToPtr
            var objectToPtr = RewriteBody(updateEngineHelperType.Methods.First(x => x.Name == "ObjectToPtr"));
            objectToPtr.Emit(OpCodes.Ldarg, objectToPtr.Body.Method.Parameters[0]);
            objectToPtr.Emit(OpCodes.Conv_I);
            objectToPtr.Emit(OpCodes.Ret);

            // UpdateEngineHelper.PtrToObject
            var ptrToObject = RewriteBody(updateEngineHelperType.Methods.First(x => x.Name == "PtrToObject"));
            ptrToObject.Emit(OpCodes.Ldarg, ptrToObject.Body.Method.Parameters[0]);
            ptrToObject.Emit(OpCodes.Ret);
        }

        private static void GenerateUpdatableFieldCode(AssemblyDefinition assembly)
        {
            var updatableFieldType = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdatableField");
            var updatableFieldGenericType = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdatableField`1");

            // UpdatableField.GetObject
            var getObject = RewriteBody(updatableFieldType.Methods.First(x => x.Name == "GetObject"));
            getObject.Emit(OpCodes.Ldarg, getObject.Body.Method.Parameters[0]);
            getObject.Emit(OpCodes.Ldind_Ref);
            getObject.Emit(OpCodes.Ret);

            // UpdatableField.SetObject
            var setObject = RewriteBody(updatableFieldType.Methods.First(x => x.Name == "SetObject"));
            setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[0]);
            setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[1]);
            setObject.Emit(OpCodes.Stind_Ref);
            setObject.Emit(OpCodes.Ret);

            // UpdatableField<T>.SetStruct
            var setStruct = RewriteBody(updatableFieldGenericType.Methods.First(x => x.Name == "SetStruct"));
            setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[0]);
            setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[1]);
            setStruct.Emit(OpCodes.Unbox, updatableFieldGenericType.GenericParameters[0]);
            setStruct.Emit(OpCodes.Cpobj, updatableFieldGenericType.GenericParameters[0]);
            setStruct.Emit(OpCodes.Ret);
        }

        private static ILProcessor RewriteBody(MethodDefinition method)
        {
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }

        /// <summary>
        /// Helper class to generate code for UpdatablePropertyBase (since they usually have lot of similar code).
        /// </summary>
        abstract class UpdatablePropertyBaseCodeGenerator
        {
            protected readonly AssemblyDefinition assembly;
            protected TypeDefinition declaringType;

            public UpdatablePropertyBaseCodeGenerator(AssemblyDefinition assembly)
            {
                this.assembly = assembly;
            }

            public abstract void EmitGetCode(ILProcessor il, TypeReference type);

            public virtual void EmitSetCodeBeforeValue(ILProcessor il, TypeReference type)
            {
            }

            public abstract void EmitSetCodeAfterValue(ILProcessor il, TypeReference type);

            public virtual void GenerateUpdatablePropertyCode()
            {
                // UpdatableProperty.GetStructAndUnbox
                var getStructAndUnbox = RewriteBody(declaringType.Methods.First(x => x.Name == "GetStructAndUnbox"));
                getStructAndUnbox.Emit(OpCodes.Ldarg, getStructAndUnbox.Body.Method.Parameters[1]);
                getStructAndUnbox.Emit(OpCodes.Unbox, declaringType.GenericParameters[0]);
                getStructAndUnbox.Emit(OpCodes.Dup);
                getStructAndUnbox.Emit(OpCodes.Ldarg, getStructAndUnbox.Body.Method.Parameters[0]);
                EmitGetCode(getStructAndUnbox, declaringType.GenericParameters[0]);
                getStructAndUnbox.Emit(OpCodes.Stobj, declaringType.GenericParameters[0]);
                getStructAndUnbox.Emit(OpCodes.Ret);

                // UpdatableProperty.GetBlittable
                var getBlittable = RewriteBody(declaringType.Methods.First(x => x.Name == "GetBlittable"));
                getBlittable.Emit(OpCodes.Ldarg, getBlittable.Body.Method.Parameters[1]);
                getBlittable.Emit(OpCodes.Ldarg, getBlittable.Body.Method.Parameters[0]);
                EmitGetCode(getBlittable, declaringType.GenericParameters[0]);
                getBlittable.Emit(OpCodes.Stobj, declaringType.GenericParameters[0]);
                getBlittable.Emit(OpCodes.Ret);

                // UpdatableProperty.SetStruct
                var setStruct = RewriteBody(declaringType.Methods.First(x => x.Name == "SetStruct"));
                setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[0]);
                EmitSetCodeBeforeValue(setStruct, declaringType.GenericParameters[0]);
                setStruct.Emit(OpCodes.Ldarg, setStruct.Body.Method.Parameters[1]);
                setStruct.Emit(OpCodes.Unbox_Any, declaringType.GenericParameters[0]);
                EmitSetCodeAfterValue(setStruct, declaringType.GenericParameters[0]);
                setStruct.Emit(OpCodes.Ret);

                // UpdatableProperty.SetBlittable
                var setBlittable = RewriteBody(declaringType.Methods.First(x => x.Name == "SetBlittable"));
                setBlittable.Emit(OpCodes.Ldarg, setBlittable.Body.Method.Parameters[0]);
                EmitSetCodeBeforeValue(setBlittable, declaringType.GenericParameters[0]);
                setBlittable.Emit(OpCodes.Ldarg, setBlittable.Body.Method.Parameters[1]);
                setBlittable.Emit(OpCodes.Ldobj, declaringType.GenericParameters[0]);
                EmitSetCodeAfterValue(setBlittable, declaringType.GenericParameters[0]);
                setBlittable.Emit(OpCodes.Ret);
            }
        }

        class UpdatablePropertyCodeGenerator : UpdatablePropertyBaseCodeGenerator
        {
            private readonly FieldDefinition updatablePropertyGetter;
            private readonly FieldDefinition updatablePropertySetter;
            protected TypeDefinition declaringTypeForObjectMethods;

            public UpdatablePropertyCodeGenerator(AssemblyDefinition assembly) : base(assembly)
            {
                // GetObject/SetObject are on the non-generic implementation
                declaringTypeForObjectMethods = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdatableProperty");
                declaringType = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdatableProperty`1");

                updatablePropertyGetter = declaringTypeForObjectMethods.Fields.First(x => x.Name == "Getter");
                updatablePropertySetter = declaringTypeForObjectMethods.Fields.First(x => x.Name == "Setter");
            }

            public override void GenerateUpdatablePropertyCode()
            {
                // For UpdatableProperty, GetObject/SetObject are declared on another type

                // UpdatableProperty.GetObject
                var getObject = RewriteBody(declaringTypeForObjectMethods.Methods.First(x => x.Name == "GetObject"));
                getObject.Emit(OpCodes.Ldarg, getObject.Body.Method.Parameters[0]);
                EmitGetCode(getObject, assembly.MainModule.TypeSystem.Object);
                getObject.Emit(OpCodes.Ret);

                // UpdatableProperty.SetObject
                var setObject = RewriteBody(declaringTypeForObjectMethods.Methods.First(x => x.Name == "SetObject"));
                setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[0]);
                EmitSetCodeBeforeValue(setObject, assembly.MainModule.TypeSystem.Object);
                setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[1]);
                EmitSetCodeAfterValue(setObject, assembly.MainModule.TypeSystem.Object);
                setObject.Emit(OpCodes.Ret);

                base.GenerateUpdatablePropertyCode();
            }

            public override void EmitGetCode(ILProcessor il, TypeReference type)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, updatablePropertyGetter);
                il.Emit(OpCodes.Calli, new CallSite(type) { HasThis = true });
            }

            public override void EmitSetCodeAfterValue(ILProcessor il, TypeReference type)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, updatablePropertySetter);
                il.Emit(OpCodes.Calli, new CallSite(assembly.MainModule.TypeSystem.Void) { HasThis = true, Parameters = { new ParameterDefinition(type) } });
            }
        }

        abstract class UpdatableCustomPropertyCodeGenerator : UpdatablePropertyBaseCodeGenerator
        {
            public UpdatableCustomPropertyCodeGenerator(AssemblyDefinition assembly) : base(assembly)
            {
            }

            public override void GenerateUpdatablePropertyCode()
            {
                // For UpdatableCustomAccessor, GetObject/SetObject are declared in the generic type

                // UpdatableProperty.GetObject
                var getObject = RewriteBody(declaringType.Methods.First(x => x.Name == "GetObject"));
                getObject.Emit(OpCodes.Ldarg, getObject.Body.Method.Parameters[0]);
                EmitGetCode(getObject, declaringType.GenericParameters[0]);
                getObject.Emit(OpCodes.Ret);

                // UpdatableProperty.SetObject
                var setObject = RewriteBody(declaringType.Methods.First(x => x.Name == "SetObject"));
                setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[0]);
                EmitSetCodeBeforeValue(setObject, declaringType.GenericParameters[0]);
                setObject.Emit(OpCodes.Ldarg, setObject.Body.Method.Parameters[1]);
                EmitSetCodeAfterValue(setObject, declaringType.GenericParameters[0]);
                setObject.Emit(OpCodes.Ret);

                base.GenerateUpdatablePropertyCode();
            }
        }

        class UpdatableListCodeGenerator : UpdatableCustomPropertyCodeGenerator
        {
            private readonly FieldDefinition indexField;
            private readonly MethodReference ilistGetItem;
            private readonly MethodReference ilistSetItem;

            public UpdatableListCodeGenerator(AssemblyDefinition assembly) : base(assembly)
            {
                declaringType = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdatableListAccessor`1");
                indexField = assembly.MainModule.GetType("SiliconStudio.Core.Updater.UpdatableListAccessor").Fields.First(x => x.Name == "Index");

                // TODO: Update to new method to resolve collection assembly
                var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
                var ilistType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(IList<>).FullName);

                var ilistItem = ilistType.Properties.First(x => x.Name == "Item");

                ilistGetItem = assembly.MainModule.ImportReference(ilistItem.GetMethod).MakeGeneric(declaringType.GenericParameters[0]);
                ilistSetItem = assembly.MainModule.ImportReference(ilistItem.SetMethod).MakeGeneric(declaringType.GenericParameters[0]);
            }

            public override void EmitGetCode(ILProcessor il, TypeReference type)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, indexField);
                il.Emit(OpCodes.Callvirt, ilistGetItem);
            }

            public override void EmitSetCodeBeforeValue(ILProcessor il, TypeReference type)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, indexField);
            }

            public override void EmitSetCodeAfterValue(ILProcessor il, TypeReference type)
            {
                il.Emit(OpCodes.Callvirt, ilistSetItem);
            }
        }
    }
};