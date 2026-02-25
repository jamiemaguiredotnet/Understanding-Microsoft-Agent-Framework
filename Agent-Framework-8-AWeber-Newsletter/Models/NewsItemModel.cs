using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent_Framework_8_AWeber_Newsletter.Models
{
    public class NewsItemModel
    {
        public string Headline { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string PublishedDate { get; set; } = string.Empty;

        public NewsItemModel(string Headline, string Summary, string Url, string Source, string Company, string PublishedDate = "")
        {
            this.Headline = Headline;
            this.Summary = Summary;
            this.Url = Url;
            this.Source = Source;
            this.Company = Company;
            this.PublishedDate = PublishedDate;
        }
    }
}
