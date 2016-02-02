using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RockBox
{
    public static class DirectoryHelper
    {
        public class SuperDirectoryCollection
        {
            List<SuperDirectory> _list;

            public SuperDirectoryCollection()
            {
                _list = new List<SuperDirectory>();
            }

            public List<SuperDirectory> Items
            {
                get
                {
                    return _list;
                }
            }

            public int FileCount
            {
                get
                {
                    int i = 0;
                    foreach (SuperDirectory s in _list)
                    {
                        i = i + SuperDirectory.FileCount(s);
                    }
                    return i;
                }
            }

            public int DirectoryCount
            {
                get
                {
                    int i = 0;
                    foreach (SuperDirectory s in _list)
                    {
                        i = i + SuperDirectory.DirectoryCount(s);
                    }
                    return i;
                }
            }

            public int SuperDirectoryCount
            {
                get
                {
                    return _list.Count;
                }
            }

            internal void ApplyExtensionFilter(string[] s)
            {
                foreach (SuperDirectory sd in _list)
                {
                    sd.ApplyExtensionFilter(s);
                }
            }
        }

        public class SuperDirectory
        {
            string _path;
            DirectoryInfo _di;
            Dictionary<string, SuperDirectory> _directories = new Dictionary<string, SuperDirectory>();
            List<string> _files = new List<string>();

            public SuperDirectory(string path)
            {
                _path = path;
                _di = new DirectoryInfo(_path);
                GetDirectories();
                GetFiles();
            }

            public static int FileCount(SuperDirectory s)
            {
                int i = s.Files.Count;
                foreach (KeyValuePair<string, SuperDirectory> k in s.Directories)
                {
                    i = i + FileCount(k.Value);
                }
                return i;
            }

            public static int DirectoryCount(SuperDirectory s)
            {
                int i = s.Directories.Count;
                foreach (KeyValuePair<string, SuperDirectory> k in s.Directories)
                {
                    i = i + DirectoryCount(k.Value);
                }
                return i;
            }

            public List<string> Files
            {
                get
                {
                    return _files;
                }
            }

            public Dictionary<string, SuperDirectory> Directories
            {
                get
                {
                    return _directories;
                }
            }

            private void GetFiles()
            {
                DirectoryInfo d = new DirectoryInfo(_path);
                FileInfo[] f = d.GetFiles();
                foreach (FileInfo file in f)
                {
                    _files.Add(file.Name);
                }
            }

            private void GetDirectories()
            {
                DirectoryInfo d = new DirectoryInfo(_path);
                DirectoryInfo[] di = d.GetDirectories();
                foreach (DirectoryInfo directory in di)
                {
                    _directories.Add(directory.Name, new SuperDirectory(directory.FullName));
                }
            }

            internal void ApplyExtensionFilter(string[] s)
            {
                List<string> remove = new List<string>();

                foreach (string file in _files)
                {
                    FileInfo fi = new FileInfo(_path + "/" + file);
                    bool keep = false;
                    foreach (string filter in s)
                    {
                        if (fi.Extension == filter)
                        {
                            keep = true;
                        }
                    }
                    if (!keep)
                    {
                        remove.Add(file);
                    }
                }

                foreach (string file in remove)
                {
                    _files.Remove(file);
                }



                foreach (KeyValuePair<string, SuperDirectory> k in _directories)
                {
                    k.Value.ApplyExtensionFilter(s);
                }

            }

            internal List<string> GetFileList()
            {
                List<string> filelist = new List<string>();
                foreach (string file in _files)
                {
                    filelist.Add(_path + "/" + file);
                }
                foreach (KeyValuePair<string, SuperDirectory> k in _directories)
                {
                    List<string> fl = k.Value.GetFileList();
                    foreach (string s in fl)
                    {
                        filelist.Add(s);
                    }
                }

                return filelist;
            }
        }

        internal static int AddFile(Database db, FileInfo f)
        {
            // this method is pretty prone to problems.
            // We try to read tag information from the audio file.
            // If there is any sort of problem with that, we will just
            // skip adding it to the database for now.

            TagLib.File file = null;
            TagLib.Tag tag = null;
            bool fail = false;

            try
            {
                file = TagLib.File.Create(f.FullName);
                tag = file.Tag;
            }
            catch (Exception e)
            {
                fail = true;
            }


            // the file can't be added if there are no tags
            if (!fail)
            {
                string title = tag.Title == null ? " " : tag.Title;
                string artist = " ";
                if (tag.AlbumArtists.Length > 0)
                {
                    artist = tag.AlbumArtists[0];
                }
                else
                {
                    if (tag.Artists.Length > 0)
                    {
                        artist = tag.Artists[0];
                    }
                }
                string album = tag.Album == null ? " " : tag.Album;
                string year = tag.Year.ToString() == null ? " " : tag.Year.ToString();
                string length = file.Properties.Duration == null ? "00:00:00" : file.Properties.Duration.ToString();
                string bitrate = file.Properties.AudioBitrate.ToString() == null ? " " : file.Properties.AudioBitrate.ToString();
                string genre = " ";
                if (tag.Genres.Length > 0)
                {
                    genre = tag.Genres[0];
                }
                string track = tag.Track.ToString();
                string comments = " ";

                char[] trimChars = " \r\n\t\0".ToCharArray();

                title.Trim(trimChars);
                artist.Trim(trimChars);
                album.Trim(trimChars);
                year.Trim(trimChars);
                length.Trim(trimChars);
                bitrate.Trim(trimChars);
                genre.Trim(trimChars);
                track.Trim(trimChars);
                comments.Trim(trimChars);

                string hour, min, sec;
                string[] temp = length.Split(":".ToCharArray());
                hour = temp[0];
                min = temp[1];
                sec = temp[2];
                string[] temp2 = sec.Split(".".ToCharArray());
                sec = temp2[0];
                length = hour + ":" + min + ":" + sec;

                return db.Songs.AddRow(f.Directory.FullName, f.Name, title, artist, album, year, length, bitrate, genre, track, comments);
            }
            else
            {
                return -1;
            }
        }


        internal static bool IsInLibrary(Database db, FileInfo f)
        {
            bool ret = false;
            Database.SongCollection sdt = db.Songs.GetDataByPathAndFileName(f.Directory.FullName, f.Name);

            if (sdt.Count > 0)
            {
                ret = true;
            }

            return ret;
        }

        internal static SuperDirectory ScanDirectory(string path)
        {
            SuperDirectory d = new SuperDirectory(path);

            return d;
        }

        internal static SuperDirectoryCollection ScanDirectories(string[] paths)
        {
            SuperDirectoryCollection coll = new SuperDirectoryCollection();

            foreach (string path in paths)
            {
                SuperDirectory s = new SuperDirectory(path);
                coll.Items.Add(s);
            }

            return coll;
        }

        internal static int AddCollection(SuperDirectoryCollection coll, Database db)
        {
            int i = 0;

            foreach (SuperDirectory s in coll.Items)
            {
                List<string> filelist = s.GetFileList();
                foreach (string file in filelist)
                {
                    FileInfo f = new FileInfo(file);
                    DirectoryInfo d = f.Directory;
                    if (!IsInLibrary(db, f))
                    {
                        AddFile(db, f);
                        i++;
                    }

                }
            }

            return i;
        }

    }
}
