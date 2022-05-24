using DCHMediaPicker.Core.Models;
using DCHMediaPicker.Data.Models;
using System.Collections.Generic;
using System.Linq;
using static DCHMediaPicker.Data.Models.DCHSearchRequest;

namespace DCHMediaPicker.Core.Factories
{
    public class DCHSearchRequestFactory
    {
        public DCHSearchRequest Create(AdvancedSearchRequest searchRequest, int itemsPerPage, IEnumerable<DCHFilter> defaultFilters = null)
        {
            var dchSearchRequest = new DCHSearchRequest()
            {
                Skip = Helper.GetSkipAmount(searchRequest.Page, itemsPerPage),
                Take = itemsPerPage,
            };

            if (defaultFilters != null)
            {
                dchSearchRequest.Filters.AddRange(defaultFilters);
            }

            if (!string.IsNullOrEmpty(searchRequest.SearchTerms))
            {
                dchSearchRequest.Fulltext = searchRequest.SearchTerms.Split(' ');
            }

            if (!string.IsNullOrEmpty(searchRequest.Keywords))
            {
                dchSearchRequest.Filters.Add(new DCHFilter()
                {
                    Name = "Keywords",
                    Operator = "Contains",
                    Values = searchRequest.Keywords.Split(' ')
                });
            }

            if (!string.IsNullOrEmpty(searchRequest.FileType))
            {
                var existingMimeFilter = dchSearchRequest.Filters.SingleOrDefault(x => x.Name.Equals("MIMEType"));

                if (existingMimeFilter != null)
                {
                    dchSearchRequest.Filters.Remove(existingMimeFilter);
                }

                dchSearchRequest.Filters.Add(new DCHFilter()
                {
                    Name = "MIMEType",
                    Operator = "Contains",
                    Values = new string[] { searchRequest.FileType }
                });
            }

            if (searchRequest.Modified != null)
            {
                dchSearchRequest.Filters.Add(new DCHFilter()
                {
                    Name = "modified_on",
                    Operator = "Between",
                    Values = new string[] 
                    { 
                        searchRequest.Modified.Start.ToString("o"),
                        searchRequest.Modified.End.ToString("o")
                    }
                });
            }

            return dchSearchRequest;
        }

        public DCHSearchRequest Create(string q, int page, int itemsPerPage, IEnumerable<DCHFilter> defaultFilters = null)
        {
            var dchSearchRequest = new DCHSearchRequest()
            {
                Skip = Helper.GetSkipAmount(page, itemsPerPage),
                Take = itemsPerPage,
            };

            if (defaultFilters != null)
            {
                dchSearchRequest.Filters.AddRange(defaultFilters);
            }

            if (!string.IsNullOrEmpty(q))
            {
                dchSearchRequest.Fulltext = q.Split(' ');
            }

            return dchSearchRequest;
        }
    }
}