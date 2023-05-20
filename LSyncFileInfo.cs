using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightSync
{
    public class LSyncFileInfo
    {
        public string FilePath;
        public string Name;
        public DateTime DateModified;
        public bool IsFile;
        public string Raw;

        public LSyncFileInfo(string raw) 
        {
            Raw = raw;
            var cols = raw.Split(new[] { " " }, 4, StringSplitOptions.RemoveEmptyEntries);

            DateModified = DateTime.Parse($"{cols[0]} {cols[1]}");
            IsFile = cols[2] == "A";
            Name = Path.GetFileName(cols[3]);
            FilePath = cols[3];
        }

        public override bool Equals(object obj)
        {
            if (obj is  LSyncFileInfo other)
            {
                return other.FilePath == FilePath && other.DateModified == DateModified;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hashCode = 300261098;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
            hashCode = hashCode * -1521134295 + DateModified.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return Raw;
        }
    }
}
