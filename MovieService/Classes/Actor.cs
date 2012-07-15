using System.Runtime.Serialization;

namespace MovieService.Classes
{
    [DataContract]
    public class Actor
    {
        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }
    }
}