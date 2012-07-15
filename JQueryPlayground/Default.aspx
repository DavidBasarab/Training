<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="JQueryPlayground._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
    <script type="text/javascript">
        $(document).ready(function () {
            $("#btnGetMovies").click(function(event) {
                $.ajax({
                    type: "GET",
                    contentType: "application/json; charset=utf-8",
                    url: "http://localhost:81/MovieService/MovieProvider.svc/GetMovies",
                    data: "{}",
                    processdata: true,
                    dataType: "json",
                    success: function(msg) {
                        AjaxSucceeded(msg);
                    },
                    error: AjaxFailed
                });
            });
        });

        function AjaxSucceeded(result) {
            var list;
            list = "";
            $.each(result.Movies, function(i, movie) {
                list += '<h2>' + movie.Title + ' (' + movie.Director + ')</h2>';
                list += '<div class="castList"><h3>Cast</h3><ol>';
                $.each(movie.Cast, function(j, actor) {
                    list += '<li>' + actor.FirstName + ' ' + actor.LastName + '</li>';
                });
                list += '</ol></div>';
            });
            
            $('#movieList').html(list);
        }
        
        function AjaxFailed(result) {
            alert('fail ' + result.status + ' ' + result.statusText);
        }  
    </script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <h1>My Movies</h1>
    <div>
        <input id="btnGetMovies" type="button" value="Get Movies" />
    </div>
    <div id="movieList">

    </div>
</asp:Content>
