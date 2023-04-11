﻿namespace GrayMint.Common;

public class ListResult<T>
{
    public required long? TotalCount { get; set; }
    public required IEnumerable<T> Items { get; set; }
}