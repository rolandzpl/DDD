using System.Collections.Generic;
using System.IO;

namespace DDD.Domain
{
	public interface IFileSystem
	{
		TextWriter CreateText(string path);

		IEnumerable<string> GetFiles(string path, string searchPattern);

		TextReader OpenText(string fileName);
	}
}