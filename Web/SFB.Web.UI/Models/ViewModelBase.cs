﻿using Newtonsoft.Json;

namespace SFB.Web.UI.Models
{
    public abstract class ViewModelBase
    {
        [JsonIgnore]
        public SchoolComparisonListModel ComparisonList;

        [JsonIgnore]
        public int ComparisonListCount => ComparisonList?.BenchmarkSchools?.Count ?? 0;

        [JsonIgnore]
        public string ErrorMessage { get; set; }

        public bool HasError()
        {
            return !string.IsNullOrEmpty(this.ErrorMessage);
        }
    }
}