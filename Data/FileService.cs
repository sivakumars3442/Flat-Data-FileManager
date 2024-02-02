using Microsoft.Extensions.FileProviders;
using Syncfusion.Blazor.FileManager;

namespace TestSyncfusion.Data
{
	public class FileService
	{
		public FileManagerResponse<FileManagerDirectoryContent> Read(string path, List<FileManagerDirectoryContent> dataSource, FileManagerDirectoryContent[] fileDetails)
		{
			FileManagerResponse<FileManagerDirectoryContent> response = new FileManagerResponse<FileManagerDirectoryContent>();
			if (path == "/")
			{
				string ParentId = dataSource
					.Where(x => x.ParentId == null)
					.Select(x => x.Id).First();
				response.CWD = dataSource
					.Where(x => x.ParentId == null)
					.Select(x => new FileManagerDirectoryContent()
					{
						Id = x.Id,
						Name = x.Name,
						Size = x.Size,
						DateCreated = x.DateCreated,
						DateModified = x.DateModified,
						Type = Path.GetExtension(x.Name),
						HasChild = x.HasChild,
						ParentId = "",
						FilterPath = x.FilterPath,
						FilterId = x.FilterId,
						IsFile = x.IsFile,
					}).First();
				response.Files = dataSource
					.Where(x => x.ParentId == ParentId)
					.Select(x => new FileManagerDirectoryContent()
					{
						Id = x.Id,
						Name = x.Name,
						Size = x.Size,
						DateCreated = x.DateCreated,
						DateModified = x.DateModified,
						Type = Path.GetExtension(x.Name),
						ParentId = "0",
						HasChild = x.HasChild,
						FilterPath = x.FilterPath,
						FilterId = x.FilterId,
						IsFile = x.IsFile,
					}).ToList();
			}
			else
			{
				response.CWD = dataSource
					.Where(x => x.Id == fileDetails[0].Id)
					.Select(x => new FileManagerDirectoryContent()
					{
						Id = x.Id,
						Name = x.Name,
						Size = x.Size,
						DateCreated = x.DateCreated,
						DateModified = x.DateModified,
						Type = Path.GetExtension(x.Name),
						HasChild = x.HasChild,
						ParentId = x.ParentId,
						FilterPath = x.FilterPath,
						FilterId = x.FilterId,
						IsFile = x.IsFile,
					}).First();
				response.Files = dataSource
					.Where(x => x.ParentId == fileDetails[0].Id)
					.Select(x => new FileManagerDirectoryContent()
					{
						Id = x.Id,
						Name = x.Name,
						Size = x.Size,
						DateCreated = x.DateCreated,
						DateModified = x.DateModified,
						Type = x.Type,
						ParentId = fileDetails[0].Id,
						HasChild = x.HasChild,
						FilterPath = x.FilterPath,
						FilterId = x.FilterId,
						IsFile = x.IsFile,
					}).ToList();
			}
			return response;
		}

	}
}
