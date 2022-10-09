using MLL.Common.Layer.Backpropagation;
using MLL.Layer.Backpropagation;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MLL.Tests;

public class SingleComparer : IComparer<float>, IComparer
{
    private readonly float _tolerance;

    public SingleComparer(float tolerance)
    {
        _tolerance = tolerance;
    }

    public int Compare(float x, float y)
    {
        if (Math.Abs(x - y) <= _tolerance)
        {
            return 0;
        }

        return x > y ? +1 : -1;
    }

    public int Compare(object? x, object? y)
    {
        if (x is float xi && y is float yi)
        {
            return Compare(xi, yi);
        }

        throw new InvalidOperationException();
    }
}

public class ErrorBackpropTests
{
    private readonly IErrorBackpropagation _errorBackprop = new ErrorBackpropagation();
    private readonly IErrorBackpropagation _threadedErrorBackprop = new ThreadedErrorBackpropagation()
    {
        ThreadInfo = new(8)
    };

    [Test]
    public void SingleAndMultiThreaded_SameResults()
    {
        var random = new Random(676);

        const int neuronsCount = 10;
        const int weightsCount = 20;

        var previousNeurons = new float[neuronsCount][];
        var previousErrors = new float[neuronsCount];
        var errorsBuffer = new float[weightsCount];
        var threadedErrorsBuffer = new float[weightsCount];

        for (int ni = 0; ni < previousNeurons.Length; ni++)
        {
            previousNeurons[ni] = new float[weightsCount];

            for (int wi = 0; wi < previousNeurons[ni].Length; wi++)
            {
                previousNeurons[ni][wi] = random.NextSingle();
            }
        }

        for (int ei = 0; ei < previousErrors.Length; ei++)
        {
            previousErrors[ei] = random.NextSingle();
        }

        var ctx = new BackpropContext(previousNeurons, previousErrors);

        _errorBackprop.ReorganizeErrors(ctx, errorsBuffer);
        _threadedErrorBackprop.ReorganizeErrors(ctx, threadedErrorsBuffer);

        var comparer = new SingleComparer(0.0000001f);
        CollectionAssert.AreEqual(errorsBuffer, threadedErrorsBuffer, comparer);
    }
}
