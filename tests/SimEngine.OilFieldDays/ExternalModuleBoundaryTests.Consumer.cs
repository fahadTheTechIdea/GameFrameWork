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
    internal static partial class ExternalModuleBoundaryTests
    {
        static partial void RegisterConsumerCases() => TestRunner.Add("Oil compiled boundary: actual module signatures IL and client facts require no privileged engine owners", Oil);
        internal static Assembly LoadOilAssembly()
        {
            string configured = Environment.GetEnvironmentVariable("OILFIELDDAYS_GAME_ASSEMBLY");
            if (string.IsNullOrWhiteSpace(configured)) throw new InvalidOperationException("Set OILFIELDDAYS_GAME_ASSEMBLY to the actual OilCo.Game.dll for consumer integration tests.");
            string path = Path.GetFullPath(configured);
            if (!File.Exists(path)) throw new InvalidOperationException("build actual OilCo.PublicProof Release target before auditing its domain assembly");
            return Assembly.LoadFrom(path);
        }
        private static void Oil(Check c)
        {
            Assembly oil = LoadOilAssembly(); c.Equal("OilCo.Game", oil.GetName().Name, "actual domain artifact, no boundary stub");
            c.That(SharedOwners.All(name => typeof(World).Assembly.GetType(name, false) != null),
                "every implementation-owner policy names an actual current engine type");
            Audit(c, oil, true);
            c.That(typeof(Beep.SimEngine.Hosting.SimGame).GetProperty("World", BindingFlags.Public | BindingFlags.Instance) == null,
                "the actual public game facade exposes no mutable World");
            var leaks = new List<string>(); var seen = new HashSet<Type>(); int signatures = 0;
            void Surface(Type type)
            {
                if (type.HasElementType) { Surface(type.GetElementType()); return; }
                if (LiveOwner(type)) { leaks.Add(type.FullName); return; }
                if (type.IsGenericType) foreach (var argument in type.GetGenericArguments()) Surface(argument);
                if (typeof(Delegate).IsAssignableFrom(type)) {
                    var invoke = type.GetMethod("Invoke"); if (invoke != null) { Surface(invoke.ReturnType); foreach (var parameter in invoke.GetParameters()) Surface(parameter.ParameterType); }
                }
                if (type.Assembly != oil || !seen.Add(type)) return;
                foreach (var field in type.GetFields(Declared).Where(field => field.IsPublic)) { signatures++; Surface(field.FieldType); }
                foreach (var method in type.GetMethods(Declared).Where(method => method.IsPublic)) {
                    signatures++; Surface(method.ReturnType); foreach (var parameter in method.GetParameters()) Surface(parameter.ParameterType);
                }
            }
            foreach (var type in oil.GetExportedTypes()) if (type.FullName == "OilCo.Game.OilCoGame"
                || type.Namespace == "OilCo.Game.Views" || type.Namespace == "OilCo.Game.Commands") Surface(type);
            c.That(signatures > 0 && leaks.Count == 0, "actual host-facing fact/command/factory signatures contain no live owner: " + string.Join(",", leaks.Take(8)));
            Console.WriteLine("  Oil client surface: signatures=" + signatures + ", live-owner-leaks=" + leaks.Count);
            foreach (string defect in new[] { "private-field", "reflection", "native", "generic-private-type", "shared-owner" }) {
                var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("boundary.negative." + defect), AssemblyBuilderAccess.Run);
                var type = assembly.DefineDynamicModule("proof").DefineType("InvalidBoundary", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
                if (defect == "native") type.DefinePInvokeMethod("NeverExecuted", "not-loaded-boundary-proof", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                    CallingConventions.Standard, typeof(void), Type.EmptyTypes, System.Runtime.InteropServices.CallingConvention.Cdecl, System.Runtime.InteropServices.CharSet.Ansi);
                else {
                    Type result = defect == "private-field" ? typeof(int) : defect == "generic-private-type"
                        ? typeof(List<>).MakeGenericType(typeof(World).Assembly.GetType("Beep.SimEngine.Standard.AutomationSystem", true))
                        : defect == "shared-owner" ? typeof(Beep.SimEngine.Standard.WorkBoard) : typeof(Type);
                    var method = type.DefineMethod("NeverExecuted", MethodAttributes.Public | MethodAttributes.Static,
                        result, defect == "private-field" ? new[] { typeof(World) } : Type.EmptyTypes);
                    var il = method.GetILGenerator();
                    if (defect == "private-field") { il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, typeof(World).GetField("_entityInitializationDepth", BindingFlags.NonPublic | BindingFlags.Instance)); }
                    else if (defect == "reflection") { il.Emit(OpCodes.Ldstr, "not-loaded-boundary-proof"); il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetType", new[] { typeof(string) })); }
                    else il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ret);
                }
                type.CreateType(); Inspection invalid = Inspect(assembly, true);
                string reason = defect == "private-field" ? "nonpublic engine member" : defect == "reflection" ? "reflective binding"
                    : defect == "native" ? "native entry point" : defect == "generic-private-type" ? "nonpublic engine type" : "shared implementation owner";
                c.That(invalid.Errors.Any(error => error.Contains(reason, StringComparison.Ordinal)), "same actual binary inspector rejects intended synthetic defect: " + defect);
            }
        }
    }
}
