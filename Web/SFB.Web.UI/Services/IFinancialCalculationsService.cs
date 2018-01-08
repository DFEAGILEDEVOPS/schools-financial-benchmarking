﻿using SFB.Web.UI.Models;
using System.Collections.Generic;
using SFB.Web.UI.Helpers.Enums;
using SFB.Web.Domain.Helpers.Enums;

namespace SFB.Web.UI.Services
{
    public interface IFinancialCalculationsService
    {
        void PopulateHistoricalChartsWithSchoolData(List<ChartViewModel> historicalCharts, List<SchoolDataModel> financialDataModels, string term, RevenueGroupType revgroup, UnitType unit, SchoolFinancialType schoolFinancialType);
        void PopulateBenchmarkChartsWithFinancialData(List<ChartViewModel> benchmarkCharts, List<SchoolDataModel> financialDataModels, IEnumerable<CompareEntityBase> bmEntities, string homeSchoolId, UnitType? unit, bool trimSchoolNames = false);        
    }
}
