using System.Collections.Generic;
using System.IO;

namespace DDD.Domain
{
	public class FileSystem : IFileSystem
	{
		public TextWriter CreateText(string path)
		{
			return File.CreateText(path);
		}

		public IEnumerable<string> GetFiles(string path, string searchPattern)
		{
			return Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
		}

		public TextReader OpenText(string fileName)
		{
			return File.OpenText(fileName);
		}
	}
}
