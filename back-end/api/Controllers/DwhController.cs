using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Sara.Lib.Metadata;
using Sara.Lib.Models.Dataset;
using Sara.Lib.Data;
using Sara.Lib.Extensions;

namespace api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DwhController : SaraController
    {
        public DwhController(IConfiguration configuration, IMetadataRepository metadataRepository) : base(configuration, metadataRepository) { }

        [HttpGet("Categories")]
        public ActionResult<IEnumerable<string>> Categories()
        {
            var datasets = MetadataRepository.GetDatasets();
            var categories = datasets.Select(d => d.Category).Distinct().OrderBy(c => c);
            return Ok(categories);
        }

        [HttpGet("Datasets")]
        public ActionResult<IEnumerable<DatasetInfo>> Datasets([FromQuery] string category = null)
        {
            var datasets = MetadataRepository.GetDatasets().OrderBy(d => d.Category).ThenBy(d => d.Dataset);

            if (string.IsNullOrEmpty(category))
                return Ok(datasets);
            else
                return Ok(datasets.Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));

        }

        [HttpGet("Data/{category}/{dataset}")]
        public ActionResult<IEnumerable> Data(string category, string dataset, int? top = null, int? sample = null, string filter = null, string param = null, string select = null, int? timeout = null)
        {
            var ds = MetadataRepository.GetDataset(category, dataset);

            if (ds == null)
            {
                return NotFound("Dataset not found.");
            }
            else
            {
                // Get the appropriate data lake provider for the dataset
                var server = MetadataRepository.GetServers().First(s => s.ServerName.Equals(ds.ServerName, StringComparison.OrdinalIgnoreCase));
                var dl = AbstractDataLake.Create(server);

                var data = dl.Query(
                    ds,
                    top,
                    sample,
                    filter,
                    param,
                    select,
                    timeout);

                // Standard return is json
                // If Accept='text/csv' header is sent, we do file download
                // NOTE THIS IS NOT YET OPTIMISED - SUGGEST DON'T USE > 1,000,000 ROWS
                if (Request != null && Request.Headers != null && Request.Headers.ContainsKey("Accept") && Request.Headers["Accept"] == "text/csv")
                {
                    // csv download
                    Response.Headers.Add("Content-Disposition", "Attachment;FileName=myfile.csv");
                    Response.Headers.Add("ContentType", "APPLICATION/octet-stream");

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (StreamWriter sw = new StreamWriter(ms))
                        {
                            data.ToCsv(sw);
                        }
                        return File(
                            ms.ToArray(),
                            "application/octet-stream",
                            string.Format("{0}.csv", dataset));
                    }
                }
                else
                {
                    // json
                    return Ok(data);
                }
            }
        }

    }
}
