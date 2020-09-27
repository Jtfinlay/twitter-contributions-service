
using System;
using Microsoft.AspNetCore.Mvc;

public class RateLimitedActionResult : ObjectResult
{
    public RateLimitedActionResult(double value) : base(value)
    {
        this.StatusCode = 429;
    }
}