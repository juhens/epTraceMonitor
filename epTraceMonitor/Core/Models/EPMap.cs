using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class EPMap
    {
        private readonly static ConcurrentDictionary<string, string[]> epsContent = new();
        public EPMap(string filePath, string fileName, string functionName, uint line)
        {
            FilePath = filePath;
            FileName = fileName;
            FunctionName = functionName;
            Line = line;

            fullPath = filePath + "\\" + fileName;
            if (!epsContent.ContainsKey(fullPath))
            {
                epsContent[fullPath] = File.ReadAllLines(fullPath, Encoding.Default);
            }
        }
        public readonly string FilePath;
        public readonly string FileName;
        public readonly string FunctionName;
        public readonly uint Line;
        private readonly string fullPath;
        private string? _content;
        public string Content
        {
            get
            {
                if (_content != null)
                    return _content;

                _content = epsContent[fullPath][Line - 1].Trim().Replace(Environment.NewLine, string.Empty).Split("\t")[0];
                if (_content.Length > 80)
                    _content = _content.AsSpan().Slice(0, 80).ToString();
                return _content;
            }
        }
    }
}
