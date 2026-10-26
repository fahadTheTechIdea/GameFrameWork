using System;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class PublicContractIdentityTests
    {
        public const string CaseName = "Contract identity: shared public grammar across extension assemblies";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Run(Check check)
        {
            foreach (bool external in new[] { false, true })
            {
                Action<string, int, string> validate = external ? ContractIdentityProof.Validate : ContractIdentity.Validate;
                Action<string, string> identity = external ? ContractIdentityProof.ValidateId : ContractIdentity.ValidateId;
                foreach (string id in new[] { "a", "0", "oil.power", "fuel_reserve", "resource-output" })
                {
                    identity(id, "capability"); validate(id, 1, "capability");
                    check.That(true, "same accepted stable authored identity");
                }
                validate("schema.boundary", int.MaxValue, "capability");
                check.That(true, "positive schema has no arbitrary engine cap");
                foreach (string id in new[] { null, "", "UpperCase", "white space", "slash/path", "unicode.é", "trailing!", "a:b", "a\tb" })
                {
                    check.Throws<ArgumentException>(() => identity(id, "capability"), "malformed identity is rejected by the sole grammar");
                    check.Throws<ArgumentException>(() => validate(id, 1, "capability"), "schema validation uses the same identity grammar");
                }
                foreach (int schema in new[] { 0, -1 })
                    check.Throws<ArgumentOutOfRangeException>(() => validate("valid.schema", schema, "capability"), "nonpositive schema refuses");
            }
        }
    }
}
