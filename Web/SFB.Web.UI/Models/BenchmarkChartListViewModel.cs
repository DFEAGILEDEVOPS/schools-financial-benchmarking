﻿using System.Collections.Generic;
using System.Linq;
using SFB.Web.ApplicationCore.Models;
using SFB.Web.UI.Helpers.Enums;
using SFB.Web.ApplicationCore.Helpers.Enums;

namespace SFB.Web.UI.Models
{
    public class BenchmarkChartListViewModel : ViewModelListBase<ChartViewModel>
    {
        public string SchoolArea { get; set; }
        public string SelectedArea { get; set; }
        public List<ChartViewModel> ChartGroups { get; set; }
        public ComparisonType ComparisonType { get; set; }
        public BenchmarkCriteria AdvancedCriteria { get; set; }
        public SimpleCriteria SimpleCriteria { get; set; }
        public BestInClassCriteria BicCriteria { get; set; }
        public FinancialDataModel BenchmarkSchoolData { get; set; }    
        public EstablishmentType EstablishmentType { get; set; }
        public EstablishmentType SearchedEstablishmentType { get; set; }
        public TrustComparisonListModel TrustComparisonList { get; set; }
        public List<EstablishmentViewModelBase> ComparisonSchools { get; set; }
        public bool ExcludePartial { get; set; }
        public string LatestTermAcademies { get; set; }
        public string LatestTermMaintained { get; set; }
        public ComparisonArea AreaType { get; }
        public string LaCode { get; }
        public long? URN { get; }

        public bool HasIncompleteFinancialData
        {
            get
            {
                if (this.EstablishmentType == EstablishmentType.MAT)
                {
                    return ModelList.SelectMany(m => m.BenchmarkData).Any(d => !d.IsCompleteYear)
                           || ModelList.SelectMany(m => m.BenchmarkData).Any(d => d.PartialYearsPresentInSubSchools);
                }
                return ModelList.SelectMany(m => m.BenchmarkData).Any(d => !d.IsCompleteYear);
            }
        }

        public bool HasIncompleteWorkforceData
        {
            get
            {
                if (this.EstablishmentType == EstablishmentType.MAT)
                {
                    return false;
                }
                else
                {
                    return ModelList.SelectMany(m => m.BenchmarkData).Any(d => !d.IsWFDataPresent);
                }
            }
        }

        public bool HasNoTeacherData => ModelList.SelectMany(m => m.BenchmarkData).Any(d => d.TeacherCount == 0m);

        public bool NoResultsForSimpleSearch => (ComparisonType == ComparisonType.Basic && ComparisonListCount < 2);
        public int BasketSize { get; set; }

        public BenchmarkChartListViewModel(List<ChartViewModel> modelList, SchoolComparisonListModel comparisonList, List<ChartViewModel> chartGroups, 
            ComparisonType comparisonType, BenchmarkCriteria advancedCriteria, SimpleCriteria simpleCriteria, BestInClassCriteria bicCriteria, 
            FinancialDataModel benchmarkSchoolData, EstablishmentType estabType, EstablishmentType searchedEstabType, string schoolArea, string selectedArea, 
            string latestTermAcademies, string latestTermMaintained, ComparisonArea areaType, string laCode, long? urn, int basketSize, 
            TrustComparisonListModel trustComparisonList = null, List<EstablishmentViewModelBase> comparisonSchools = null, bool excludePartial = false)
            :base(modelList, comparisonList)
        {
            this.ChartGroups = chartGroups;
            this.AdvancedCriteria = advancedCriteria;
            this.SimpleCriteria = simpleCriteria;
            this.BicCriteria = bicCriteria;
            this.ComparisonType = comparisonType;
            this.BenchmarkSchoolData = benchmarkSchoolData;
            this.EstablishmentType = estabType;
            this.SearchedEstablishmentType = searchedEstabType;
            this.SchoolArea = schoolArea;
            this.SelectedArea = selectedArea;
            this.TrustComparisonList = trustComparisonList;
            this.LatestTermAcademies = latestTermAcademies;
            this.LatestTermMaintained = latestTermMaintained;
            this.AreaType = areaType;
            this.LaCode = laCode;
            this.URN = urn;
            this.BasketSize = basketSize;
            this.ComparisonSchools = comparisonSchools;
            this.ExcludePartial = excludePartial;
        }
    }
}