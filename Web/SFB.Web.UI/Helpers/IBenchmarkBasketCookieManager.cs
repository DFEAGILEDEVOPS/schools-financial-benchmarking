﻿using System.Web;
using SFB.Web.UI.Models;
using SFB.Web.UI.Helpers.Enums;

namespace SFB.Web.UI.Helpers
{
    public interface IBenchmarkBasketCookieManager
    {
        SchoolComparisonListModel ExtractSchoolComparisonListFromCookie();
        void UpdateSchoolComparisonListCookie(CookieActions withAction, BenchmarkSchoolModel benchmarkSchool);
        TrustComparisonListModel ExtractTrustComparisonListFromCookie();
        TrustComparisonListModel UpdateTrustComparisonListCookie(CookieActions withAction, string matNo = null, string matName = null);        
    }
}