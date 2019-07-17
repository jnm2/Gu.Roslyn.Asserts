namespace Gu.Roslyn.Asserts.Tests
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;

    [TestFixture]
    public partial class RoslynAssertTests
    {
        public static class DiagnosticsSuccess
        {
            [OneTimeSetUp]
            public static void OneTimeSetUp()
            {
                RoslynAssert.MetadataReferences.Add(MetadataReference.CreateFromFile(typeof(int).Assembly.Location));
            }

            [OneTimeTearDown]
            public static void OneTimeTearDown()
            {
                RoslynAssert.ResetAll();
            }

            [Test]
            public static void OneErrorIndicatedPosition()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value;
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                var expectedDiagnostic = ExpectedDiagnostic.Create(FieldNameMustNotBeginWithUnderscore.DiagnosticId);
                RoslynAssert.Diagnostics(analyzer, code);
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
            }

            [Test]
            public static void TwoErrorsSamePosition()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value;
    }
}";
                var expectedDiagnostic1 = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                    FieldNameMustNotBeginWithUnderscoreReportsTwo.DiagnosticId1,
                    code,
                    out _);

                var expectedDiagnostic2 = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated(
                    FieldNameMustNotBeginWithUnderscoreReportsTwo.DiagnosticId2,
                    code,
                    out code);
                var analyzer = new FieldNameMustNotBeginWithUnderscoreReportsTwo();
                RoslynAssert.Diagnostics(analyzer, new[] { expectedDiagnostic1, expectedDiagnostic2 }, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticIdOnly()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int _value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.Create("SA1309");
                RoslynAssert.Diagnostics(new FieldNameMustNotBeginWithUnderscore(), expectedDiagnostic, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticIdAndMessage()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int _value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.Create("SA1309", "Field '_value1' must not begin with an underscore");
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticIdAndPosition()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int _value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.Create("SA1309", 5, 29);
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnostics()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("SA1309", code, out code);
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
                RoslynAssert.Diagnostics(analyzer, new[] { expectedDiagnostic }, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticPositionFromIndicatedCode()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("SA1309", code, out code);
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
                RoslynAssert.Diagnostics(analyzer, new[] { expectedDiagnostic }, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticWithMessageAndPosition()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("SA1309", "Field '_value1' must not begin with an underscore", code, out code);

                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, new[] { code });
                RoslynAssert.Diagnostics(analyzer, new[] { expectedDiagnostic }, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticWithMessageAndErrorIndicatedInCode()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.Create(FieldNameMustNotBeginWithUnderscore.DiagnosticId, "Field '_value1' must not begin with an underscore");

                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticsWithMessageAndErrorIndicatedInCode()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("SA1309", "Field '_value1' must not begin with an underscore", code, out code);
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, new[] { expectedDiagnostic }, code);
            }

            [Test]
            public static void OneErrorWithExpectedDiagnosticWithMessageWhenAnalyzerSupportsTwoDiagnostics()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.CreateFromCodeWithErrorsIndicated("SA1309b", "Field '_value1' must not begin with an underscore", code, out code);
                var analyzer = new FieldNameMustNotBeginWithUnderscoreDifferentDiagnosticsForPublic();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
                RoslynAssert.Diagnostics(analyzer, new[] { expectedDiagnostic }, code);
            }

            [Test]
            public static void SingleDocumentOneErrorPassingAnalyzer()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value = 1;
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, code);
            }

            [Test]
            public static void WhenCompilationError()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value = 1;
        INCOMPLETE
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, code, AllowCompilationErrors.Yes);
            }

            [Test]
            public static void SingleDocumentTwoErrorsIndicated()
            {
                var code = @"
namespace N
{
    class C
    {
        private readonly int ↓_value1;
        private readonly int ↓_value2;
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, code);
            }

            [Test]
            public static void TwoDocumentsOneError()
            {
                var c1 = @"
namespace N
{
    class C1
    {
        private readonly int ↓_value = 1;
    }
}";
                var c2 = @"
namespace N
{
    class C2
    {
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, c1, c2);
            }

            [Test]
            public static void TwoDocumentsTwoErrors()
            {
                var c1 = @"
namespace N
{
    class C1
    {
        private readonly int ↓_value = 1;
    }
}";
                var c2 = @"
namespace N
{
    class C2
    {
        private readonly int ↓_value = 1;
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscore();
                RoslynAssert.Diagnostics(analyzer, c1, c2);
            }

            [Test]
            public static void TwoDocumentsTwoErrorsDefaultDisabledAnalyzer()
            {
                var c1 = @"
namespace N
{
    class C1
    {
        private readonly int ↓_value = 1;
    }
}";
                var c2 = @"
namespace N
{
    class C2
    {
        private readonly int ↓_value = 1;
    }
}";
                var analyzer = new FieldNameMustNotBeginWithUnderscoreDisabled();
                RoslynAssert.Diagnostics(analyzer, c1, c2);
            }

            [Test]
            public static void WithExpectedDiagnosticWhenOneReportsError()
            {
                var code = @"
namespace N
{
    class Value
    {
        private readonly int ↓wrongName;
        
        public int WrongName { get; set; }
    }
}";

                var expectedDiagnostic = ExpectedDiagnostic.Create(FieldAndPropertyMustBeNamedValueAnalyzer.FieldDiagnosticId);
                var analyzer = new FieldAndPropertyMustBeNamedValueAnalyzer();
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);

                code = @"
namespace N
{
    class Value
    {
        private readonly int wrongName;
        
        public int ↓WrongName { get; set; }
    }
}";

                expectedDiagnostic = ExpectedDiagnostic.Create(FieldAndPropertyMustBeNamedValueAnalyzer.PropertyDiagnosticId);
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
            }

            [Explicit("Temp suppress.")]
            [Test]
            public static void AdditionalLocation()
            {
                var code = @"
namespace N
{
    ↓class Value
    {
        ↓private readonly int f;
    }
}";

                var analyzer = new FieldWithAdditionalLocationClassAnalyzer();
                var expectedDiagnostic = ExpectedDiagnostic.Create(analyzer.SupportedDiagnostics.Single());

                RoslynAssert.Diagnostics(analyzer, code);
                RoslynAssert.Diagnostics(analyzer, expectedDiagnostic, code);
            }
        }
    }
}
