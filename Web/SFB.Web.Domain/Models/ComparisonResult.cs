﻿using System.Collections.Generic;
using SFB.Web.Common;
using SFB.Web.Common.Entities;

namespace SFB.Web.Domain.Models
{
    public class ComparisonResult
    {
        public List<SchoolTrustFinancialDataObject> BenchmarkSchools { get; set; }
        public BenchmarkCriteria BenchmarkCriteria { get; set; }
    }
}
