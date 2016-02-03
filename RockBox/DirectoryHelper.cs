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

        //WARNFIX
        /*
        internal static SuperDirectory ScanDirectory(string path)
        {
            SuperDirectory d = new SuperDirectory(path);

            return d;
        }
        */

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

        //WARNFIX
        /*
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
                    if (!db.Songs.Contains(f))
                    {
                        //AddFile(db, f);
                        db.Songs.AddFile(f);
                        i++;
                    }

                }
            }

            return i;
        }
        */

    }
}
