using Syncfusion.Blazor.FileManager;

namespace TestSyncfusion.Data
{
	public class CloudServiceOperations
	{
		public FileManagerResponse<FileManagerDirectoryContent> Read(string path, List<FileManagerDirectoryContent> dataSource, FileManagerDirectoryContent[] fileDetails)
		{
			FileManagerResponse<FileManagerDirectoryContent> response = new FileManagerResponse<FileManagerDirectoryContent>();
			if (path == "/")
			{
				string ParentId = dataSource
					.Where(x => x.FilterPath == string.Empty)
					.Select(x => x.Id).First();
				response.CWD = dataSource
					.Where(x => x.FilterPath == string.Empty)
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
				var id = fileDetails.Length > 0 && fileDetails[0] != null ? fileDetails[0].Id : dataSource
					.Where(x => x.FilterPath == path)
					.Select(x => x.ParentId).First();
				response.CWD = dataSource
					.Where(x => x.Id == (fileDetails.Length > 0 && fileDetails[0] != null ? fileDetails[0].Id : id))
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
					.Where(x => x.ParentId == (fileDetails.Length > 0 && fileDetails[0] != null ? fileDetails[0].Id : id))
					.Select(x => new FileManagerDirectoryContent()
					{
						Id = x.Id,
						Name = x.Name,
						Size = x.Size,
						DateCreated = x.DateCreated,
						DateModified = x.DateModified,
						Type = x.Type,
						ParentId = (fileDetails.Length > 0 && fileDetails[0] != null ? fileDetails[0].Id : id),
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
