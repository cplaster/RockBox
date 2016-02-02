using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;

namespace RockBox
{

    public enum LastFmQueryType
    {
        None,
        ByAlbum,
        ByAlbumAndArtist,
        ByMusicBrainzId
    }

    public class LastFmQuery
    {
        string _album;
        string _artist;
        string _lastFmQueryAddress = RockBox.Properties.Resources.LastFmQueryAddress;
        string _lastFmApiKey = RockBox.Properties.Resources.LastFmApiKey;
        string _mbid;
        string _query;
        LastFmQueryType _queryType = LastFmQueryType.None;

        public string Artist
        {
            get { return _artist; }
        }

        public string Album
        {
            get { return _album; }
        }

        public string MusicBrainzId
        {
            get { return _mbid; }
        }

        public string Query
        {
            get { return _query; }
        }

        public LastFmQueryType QueryType
        {
            get { return _queryType; }
        }

        public LastFmQuery(string album, string artist = null)
        {
            _album = album;
            _artist = artist;

            if (_artist == null)
            {
                _queryType = LastFmQueryType.ByAlbum;
                _query = _lastFmQueryAddress + "?method=album.search&api_key=" + _lastFmApiKey + "&album=" + album;
            }
            else
            {
                _queryType = LastFmQueryType.ByAlbumAndArtist;
                _query = _lastFmQueryAddress + "?method=album.getinfo&api_key=" + _lastFmApiKey + "&artist=" + _artist + "&album=" + _album;
            }
        }

        public LastFmQuery(string mbid)
        {
            _mbid = mbid;
            _queryType = LastFmQueryType.ByMusicBrainzId;
            _query = _lastFmQueryAddress + "?method=track.getInfo&api_key=" + _lastFmApiKey + "&mbid=" + _mbid;
        }

    }


    static class ImageHelper
    {


        /// <summary>
        /// Downloads the file at the given uri to filename.
        /// This method does check for redirects and also for content type.
        /// Defaults to 'image' content type, will not download the file if the content type
        /// does not match! Will overwrite any file at the target location.
        /// </summary>
        /// <param name="uri">String representation of the uri of the file to download.</param>
        /// <param name="fileName">File location to save to.</param>
        /// <param name="contentType">Optional contentType filter. Defaults to 'image'.</param>
        public static void DownloadFile(string uri, string fileName, string contentType = "image")
        {
            Console.WriteLine("Downloading File: " + fileName + " | type=" + contentType + " | (" + uri + ")");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Check that the remote file was found. The ContentType
            // check is performed since a request for a non-existent
            // image file might be redirected to a 404-page, which would
            // yield the StatusCode "OK", even though the image was not
            // found.
            if ((response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Moved ||
                response.StatusCode == HttpStatusCode.Redirect) &&
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            {

                // if the remote file was found, download oit
                using (Stream inputStream = response.GetResponseStream())
                using (Stream outputStream = File.OpenWrite(fileName))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    do
                    {
                        bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, bytesRead);
                    } while (bytesRead != 0);

                    outputStream.Close();
                }
            }

            response.Dispose();
        }

        /// <summary>
        /// Sends a search request to LastFm
        /// </summary>
        /// <param name="query">Instance of LastFm query object.</param>
        /// <returns>XmlDocument representation of the query results.</returns>
        public static string GetLastFmSearchResults(LastFmQuery query)
        {
            Console.WriteLine("Getting LastFm Query: " + query.Query);

            string ret = "";

            using (WebClient client = new WebClient())
            {
                try
                {
                    ret = client.DownloadString(query.Query);
                }
                catch (Exception e)
                {
                    ret = "<xml></xml>";
                }
            }

            var xml = new XmlDocument();
            xml.LoadXml(ret);
            ret = "";

            var alb = xml["lfm"];
            if (alb != null)
            {
                foreach (XmlElement item in alb["album"])
                {
                    if (item.Name == "image")
                    {
                        XmlAttribute a = item.Attributes[0];
                        if (a.Name == "size" && a.Value == "large")
                        {
                            ret = item.InnerText;
                        }
                    }

                }
            }
            return ret;
        }

        public static byte[] GetDefaultImage()
        {
            byte[] image = null;
            Bitmap bm = RockBox.Properties.Resources.jewelcase_medium;
            using (MemoryStream ms = new MemoryStream())
            {
                bm.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                image = ms.ToArray();
            }

            return image;
        }

        public static byte[] DownloadAndSaveImage(Database d, string artist, string album)
        {
            byte[] image = null;
            string uri = ImageHelper.GetLastFmSearchResults(new LastFmQuery(album, artist));
            if (uri != null && uri != "")
            {
                System.Drawing.Bitmap b;
                ImageHelper.DownloadFile(uri, "temp.rkbxi");
                try
                {
                    b = new Bitmap("temp.rkbxi");
                    image = ImageHelper.ConvertBitmapToBytes(b);
                    b.Dispose();
                    d.Images.AddRow(artist, album, uri, image, "large", "large");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Was not able to download image for artist:[" + artist + "] and album: [" + album + "]");
                }
            }

            return image;
        }


        public static byte[] DownloadAndSaveImageOLD(Database d, string artist, string album)
        {
            byte[] image;
            //string uri = ImageHelper.GetAlbumArtFromLastFmByAlbumAndArtist(album, artist);
            string uri = ImageHelper.GetLastFmSearchResults(new LastFmQuery(album, artist));
            if (uri != null && uri != "")
            {
                System.Drawing.Bitmap b;
                ImageHelper.DownloadFile(uri, "temp.rkbxi");
                try
                {
                    b = new Bitmap("temp.rkbxi");
                    image = ImageHelper.ConvertBitmapToBytes(b);
                    b.Dispose();
                    d.Images.AddRow(artist, album, uri, image, "large", "large");
                    //ica.Insert(artist, album, uri, image, "large", "large");
                }
                catch (Exception e)
                {
                    image = ImageHelper.GetDefaultImage();
                    //MemoryStream ms = new MemoryStream(image);
                    //Bitmap bi = new Bitmap(ms, false);
                    //ms.Close();
                    //ms.Dispose();
                    d.Images.AddRow(artist, album, "", image, "large", "default.png");
                    //ica.Insert(artist, album, "", image, "large", "default.png");
                }
            }
            else
            {
                /*FIXME: A potential problem here is that if no image for the album is found, we insert default copy into the database, which we really don't need/want to do.
                 * it would be better to just return null here instead.
                image = ImageHelper.GetDefaultImage();
                MemoryStream ms = new MemoryStream(image);
                Bitmap bi = new Bitmap(ms, false);
                ica.Insert(artist, album, "", image, "large", "default.png");
                 */
                image = null;

            }
            return image;
        }

        public static bool ImagesCacheIsInDatabase(Database d, string artist, string album)
        {
            if (d.Images.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static byte[] GetFromDatabase(Database d, string artist, string album)
        {
            byte[] image = null;

            Database.ImageCollection ict = d.Images.GetDataByArtistAndAlbum(artist, album);
            if (ict.Count > 0)
            {
                Database.Image r = ict[0];
                image = r.ImageData;
            }

            return image;
        }



        /// <summary>
        /// Converts a byte array to a BitmapImage object.
        /// </summary>
        /// <param name="b">Byte array containing image data.</param>
        /// <returns>BitmapImage object instance.</returns>
        public static BitmapImage ConvertBytesToBitmapImage(byte[] b)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(b);
            bi.EndInit();
            return bi;
        }

        /// <summary>
        /// Converts a bitmap object to byte array
        /// </summary>
        /// <param name="b">Bitmap object to convert.</param>
        /// <returns>ByteArray representation of the image data.</returns>
        public static byte[] ConvertBitmapToBytes(System.Drawing.Bitmap b)
        {

            byte[] byteArray = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                b.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Close();

                byteArray = stream.ToArray();
            }
            return byteArray;
        }
    }
}

