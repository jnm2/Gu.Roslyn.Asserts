namespace Gu.Roslyn.Asserts.Analyzers.Tests.GURA07TestClassShouldBePublicStatic
{
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ClassDeclarationAnalyzer();

        [Test]
        public static void WhenNoAsserts()
        {
            var code = @"
namespace N
{
    using System;

    internal class C
    {
        private static readonly PlaceholderAnalyzer Analyzer = new PlaceholderAnalyzer();

        void M()
        {
            var c = ""class C { }"";
            Console.WriteLine(c);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Code.PlaceholderAnalyzer, code);
        }

        [Test]
        public static void WhenPublicStatic()
        {
            var code = @"
namespace N
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly PlaceholderAnalyzer Analyzer = new PlaceholderAnalyzer();

        [Test]
        public static void M()
        {
            var c = ""class C { }"";
            RoslynAssert.Valid(Analyzer, c);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Code.PlaceholderAnalyzer, code);
        }

        [Test]
        public static void WhenNestedClass()
        {
            var code = @"
namespace N
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly PlaceholderAnalyzer Analyzer = new PlaceholderAnalyzer();

        [Test]
        public static void M()
        {
            var c = ""class C { }"";
            RoslynAssert.Valid(Analyzer, c);
        }

        private class C { }
    }
}";
            RoslynAssert.Valid(Analyzer, Code.PlaceholderAnalyzer, code);
        }

        [Test]
        public static void ScriptAttribute()
        {
            var code = @"
namespace N
{
    using NUnit.Framework;

    public class ScriptAttribute : ExplicitAttribute
    {
        public ScriptAttribute()
         : base(""SCRIPT"")
        {
        }
    }
}";
            RoslynAssert.Valid(Analyzer, Code.PlaceholderAnalyzer, code);
        }
    }
}
