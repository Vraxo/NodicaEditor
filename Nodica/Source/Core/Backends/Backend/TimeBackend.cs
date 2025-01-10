﻿namespace Nodica.Backends;

public abstract class ITimeBackend
{
    public abstract float GetDeltaTime();
    public abstract float GetElapsedTime();
}