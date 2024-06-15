using System;
using System.Collections.Generic;
using System.Text;

namespace CpuGuard.NET.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CpuLimitAttribute : Attribute
    {
        public double CpuLimitPercentage { get; }

        public CpuLimitAttribute(double cpuLimitPercentage)
        {
            CpuLimitPercentage = cpuLimitPercentage;
        }
    }
}
