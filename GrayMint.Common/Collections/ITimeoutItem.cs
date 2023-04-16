﻿namespace GrayMint.Common.Collections;

public interface ITimeoutItem : IDisposable
{
    DateTime LastUsedTime { get; set; }
    bool Disposed { get; }
}