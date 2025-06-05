// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nethermind.Libp2p.Core.Extensions;

public static class SequenceExtensions
{
    public static async Task<byte[]> AsByteArray(this IAsyncEnumerable<ReadOnlySequence<byte>> sequence)
    {
        var result = new List<byte>();

        await foreach (var chunk in sequence)
        {
            result.AddRange(chunk.ToArray());
        }

        return [.. result];
    }

    public static async Task<string> AsUtf8String(this IAsyncEnumerable<ReadOnlySequence<byte>> sequence)
    {
        var result = await sequence.AsByteArray();

        return Encoding.UTF8.GetString(result);
    }

    public static async Task<string> AsAsciiString(this IAsyncEnumerable<ReadOnlySequence<byte>> sequence)
    {
        var result = await sequence.AsByteArray();

        return Encoding.ASCII.GetString(result);
    }
}
