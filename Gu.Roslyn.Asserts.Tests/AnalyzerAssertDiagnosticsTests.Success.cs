﻿namespace Gu.Roslyn.Asserts.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public partial class AnalyzerAssertDiagnosticsTests
    {
        public class Success
        {
            [Test]
            public void SingleClassOneErrorGeneric()
            {
                var code = @"
namespace RoslynSandbox
{
    class Foo
    {
        private readonly int ↓_value;
    }
}";
                AnalyzerAssert.Diagnostics<FieldNameMustNotBeginWithUnderscore>(code);
            }

            [TestCase(typeof(FieldNameMustNotBeginWithUnderscore))]
            [TestCase(typeof(FieldNameMustNotBeginWithUnderscoreDisabled))]
            public void SingleClassOneErrorType(Type type)
            {
                var code = @"
namespace RoslynSandbox
{
    class Foo
    {
        private readonly int ↓_value = 1;
    }
}";
                AnalyzerAssert.Diagnostics(type, code);
            }

            [Test]
            public void SingleClassOneErrorPassingAnalyzer()
            {
                var code = @"
namespace RoslynSandbox
{
    class Foo
    {
        private readonly int ↓_value = 1;
    }
}";
                AnalyzerAssert.Diagnostics(new FieldNameMustNotBeginWithUnderscore(), code);
            }

            [Test]
            public void SingleClassTwoErrorsIndicated()
            {
                var code = @"
namespace RoslynSandbox
{
    class Foo
    {
        private readonly int ↓_value1;
        private readonly int ↓_value2;
    }
}";

                AnalyzerAssert.Diagnostics<FieldNameMustNotBeginWithUnderscore>(code);
            }

            [Test]
            public void TwoClassesOneError()
            {
                var foo1 = @"
namespace RoslynSandbox
{
    class Foo1
    {
        private readonly int ↓_value = 1;
    }
}";
                var foo2 = @"
namespace RoslynSandbox
{
    class Foo2
    {
    }
}";
                AnalyzerAssert.Diagnostics<FieldNameMustNotBeginWithUnderscore>(foo1, foo2);
            }

            [Test]
            public void TwoClassesTwoErrorsGeneric()
            {
                var foo1 = @"
namespace RoslynSandbox
{
    class Foo1
    {
        private readonly int ↓_value = 1;
    }
}";
                var foo2 = @"
namespace RoslynSandbox
{
    class Foo2
    {
        private readonly int ↓_value = 1;
    }
}";
                AnalyzerAssert.Diagnostics<FieldNameMustNotBeginWithUnderscore>(foo1, foo2);
            }

            [TestCase(typeof(FieldNameMustNotBeginWithUnderscore))]
            [TestCase(typeof(FieldNameMustNotBeginWithUnderscoreDisabled))]
            public void TwoClassesTwoErrorsType(Type type)
            {
                var foo1 = @"
namespace RoslynSandbox
{
    class Foo1
    {
        private readonly int ↓_value = 1;
    }
}";
                var foo2 = @"
namespace RoslynSandbox
{
    class Foo2
    {
        private readonly int ↓_value = 1;
    }
}";
                AnalyzerAssert.Diagnostics(type, foo1, foo2);
            }
        }
    }
}