using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using MovieService.Classes;

namespace MovieService
{
    [ServiceContract]
    public interface IMovieProvider
    {
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        MovieList GetMovies();
    }
}
