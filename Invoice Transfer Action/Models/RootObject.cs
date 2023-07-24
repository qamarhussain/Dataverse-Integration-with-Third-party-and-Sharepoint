using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Invoice_Transfer_Action.Models
{
    [DataContract]
    public class Metadata
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string uri { get; set; }
        [DataMember]
        public string type { get; set; }
    }

    [DataContract]
    public class Deferred
    {
        [DataMember]
        public string uri { get; set; }
    }

    [DataContract]
    public class Files
    {
        [DataMember]
        public Deferred __deferred { get; set; }
    }

    [DataContract]
    public class Deferred2
    {
        [DataMember]
        public string uri { get; set; }
    }

    [DataContract]
    public class ListItemAllFields
    {
        [DataMember]
        public Deferred2 __deferred { get; set; }
    }

    [DataContract]
    public class Deferred3
    {
        [DataMember]
        public string uri { get; set; }
    }

    [DataContract]
    public class ParentFolder
    {
        [DataMember]
        public Deferred3 __deferred { get; set; }
    }

    [DataContract]
    public class Deferred4
    {
        [DataMember]
        public string uri { get; set; }
    }

    [DataContract]
    public class Properties
    {
        [DataMember]
        public Deferred4 __deferred { get; set; }
    }

    [DataContract]
    public class Deferred5
    {
        [DataMember]
        public string uri { get; set; }
    }

    [DataContract]
    public class StorageMetrics
    {
        [DataMember]
        public Deferred5 __deferred { get; set; }
    }

    [DataContract]
    public class Deferred6
    {
        [DataMember]
        public string uri { get; set; }
    }

    [DataContract]
    public class Folders
    {
        [DataMember]
        public Deferred6 __deferred { get; set; }
    }

    [DataContract]
    public class Result
    {
        [DataMember]
        public Metadata __metadata { get; set; }
        [DataMember]
        public Files Files { get; set; }
        [DataMember]
        public ListItemAllFields ListItemAllFields { get; set; }
        [DataMember]
        public ParentFolder ParentFolder { get; set; }
        [DataMember]
        public Properties Properties { get; set; }
        [DataMember]
        public StorageMetrics StorageMetrics { get; set; }
        [DataMember]
        public Folders Folders { get; set; }
        [DataMember]
        public bool Exists { get; set; }
        [DataMember]
        public bool IsWOPIEnabled { get; set; }
        [DataMember]
        public int ItemCount { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public object ProgID { get; set; }
        [DataMember]
        public string ServerRelativeUrl { get; set; }
        [DataMember]
        public string TimeCreated { get; set; }
        [DataMember]
        public string TimeLastModified { get; set; }
        [DataMember]
        public string UniqueId { get; set; }
        [DataMember]
        public string WelcomePage { get; set; }

        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string ClientId { get; set; }
        [DataMember]
        public string EncodedAbsUrl { get; set; }
        [DataMember]
        public string FileLeafRef { get; set; }
    }

    [DataContract]
    public class D
    {
        [DataMember]
        public List<Result> results { get; set; }
    }

    [DataContract]
    public class RootObject
    {
        [DataMember]
        public D d { get; set; }
    }
}
