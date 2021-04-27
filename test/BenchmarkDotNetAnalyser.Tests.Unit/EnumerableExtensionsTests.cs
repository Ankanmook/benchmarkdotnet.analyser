﻿using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace BenchmarkDotNetAnalyser.Tests.Unit
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void MinBy_Decimal_EmptySequence_ExceptionThrown()
        {
            var xs = Enumerable.Empty<decimal>();

            Func<decimal> f = () => xs.MinBy(x => x);

            f.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MinBy_Decimal_EmptySequence_ExceptionMessageNonEmpty()
        {
            var xs = Enumerable.Empty<decimal>();

            try
            {
                var r = xs.MinBy(x => x);
            }
            catch (InvalidOperationException e)
            {
                if (string.IsNullOrWhiteSpace(e.Message))
                {
                    throw new AssertionFailedException("Missing message");
                }
            }
        }

        [Property(Verbose = true)]
        public bool MinBy_Decimal_FindsMinByReverse(PositiveInt count)
        {
            var xs = Enumerable.Range(1, count.Get).Select(x => (decimal)x).ToArray();
            
            var r1 = xs.MinBy(x => x);
            var r2 = xs.Min();

            return r1 == r2;
        }

        [Property(Verbose = true)]
        public bool MinBy_Decimal_Reversed_FindsMinByReverse(PositiveInt count)
        {
            var xs = Enumerable.Range(1, count.Get).Select(x => (decimal)x).ToArray();
            
            var r1 = xs.Reverse().MinBy(x => x);
            var r2 = xs.Min();

            return r1 == r2;
        }

        [Property(Verbose = true)]
        public bool MinBy_Decimal_ValuesEqual_FindsMinByReverse(PositiveInt count)
        {
            var xs = Enumerable.Range(1, count.Get).Select(x => (decimal)x).ToList();
            var r2 = xs.Min();
            xs.Add(r2);

            var r1 = xs.MinBy(x => x);
            
            return r1 == r2;
        }

        [Property(Verbose = true)]
        public bool MinBy_Decimal_StringValueProjections_FindsMinByReverse(PositiveInt count)
        {
            var xs = Enumerable.Range(1, count.Get).Select(x => x.ToString()).ToArray();

            var r1 = xs.Reverse().MinBy(decimal.Parse);
            var r2 = xs.Min();

            return r1 == r2;
        }
    }
}
