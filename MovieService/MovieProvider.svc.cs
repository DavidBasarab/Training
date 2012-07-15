using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web.Script.Services;
using System.Web.Services;
using MovieService.Classes;

namespace MovieService
{
    [AspNetCompatibilityRequirements(RequirementsMode=AspNetCompatibilityRequirementsMode.Allowed)] 
    public class MovieProvider : IMovieProvider
    {
        public MovieList GetMovies()
        {
            MovieList list = new MovieList();

            IList<Movie> movies = new List<Movie>();

            var movie = new Movie
                              {
                                  Title = "Spaceballs",
                                  Director = "Mel Gibson",
                                  Cast = new List<Actor> {new Actor() {FirstName = "John", LastName = "Candy"},
                                  new Actor() {FirstName = "Mel", LastName = "Brooks"},
                                  new Actor() {FirstName = "Bill", LastName = "Pullman"},
                                  new Actor() {FirstName = "Rick", LastName = "Moranis"}}
                              };

            movies.Add(movie);
            movies.Add(new Movie
                              {
                                  Title = "CaddyShack",
                                  Director = "Harold Ramus",
                                  Cast = new List<Actor> {new Actor() {FirstName = "Bill", LastName = "Murray"},
                                  new Actor() {FirstName = "Chevy", LastName = "Chase"}}
                              });
            list.Movies = movies;
            return list;
        }
    }
}
