﻿using System;
using System.Linq;
using SFB.Web.UI.Helpers.Constants;
using SFB.Web.ApplicationCore.Entities;
using System.Globalization;
using SFB.Web.ApplicationCore.Helpers.Enums;

namespace SFB.Web.UI.Models
{
    public class SchoolViewModel : EstablishmentViewModelBase
    {
        public EdubaseDataObject ContextDataModel { get; set; }

        public SchoolComparisonListModel ManualComparisonList { get; set; }

        public SchoolViewModel(EdubaseDataObject contextDataModel)
        {
            this.ContextDataModel = contextDataModel;
        }

        public SchoolViewModel(EdubaseDataObject schoolContextDataModel, SchoolComparisonListModel comparisonList)
        {
            this.ContextDataModel = schoolContextDataModel;
            base.ComparisonList = comparisonList;
        }

        public SchoolViewModel(EdubaseDataObject schoolContextDataModel, SchoolComparisonListModel comparisonList, SchoolComparisonListModel manualComparisonList)
        {
            this.ContextDataModel = schoolContextDataModel;
            base.ComparisonList = comparisonList;
            this.ManualComparisonList = manualComparisonList;
        }

        public LocationDataObject GetLocation()
        {
            return ContextDataModel.Location;
        }

        public bool IsInComparisonList
        {
            get
            {
                if (ComparisonList == null)
                {
                    return false;
                }
                return base.ComparisonList.BenchmarkSchools.Any(s => s.Urn == ContextDataModel.URN.ToString());
            }
        }

        public bool IsDefaultBenchmark => base.ComparisonList.HomeSchoolUrn == ContextDataModel.URN.ToString();

        public int Id => ContextDataModel.URN;

        public string LaEstab => $"{ContextDataModel.LACode} {ContextDataModel.EstablishmentNumber}";

        public override string Name
        {
            get
            {
                return ContextDataModel.EstablishmentName;
            }

            set { }
        }

        public int La => ContextDataModel.LACode;

        public string LaName { get; set; }

        public int Estab => ContextDataModel.EstablishmentNumber;

        public string OverallPhase => ContextDataModel.OverallPhase;

        public string Phase => ContextDataModel.PhaseOfEducation;

        public string SchoolWebSite
        {
            get {
                var url = ContextDataModel.SchoolWebsite;
                if (!string.IsNullOrEmpty(url) && !url.ToLower().StartsWith("http"))
                {
                    url = "http://" + url;
                }

                return url;
            }
        }

        public string AgeRange => $"{ContextDataModel.StatutoryLowAge} to {ContextDataModel.StatutoryHighAge}";

        public string HeadTeachFullName => $"{ContextDataModel.HeadFirstName} {ContextDataModel.HeadLastName}";

        public string TrustName => ContextDataModel.Trusts;

        public int? CompanyNo => ContextDataModel.CompanyNumber;

        public string PhoneNumber => ContextDataModel.TelephoneNum;

        public string OfstedRating => ContextDataModel.OfstedRating;

        public string OfstedInspectionDate 
        {
            get
            {
                try
                {
                    return DateTime.Parse(ContextDataModel.OfstedLastInsp, CultureInfo.CurrentCulture, DateTimeStyles.None).ToLongDateString();                    
                }
                catch
                {
                    return null;
                }
            }
        }

        public string OpenDate
        {
            get
            {
                try
                {
                    return DateTime.Parse(ContextDataModel.OpenDate, CultureInfo.CurrentCulture, DateTimeStyles.None).ToLongDateString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public string CloseDate
        {
            get
            {
                try
                {
                    return DateTime.Parse(ContextDataModel.CloseDate, CultureInfo.CurrentCulture, DateTimeStyles.None).ToLongDateString();
                }
                catch
                {
                    return null;
                }
            }
        }

        public string OfstedRatingText
        {
            get
            {
                switch (OfstedRating)
                {
                    case OfstedRatings.Rating.GOOD:
                        return OfstedRatings.Description.GOOD;
                    case OfstedRatings.Rating.OUTSTANDING:
                        return OfstedRatings.Description.OUTSTANDING;
                    case OfstedRatings.Rating.INADEQUATE:
                        return OfstedRatings.Description.INADEQUATE;
                    case OfstedRatings.Rating.REQUIRES_IMPROVEMENT:
                        return OfstedRatings.Description.REQUIRES_IMPROVEMENT;
                    default:
                        return OfstedRatings.Description.NOT_RATED;
                }
            }
        }

        public float TotalPupils => ContextDataModel.NumberOfPupils.GetValueOrDefault();

        public string IsPost16 => ContextDataModel.OfficialSixthForm == "Has a sixth form" ? "Yes" : "No";

        public string HasNursery => ContextDataModel.HasNursery == "Has Nursery Classes" ?  "Yes" : "No";

        public string Address => $"{ContextDataModel.Street}, {ContextDataModel.Town}, {ContextDataModel.Postcode}";

        public override string Type => ContextDataModel.TypeOfEstablishment;

        public override EstablishmentType EstablishmentType => (EstablishmentType)Enum.Parse(typeof(EstablishmentType), ContextDataModel.FinanceType);

        public bool IsSAT => LatestYearFinancialData.IsSAT;

        public bool IsMAT => LatestYearFinancialData.IsMAT;

        public decimal? FSM => LatestYearFinancialData.PercentageOfEligibleFreeSchoolMeals;

        public decimal? SEN => LatestYearFinancialData.PercentageOfPupilsWithSen;

        public string UrbanRural => LatestYearFinancialData.UrbanRural;

        public decimal? ExpenditurePerPupil => LatestYearFinancialData.PerPupilTotalExpenditure;

        public string PhaseInFinancialSubmission => LatestYearFinancialData.SchoolPhase;

        public string OverallPhaseInFinancialSubmission => LatestYearFinancialData.SchoolOverallPhase;

        public BicProgressScoreType BicProgressScoreType
        {
            get {
                switch (OverallPhaseInFinancialSubmission)
                {
                    case "Primary":
                        return BicProgressScoreType.KS2;
                    case "Secondary":
                    case "All-through":
                    default:
                        return BicProgressScoreType.P8;
                }
            }
        }

        public decimal? ProgressScore => LatestYearFinancialData.ProgressScore;

        public decimal? KS2ProgressScore => LatestYearFinancialData.Ks2Progress;

        public decimal? P8ProgressScore => LatestYearFinancialData.P8Mea;

        public decimal? P8Banding => LatestYearFinancialData.P8Banding;

        public bool HasCoordinates
        {
            get
            {
                try
                {
                    var lat = GetCoordinates(1);
                    var lng = GetCoordinates(0);

                    if (lat == string.Empty || lng == string.Empty)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool InsideSearchArea { get; set; }

        public string Lat => GetCoordinates(1);
        public string Lng => GetCoordinates(0);

        private string GetCoordinates(int index)
        {
            var location = GetLocation();
            if (location != null)
            {
                switch (index)
                {
                    case 0:
                        return location.coordinates[0];
                    case 1:
                        return location.coordinates[1];
                }
            }
            return string.Empty;
        }
    }

    public class SchoolViewModelWithNoDefaultSchool : SchoolViewModel
    {
        public SchoolViewModelWithNoDefaultSchool() : base(null)
        {
        }

        public SchoolViewModelWithNoDefaultSchool(SchoolComparisonListModel manualComparisonList) : base(null, null, manualComparisonList)
        {
        }

        public SchoolViewModelWithNoDefaultSchool(SchoolComparisonListModel schoolComparisonList, SchoolComparisonListModel manualComparisonList) : base(null, schoolComparisonList, manualComparisonList)
        {
        }

        public override string Name { get => null; }

        public override string Type => null;
    }
}