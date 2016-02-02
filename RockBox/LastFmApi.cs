using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace LastFmApi.DataTypes.Track
{
    public class Streamable
    {
        public string text { get; set; }
        public string fulltrack { get; set; }
    }

    public class Artist
    {
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
    }

    public class Image
    {
        public string text { get; set; }
        public string size { get; set; }
    }

    public class Attr
    {
        public string position { get; set; }
    }

    public class Album
    {
        public string artist { get; set; }
        public string title { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public List<Image> image { get; set; }
        public Attr attr { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Toptags
    {
        public Tag tag { get; set; }
    }

    public class Track
    {
        public string id { get; set; }
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public string duration { get; set; }
        public Streamable streamable { get; set; }
        public string listeners { get; set; }
        public string playcount { get; set; }
        public Artist artist { get; set; }
        public Album album { get; set; }
        public Toptags toptags { get; set; }
        public Wiki wiki { get; set; }
    }

    public class Wiki
    {
        public string published { get; set; }
        public string summary { get; set; }
        public string content { get; set; }
    }

    public class RootObject
    {
        public Track track { get; set; }
    }
}

namespace LastFmApi.DataTypes.Track2
{
    public class Streamable
    {
        public string text { get; set; }
        public string fulltrack { get; set; }
    }

    public class Artist
    {
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
    }

    public class Image
    {
        public string text { get; set; }
        public string size { get; set; }
    }

    public class Attr
    {
        public string position { get; set; }
    }

    public class Album
    {
        public string artist { get; set; }
        public string title { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public List<Image> image { get; set; }
        public Attr attr { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Toptags
    {
        public List<Tag> tag { get; set; }
    }

    public class Track
    {
        public string id { get; set; }
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public string duration { get; set; }
        public Streamable streamable { get; set; }
        public string listeners { get; set; }
        public string playcount { get; set; }
        public Artist artist { get; set; }
        public Album album { get; set; }
        public Toptags toptags { get; set; }
        public Wiki wiki { get; set; }
    }

    public class Wiki
    {
        public string published { get; set; }
        public string summary { get; set; }
        public string content { get; set; }
    }

    public class RootObject
    {
        public Track track { get; set; }
    }
}

namespace LastFmApi.DataTypes.Album
{
    public class Image
    {
        public string text { get; set; }
        public string size { get; set; }
    }

    public class Streamable
    {
        public string text { get; set; }
        public string fulltrack { get; set; }
    }

    public class Artist
    {
        public string name { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
    }

    public class Attr
    {
        public string rank { get; set; }
    }

    public class Track
    {
        public string name { get; set; }
        public string duration { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public Streamable streamable { get; set; }
        public Artist artist { get; set; }
        public Attr attr { get; set; }
    }

    public class Tracks
    {
        public List<Track> track { get; set; }
    }

    public class Tag
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Toptags
    {
        public List<Tag> tag { get; set; }
    }

    public class Wiki
    {
        public string published { get; set; }
        public string summary { get; set; }
        public string content { get; set; }
    }

    public class Album
    {
        public string name { get; set; }
        public string artist { get; set; }
        public string id { get; set; }
        public string mbid { get; set; }
        public string url { get; set; }
        public string releasedate { get; set; }
        public List<Image> image { get; set; }
        public string listeners { get; set; }
        public string playcount { get; set; }
        public Tracks tracks { get; set; }
        public Toptags toptags { get; set; }
        public Wiki wiki { get; set; }
    }

    public class RootObject
    {
        public Album album { get; set; }
    }
}

namespace LastFmApi.Query
{
    public abstract class Query
    {
        internal string _queryString;

        public string QueryString
        {
            get
            {
                return _queryString;
            }
        }

        public string Send(Session s)
        {
            return s.Query(this.QueryString);
        }
    }
}

namespace LastFmApi.Query.Track
{
    public class GetInfo : Query
    {
        public GetInfo(string artist, string track)
        {
            base._queryString = "method=track.getInfo&artist=" + artist + "&track=" + track;
        }

        public GetInfo(string mbid)
        {
            base._queryString = "method=track.getInfo&mbid=" + mbid;
        }

        public DataTypes.Track.RootObject Get(Session s)
        {
            string o = base.Send(s);
            return JsonConvert.DeserializeObject<LastFmApi.DataTypes.Track.RootObject>(o);
        }
    }
}

namespace LastFmApi.Query.Track2
{
    public class GetInfo : Query
    {
        public GetInfo(string artist, string track)
        {
            base._queryString = "method=track.getInfo&artist=" + artist + "&track=" + track;
        }

        public GetInfo(string mbid)
        {
            base._queryString = "method=track.getInfo&mbid=" + mbid;
        }

        public DataTypes.Track2.RootObject Get(Session s)
        {
            string o = base.Send(s);
            return JsonConvert.DeserializeObject<LastFmApi.DataTypes.Track2.RootObject>(o);
        }
    }
}

namespace LastFmApi.Query.Album
{
    public class GetInfo : Query
    {
        public GetInfo(string artist, string album)
        {
            base._queryString = "method=album.getInfo&artist=" + artist + "&album=" + album;
        }

        public GetInfo(string mbid)
        {
            base._queryString = "method=album.getInfo&mbid=" + mbid;
        }

        public DataTypes.Album.RootObject Get(Session s)
        {
            string o = base.Send(s);
            return JsonConvert.DeserializeObject<LastFmApi.DataTypes.Album.RootObject>(o);
        }
    }
}

namespace LastFmApi
{

    public class Session
    {
        string _ApiKey;
        string _path;

        public Session(string ApiKey, string path)
        {
            _ApiKey = ApiKey;
            _path = path;
        }

        public string ApiKey
        {
            get
            {
                return _ApiKey;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public string Query(string querystring)
        {
            string ret = "";

            using (WebClient client = new WebClient())
            {
                try
                {
                    ret = client.DownloadString(this.Path + querystring + "&api_key=" + this.ApiKey + "&format=json");
                }
                catch (Exception e)
                {
                    //complain!
                }
            }

            ret = ret.Replace("#text", "text");
            ret = ret.Replace("@attr", "attr");

            Regex r = new Regex("\"toptags\":\"\\\\n.*\"");
            ret = r.Replace(ret, "");

            return ret;
        }

    }
}

