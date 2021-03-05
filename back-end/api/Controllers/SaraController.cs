using System;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Extensions.Configuration;
using Sara.Lib.Metadata;

namespace api.Controllers
{
    public class SaraController : ControllerBase
    {
        protected IConfiguration Configuration;
        protected IMetadataRepository MetadataRepository;

        public SaraController(IConfiguration configuration, IMetadataRepository metadataRepository)
        {
            // Map database columns with '_' to C# Objects without '_'
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            this.Configuration = configuration;
            MetadataRepository = metadataRepository;
        }
    }
}
