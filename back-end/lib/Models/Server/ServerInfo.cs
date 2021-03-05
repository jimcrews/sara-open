using Sara.Lib.Extensions;

namespace Sara.Lib.Models.Server
{
    public class ServerInfo
    {
        public string ServerName { get; set; }
        public string TypeName { get; set; }
        public string ConnectionString { get; set; }

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return Sara.Lib.Extensions.Hash
                .Start
                .Hash(ServerName)
                .Hash(TypeName)
                .Hash(ConnectionString);


        }

    }
}