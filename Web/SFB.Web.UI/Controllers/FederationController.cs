﻿using Microsoft.Ajax.Utilities;
using SFB.Web.ApplicationCore.Entities;
using SFB.Web.ApplicationCore.Helpers;
using SFB.Web.ApplicationCore.Helpers.Enums;
using SFB.Web.ApplicationCore.Models;
using SFB.Web.ApplicationCore.Services;
using SFB.Web.ApplicationCore.Services.DataAccess;
using SFB.Web.ApplicationCore.Services.LocalAuthorities;
using SFB.Web.UI.Helpers;
using SFB.Web.UI.Helpers.Constants;
using SFB.Web.UI.Helpers.Enums;
using SFB.Web.UI.Models;
using SFB.Web.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SFB.Web.UI.Controllers
{
    public class FederationController : Controller
    {

        private readonly IFinancialDataService _financialDataService;
        private readonly IContextDataService _contexDataService;
        private readonly IHistoricalChartBuilder _historicalChartBuilder;
        private readonly IFinancialCalculationsService _fcService;
        private readonly ILocalAuthoritiesService _laService;

        public FederationController(IHistoricalChartBuilder historicalChartBuilder, IFinancialDataService financialDataService,
            IFinancialCalculationsService fcService, IContextDataService contexDataService, ILocalAuthoritiesService laService)
        {
            _historicalChartBuilder = historicalChartBuilder;
            _financialDataService = financialDataService;
            _contexDataService = contexDataService;
            _fcService = fcService;
            _laService = laService;
        }

        public async Task<ActionResult> Index(int fuid,
            UnitType unit = UnitType.AbsoluteMoney,
            TabType tab = TabType.Expenditure,
            ChartFormat format = ChartFormat.Charts)
        {
            OverwriteDefaultUnitTypeForSelectedTab(tab, ref unit);
            var chartGroup = DetectDefaultChartGroupFromTabType(tab);

            var vm = await BuildFederationViewModelAsync(fuid, tab, chartGroup);

            ViewBag.Tab = tab;
            ViewBag.ChartGroup = chartGroup;
            ViewBag.UnitType = unit;
            ViewBag.ChartFormat = format;
            ViewBag.EstablishmentType = EstablishmentType.Federation;

            return View(vm);
        }

        private async Task<FederationViewModel> BuildFederationViewModelAsync(int fuid, TabType tab, ChartGroupType chartGroup)
        {
            //var comparisonListVM = _benchmarkBasketService.GetSchoolBenchmarkList();
            var vm = new FederationViewModel(fuid);

            vm.HistoricalCharts = _historicalChartBuilder.Build(tab, chartGroup, vm.EstablishmentType);
            vm.ChartGroups = _historicalChartBuilder.Build(tab, vm.EstablishmentType).DistinctBy(c => c.ChartGroup).ToList();
            vm.LatestTerm = await LatestFederationTermAsync();

            vm.HistoricalFinancialDataModels = await this.GetFinancialDataHistoricallyAsync(fuid);

            vm.SchoolsInFederation = await _contexDataService.GetMultipleSchoolDataObjectsByUrnsAsync(vm.FederationMembersURNs.ToList());
            vm.LaName = _laService.GetLaName(vm.La.ToString());

            return vm;
        }

        private async Task<List<FinancialDataModel>> GetFinancialDataHistoricallyAsync(int fuid)
        {
            var models = new List<FinancialDataModel>();
            var latestYear = await _financialDataService.GetLatestDataYearPerEstabTypeAsync(EstablishmentType.Federation);

            var taskList = new List<Task<SchoolTrustFinancialDataObject>>();
            for (int i = ChartHistory.YEARS_OF_HISTORY - 1; i >= 0; i--)
            {
                var term = SchoolFormatHelpers.FinancialTermFormatAcademies(latestYear - i);
                var task = _financialDataService.GetFederationFinancialDataObjectByFuidAsync(fuid, term);
                taskList.Add(task);
            }

            for (int i = ChartHistory.YEARS_OF_HISTORY - 1; i >= 0; i--)
            {
                var term = SchoolFormatHelpers.FinancialTermFormatAcademies(latestYear - i);
                var taskResult = await taskList[ChartHistory.YEARS_OF_HISTORY - 1 - i];
                var resultObject = taskResult;

                if (resultObject != null && resultObject.DidNotSubmit)
                {
                    var emptyObj = new SchoolTrustFinancialDataObject();
                    emptyObj.DidNotSubmit = true;
                    resultObject = emptyObj;
                }

                models.Add(new FinancialDataModel(fuid.ToString(), term, resultObject, EstablishmentType.Federation));
            }

            return models;
        }

        private async Task<string> LatestFederationTermAsync()
        {
            var latestYear = await _financialDataService.GetLatestDataYearPerEstabTypeAsync(EstablishmentType.Federation);
            return SchoolFormatHelpers.FinancialTermFormatAcademies(latestYear);
        }

        private void OverwriteDefaultUnitTypeForSelectedTab(TabType tabType, ref UnitType unitType)
        {
            switch (tabType)
            {
                case TabType.Workforce:
                    unitType = UnitType.AbsoluteCount;
                    break;
                case TabType.Balance:
                    unitType = unitType == UnitType.PercentageOfTotalIncome || unitType == UnitType.PercentageOfTotalExpenditure || unitType == UnitType.PerPupil || unitType == UnitType.PerTeacher ? unitType : UnitType.AbsoluteMoney;
                    break;
                case TabType.Income:
                    unitType = unitType == UnitType.PercentageOfTotalExpenditure ? UnitType.PercentageOfTotalExpenditure : unitType;
                    break;
                case TabType.Expenditure:
                    unitType = unitType == UnitType.PercentageOfTotalIncome ? UnitType.PercentageOfTotalIncome : unitType;
                    break;
            }
        }

        private ChartGroupType DetectDefaultChartGroupFromTabType(TabType tab)
        {
            ChartGroupType chartGroup;
            switch (tab)
            {
                case TabType.Expenditure:
                    chartGroup = ChartGroupType.TotalExpenditure;
                    break;
                case TabType.Income:
                    chartGroup = ChartGroupType.TotalIncome;
                    break;
                case TabType.Balance:
                    chartGroup = ChartGroupType.InYearBalance;
                    break;
                case TabType.Workforce:
                    chartGroup = ChartGroupType.Workforce;
                    break;
                default:
                    chartGroup = ChartGroupType.All;
                    break;
            }

            return chartGroup;
        }
    }
}