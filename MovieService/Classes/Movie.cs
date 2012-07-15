using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace MovieService.Classes
{
    [DataContract]
    public class Movie
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Director { get; set; }

        [DataMember]
        public List<Actor> Cast { get; set; } 
    }
}