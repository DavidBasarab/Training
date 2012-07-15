using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MovieService.Classes
{
    [DataContract]
    public class MovieList
    {
        [DataMember]
        public IList<Movie> Movies { get; set; }
    }
}