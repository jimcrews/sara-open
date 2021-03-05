using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sara.Lib.Json
{
    /// <summary>
    /// Represents a json object. This object is decorator object used to represent
    /// objects that can be stored as JSON documents (e.g. using the SQL Server
    /// JSON type.
    /// A loader type is free to generate data of type JsonObject. If it does,
    /// the data will store as JSON.
    /// </summary>
    public class JsonObject
    {
        private object BaseObject { get; set; }
        JsonConverter[] Converters { get; set; }

        public JsonObject(object baseObject, params JsonConverter[] converters)
        {
            this.BaseObject = baseObject;
            this.Converters = converters;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(BaseObject, Converters);
        }
    }
}