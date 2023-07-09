using System;
using System.Collections.Generic;
using System.Web;
using MySql.Data.MySqlClient;
using System.Linq;
using System.IO;
class Program
{
    static void sendresponse(string response, string user, string wiki)
    {
        var sr = new StreamReader("thanks-stats.html");
        Console.WriteLine(sr.ReadToEnd().Replace("%response%", response).Replace("%user%", user).Replace("%wiki%", wiki));
    }
    static string url2db(string url)
    {
        return url.Replace(".", "").Replace("wikipedia", "wiki");
    }
    static void Main()
    {
        var thanked = new Dictionary<string, int>();
        var thankers = new Dictionary<string, int>();
        var users = new HashSet<string>();
        MySqlDataReader r;
        MySqlCommand command;

        string input = Environment.GetEnvironmentVariable("QUERY_STRING");
        //input = "user=MBH&wiki=ru.wikipedia";
        if (input == "" || input == null)
        {
            sendresponse("", "", "ru.wikipedia");
            return;
        }
        var parameters = HttpUtility.ParseQueryString(input);
        string user = parameters["user"];
        string wiki = parameters["wiki"];
        var creds = new StreamReader("../p").ReadToEnd().Split('\n');
        var connect = new MySqlConnection("Server=" + url2db(wiki) + ".labsdb;Database=" + url2db(wiki) + "_p;Uid=" + creds[2] + ";Pwd=" + creds[3] + ";CharacterSet=utf8mb4;SslMode=none;");
        connect.Open();

        command = new MySqlCommand("select cast(replace (log_title, '_', ' ') as char) from logging where log_type=\"thanks\" and log_actor=(select actor_id from actor where actor_name=\"" + user +
            "\");", connect) { CommandTimeout = 9999 };
        r = command.ExecuteReader();
        while (r.Read())
        {
            string name = r.GetString(0);
            if (!thanked.ContainsKey(name))
                thanked.Add(name, 1);
            else
                thanked[name]++;
        }
        r.Close();

        command = new MySqlCommand("select cast(actor_name as char) source from (select log_actor from logging where log_type=\"thanks\" and log_title=\"" + user.Replace(' ', '_') + "\") log join " +
            "actor on actor_id=log_actor;", connect) { CommandTimeout = 9999 };
        r = command.ExecuteReader();
        while (r.Read())
        {
            string name = r.GetString(0);
            if (!thankers.ContainsKey(name))
                thankers.Add(name, 1);
            else
                thankers[name]++;
        }

        string response = "<table border=\"1\" cellspacing=\"0\">";

        foreach (var t in thanked.OrderByDescending(t => t.Value))
            response += "<tr><td>" + user + " <a href=\"https://" + wiki + ".org/w/index.php?title=special:log&type=thanks&user=" + Uri.EscapeDataString(user) + "&page=" + t.Key + "\">⇨</a>" +
                " <a href=\"https://tools.wmflabs.org/mbh/likes.cgi?user=" + Uri.EscapeDataString(t.Key) + "&wiki=" + wiki + "\">" + t.Key + "</a></td><td>" + t.Value + "</td></tr>\n";

        response += "</table></td><td valign=\"top\"><table border=\"1\" cellspacing=\"0\">";

        foreach (var t in thankers.OrderByDescending(t => t.Value))
            response += "<tr><td><a href=\"https://tools.wmflabs.org/mbh/likes.cgi?user=" + Uri.EscapeDataString(t.Key) + "&wiki=" + wiki + "\">" + t.Key + "</a>" +
                "<a href=\"https://" + wiki + ".org/w/index.php?title=special:log&type=thanks&user=" + t.Key + "&page=" + Uri.EscapeDataString(user) + "\">⇨</a>" + user +" </td><td>" + t.Value + "</td></tr>\n";

        sendresponse(response + "</table>", user, wiki);
    }
}
