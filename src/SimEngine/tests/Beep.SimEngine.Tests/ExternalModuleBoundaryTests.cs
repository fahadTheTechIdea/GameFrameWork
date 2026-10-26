using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using QuickGame.Engine;

namespace Beep.SimEngine.Tests
{
    // Test-side binary audit only. Portable module code must not perform these inspections.
    internal static partial class ExternalModuleBoundaryTests
    {
        internal const string Case = "Framework independence: compiled external modules use public engine contracts only";
        static partial void RegisterConsumerCases();
        internal static void Register() { TestRunner.Add(Case, Run); RegisterConsumerCases(); }
        private static readonly BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<ushort, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null)).ToDictionary(code => unchecked((ushort)code.Value));
        private static bool BindingApi(MemberInfo member)
        {
            Type type = member.DeclaringType; string name = member.Name;
            if (type == typeof(Type)) return name == "GetType" || name.StartsWith("GetField", StringComparison.Ordinal) || name.StartsWith("GetMethod", StringComparison.Ordinal) ||
                name.StartsWith("GetPropert", StringComparison.Ordinal) || name.StartsWith("GetConstructor", StringComparison.Ordinal) ||
                name.StartsWith("GetMember", StringComparison.Ordinal) || name.StartsWith("GetNestedType", StringComparison.Ordinal) || name == "InvokeMember";
            if (type == typeof(Activator)) return name == "CreateInstance";
            if (type == typeof(Assembly)) return name == "GetType" || name == "CreateInstance" || name.StartsWith("Load", StringComparison.Ordinal);
            if (type?.Namespace == "System.Reflection") return name == "Invoke" || name == "GetValue" || name == "SetValue" || name == "CreateDelegate";
            if (type?.Namespace?.StartsWith("System.Reflection.Emit", StringComparison.Ordinal) == true) return true;
            if (type?.FullName == "System.Runtime.InteropServices.Marshal" && name.StartsWith("GetDelegateForFunctionPointer", StringComparison.Ordinal)) return true;
            return (name == "DynamicInvoke" || name == "CreateDelegate") && type != null && typeof(Delegate).IsAssignableFrom(type);
        }
        private static bool PublicDefinition(Type type) => type != null &&
            (type.IsGenericType ? type.GetGenericTypeDefinition() : type).IsVisible;
        private static bool Visible(MemberInfo member)
        {
            Type type = member as Type ?? member.DeclaringType;
            // An external module may legally close a public SDK generic over its own internal type.
            // Engine-owned arguments are checked independently by TypeReference, not waived here.
            if (!PublicDefinition(type)) return false;
            if (member is MethodBase method) return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
            if (member is FieldInfo field) return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
            return member is Type;
        }
        private static IEnumerable<MethodBase> Methods(Type type)
        {
            var methods = type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared));
            if (type.TypeInitializer != null) methods = methods.Append(type.TypeInitializer);
            return methods.Distinct();
        }
        private sealed class Inspection
        {
            internal readonly List<string> Errors = new List<string>();
            internal readonly HashSet<Type> EngineTypes = new HashSet<Type>();
            internal int Bodies, References, Types;
        }
        private static readonly HashSet<string> SharedOwners = new HashSet<string>(StringComparer.Ordinal) {
            "Beep.SimEngine.Standard.WorkBoard", "Beep.SimEngine.Standard.WorkSystem",
            "Beep.SimEngine.Standard.Storage", "Beep.SimEngine.Standard.Worker", "Beep.SimEngine.Standard.Supplier",
            "Beep.SimEngine.Standard.Transformer", "Beep.SimEngine.Standard.TransformerManager",
            "Beep.SimEngine.Standard.ResourceTransactions", "Beep.SimEngine.Standard.ResourceInventoryState",
            "Beep.SimEngine.Standard.ResourceFlowJournal", "Beep.SimEngine.Standard.NetworkService",
            "Beep.SimEngine.Standard.NetworkService+NetComponent",
            "Beep.SimEngine.Standard.CompanyKnowledgeService", "Beep.SimEngine.Standard.TechnologyService",
            "Beep.SimEngine.Standard.ObjectiveSystem", "Beep.SimEngine.Standard.BusinessAccounts"
        };
        private static Inspection Inspect(Assembly assembly, bool oilOwners = false)
        {
            Assembly engine = typeof(World).Assembly; var report = new Inspection(); var errors = report.Errors;
            var seen = new HashSet<Type>(); int bodies = 0, references = 0;
            void TypeReference(Type value, string origin)
            {
                if (value == null || !seen.Add(value)) return;
                if (value.HasElementType) { TypeReference(value.GetElementType(), origin); return; }
                if (value.IsGenericParameter) { foreach (var constraint in value.GetGenericParameterConstraints()) TypeReference(constraint, origin); return; }
                if (value.IsGenericType) foreach (var argument in value.GetGenericArguments()) TypeReference(argument, origin);
                if (value.Assembly != engine) return;
                report.Types++;
                report.EngineTypes.Add(value.IsGenericType ? value.GetGenericTypeDefinition() : value);
                if (!PublicDefinition(value)) errors.Add(origin + ": nonpublic engine type " + value.FullName);
                for (Type owner = value; oilOwners && owner != null; owner = owner.DeclaringType)
                    if (SharedOwners.Contains(owner.IsGenericType ? owner.GetGenericTypeDefinition().FullName : owner.FullName))
                        errors.Add(origin + ": shared implementation owner " + owner.FullName);
            }
            void Attributes(IEnumerable<CustomAttributeData> attributes, string origin)
            {
                foreach (var attribute in attributes) if (attribute.AttributeType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorAttribute"
                    || attribute.AttributeType.Name == "IgnoresAccessChecksToAttribute") errors.Add(origin + ": privileged access bypass attribute");
            }
            Attributes(assembly.GetCustomAttributesData(), assembly.GetName().Name);
            foreach (var type in assembly.GetTypes()) {
                TypeReference(type.BaseType, type.FullName); foreach (var contract in type.GetInterfaces()) TypeReference(contract, type.FullName);
                foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : Type.EmptyTypes) TypeReference(argument, type.FullName);
                Attributes(type.GetCustomAttributesData(), type.FullName);
                foreach (var field in type.GetFields(Declared)) TypeReference(field.FieldType, type.FullName + "." + field.Name);
                foreach (var method in Methods(type)) {
                string origin = type.FullName + "." + method.Name;
                Attributes(method.GetCustomAttributesData(), origin);
                if (method is MethodInfo signature) TypeReference(signature.ReturnType, origin);
                foreach (var parameter in method.GetParameters()) TypeReference(parameter.ParameterType, origin);
                if (method.IsGenericMethod) foreach (var argument in method.GetGenericArguments()) TypeReference(argument, origin);
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0) errors.Add(origin + ": native entry point");
                var body = method.GetMethodBody(); if (body == null) continue;
                foreach (var local in body.LocalVariables) TypeReference(local.LocalType, origin);
                foreach (var clause in body.ExceptionHandlingClauses)
                    if (clause.Flags == ExceptionHandlingClauseOptions.Clause) TypeReference(clause.CatchType, origin);
                bodies++; byte[] code = body.GetILAsByteArray();
                for (int offset = 0; offset < code.Length;) {
                    ushort key = code[offset++]; if (key == 0xfe) key = (ushort)(0xfe00 | code[offset++]);
                    if (!Codes.TryGetValue(key, out var op)) throw new InvalidOperationException(origin + ": unknown IL opcode");
                    int size;
                    switch (op.OperandType) {
                        case OperandType.InlineNone: size = 0; break;
                        case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                        case OperandType.InlineVar: size = 2; break;
                        case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                        case OperandType.InlineSwitch: size = checked(4 + 4 * BitConverter.ToInt32(code, offset)); break;
                        default: size = 4; break;
                    }
                    if (op.OperandType == OperandType.InlineMethod || op.OperandType == OperandType.InlineField ||
                        op.OperandType == OperandType.InlineType || op.OperandType == OperandType.InlineTok) {
                        int token = BitConverter.ToInt32(code, offset);
                        MemberInfo member = method.Module.ResolveMember(token, type.IsGenericType ? type.GetGenericArguments() : null,
                            method.IsGenericMethod ? method.GetGenericArguments() : null);
                        references++;
                        TypeReference(member as Type ?? member.DeclaringType, origin);
                        if (member is MethodBase called) {
                            if (called is MethodInfo result) TypeReference(result.ReturnType, origin);
                            foreach (var parameter in called.GetParameters()) TypeReference(parameter.ParameterType, origin);
                            if (called.IsGenericMethod) foreach (var argument in called.GetGenericArguments()) TypeReference(argument, origin);
                        }
                        if (member is FieldInfo field) TypeReference(field.FieldType, origin);
                        if (BindingApi(member)) errors.Add(origin + ": reflective binding " + member.DeclaringType?.FullName + "." + member.Name);
                        if ((member as Type ?? member.DeclaringType)?.Assembly == engine && !Visible(member))
                            errors.Add(origin + ": nonpublic engine member " + member);
                    }
                    if (op == OpCodes.Calli) errors.Add(origin + ": indirect native-style invocation");
                    offset = checked(offset + size); if (offset > code.Length) throw new InvalidOperationException(origin + ": truncated IL operand");
                }
            }
            }
            report.Bodies = bodies; report.References = references; return report;
        }
        private static void Audit(Check c, Assembly assembly, bool oilOwners = false)
        {
            Assembly engine = typeof(World).Assembly; string name = assembly.GetName().Name;
            c.That(!ReferenceEquals(engine, assembly), "module is genuinely a separate assembly " + name);
            c.That(!engine.GetCustomAttributes<InternalsVisibleToAttribute>().Any(friend => new AssemblyName(friend.AssemblyName).Name == name),
                "engine grants no internals friendship " + name);
            var dependencies = assembly.GetReferencedAssemblies();
            c.That(dependencies.Any(reference => reference.Name == engine.GetName().Name) && dependencies.All(reference =>
                reference.Name == engine.GetName().Name || reference.Name == "netstandard" || reference.Name == "System.Runtime"),
                "this proof artifact references only engine/runtime: " + name + "/" + string.Join(",", dependencies.Select(reference => reference.Name)));
            Inspection report = Inspect(assembly, oilOwners);
            int bodies = report.Bodies, references = report.References; var errors = report.Errors;
            c.That(bodies > 0 && references > 0, "actual method bodies/member references audited " + name);
            Console.WriteLine("  compiled module audit: " + name + ", bodies=" + bodies + ", member-references=" + references + ", engine-types=" + report.Types + ", violations=" + errors.Count);
            if (oilOwners) Console.WriteLine("  Oil engine class dependencies: " + string.Join(", ", report.EngineTypes.Where(type => type.IsClass).Select(type => type.FullName).OrderBy(value => value, StringComparer.Ordinal)));
            c.That(errors.Count == 0, "portable compiled boundary " + name + ": " + string.Join("; ", errors.Take(8)));
        }
        private static bool LiveOwner(Type type)
        {
            if (type.HasElementType) return LiveOwner(type.GetElementType());
            if (type == typeof(World) || type == typeof(Entity) || type == typeof(EntityStore)
                || typeof(Component).IsAssignableFrom(type) || typeof(IEntityCapability).IsAssignableFrom(type)
                || typeof(GameSystem).IsAssignableFrom(type) || typeof(IEntity).IsAssignableFrom(type)) return true;
            return type.IsGenericType && type.GetGenericArguments().Any(LiveOwner);
        }
        private static void Run(Check c)
        {
            Audit(c, typeof(AuthoredLineCursorModule).Assembly);
            Audit(c, typeof(QuickModule).Assembly);
        }
    }
}
