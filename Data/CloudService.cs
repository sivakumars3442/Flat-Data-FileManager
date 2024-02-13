using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Syncfusion.Blazor.FileManager;
using Amazon;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace TestSyncfusion.Data
{
	public class CloudService
	{
		public static string bucketName = "syncfusion-filemanager";
		public List<FileManagerDirectoryContent>? FileData;
		public static IAmazonS3? client;
		public static ListObjectsResponse? response;
		public ListObjectsResponse? initialResponse;
		static ListObjectsResponse? childResponse;
		public string? RootName;
		private string rootName = string.Empty;
		private string accessMessage = string.Empty;
		AccessDetails AccessDetails = new AccessDetails();
		long sizeValue = 0;
		List<FileManagerDirectoryContent> s3ObjectFiles = new List<FileManagerDirectoryContent>();
		TransferUtility fileTransferUtility = new TransferUtility(client);
		public async Task GetData()
		{
			FileData = new List<FileManagerDirectoryContent>();
			await RegisterAmazonS3("bucketName", "awsAccessKeyId", "awsSecretAccessKey", "bucketRegion");
            await GetFile("/", false);
			List<string> SubFolders = initialResponse != null ? initialResponse.CommonPrefixes : new List<string>();
			for (int i = 0; i < SubFolders.Count; i++)
			{
				string commonPrefix = SubFolders[i];
				var index = commonPrefix.IndexOf('/');
				var path = commonPrefix.Substring(index);
				await GetFile(path, false);

				// Get the additional files and add them to the end of the list
				List<string> NestedFiles = initialResponse != null ? initialResponse.CommonPrefixes : new List<string>();
				SubFolders.AddRange(NestedFiles);
			}
		}
		public async Task RegisterAmazonS3(string name, string awsAccessKeyId, string awsSecretAccessKey, string region)
		{
			bucketName = name;
			RegionEndpoint bucketRegion = RegionEndpoint.GetBySystemName(region);
			client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, bucketRegion);
			await GetBucketList();
		}
		public async Task GetBucketList()
		{
			await ListingObjectsAsync("", "", false);
			RootName = response.S3Objects.Where(x => x.Key.Split(".").Length != 2).First().Key;
			RootName = RootName.Replace("../", "");
		}
		public static async Task ListingObjectsAsync(string delimiter, string prefix, bool childCheck)
		{
			try
			{
				ListObjectsRequest request = new ListObjectsRequest { BucketName = bucketName, Delimiter = delimiter, Prefix = prefix };
				if (childCheck)
					childResponse = await client.ListObjectsAsync(request);
				else
					response = await client.ListObjectsAsync(request);
			}
			catch (AmazonS3Exception amazonS3Exception) { throw amazonS3Exception; }
		}

		public async Task GetFile(string path, bool showHiddenItems)
		{
			List<FileManagerDirectoryContent> myList = new List<FileManagerDirectoryContent>();
			FileManagerDirectoryContent cwd = new FileManagerDirectoryContent();
			List<FileManagerDirectoryContent> files = new List<FileManagerDirectoryContent>();
			List<FileManagerDirectoryContent> filesS3 = new List<FileManagerDirectoryContent>();
			FileManagerResponse<FileManagerDirectoryContent> readResponse = new FileManagerResponse<FileManagerDirectoryContent>();
			await GetBucketList();
			try
			{
				if (path == "/") await ListingObjectsAsync("/", RootName, false); else await ListingObjectsAsync("/", this.RootName.Replace("/", "") + path, false);
				if (path == "/")
				{
					List<FileManagerDirectoryContent> s = new List<FileManagerDirectoryContent>(); // Use a list instead of an array
					foreach (var y in response.S3Objects.Where(x => x.Key == RootName)) // Use foreach loop to iterate over the filtered S3Objects
					{
						s.Add(await CreateDirectoryContentInstanceAsync(true, y.Key.ToString().Replace("/", ""), response.Prefix, false, "Folder", y.Size, y.LastModified, y.LastModified, checkChild(y.Key), string.Empty)); // Add the result of CreateDirectoryContentInstance to the list
					}
					myList.AddRange(s);
				}
			}
			catch (Exception ex) { throw ex; }
			try
			{
				if (response.CommonPrefixes.Count > 0)
				{
					foreach (var prefix in response.CommonPrefixes)
					{
						var file = await CreateDirectoryContentInstanceAsync(false, getFileName(prefix, path), prefix, false, "Folder", 0, DateTime.Now, DateTime.Now, checkChild(prefix), getFilePath(prefix));
						files.Add(file);
					}
					myList.AddRange(files);
				}

			}
			catch (Exception ex) { throw ex; }
			try
			{
				if (path == "/") await ListingObjectsAsync("/", RootName, false); else await ListingObjectsAsync("/", this.RootName.Replace("/", "") + path, false);
				if (response.S3Objects.Count > 0)
				{
					foreach (var obj in response.S3Objects.Where(x => x.Key != RootName.Replace("/", "") + path))
					{
						var file = await CreateDirectoryContentInstanceAsync(false, obj.Key.ToString().Replace(RootName.Replace("/", "") + path, "").Replace("/", ""), obj.Key, true, Path.GetExtension(obj.Key.ToString()), obj.Size, obj.LastModified, obj.LastModified, checkChild(obj.Key), getFilterPath(obj.Key, path));
						filesS3.Add(file);
					}
					myList.AddRange(filesS3);
				}

			}
			catch (Exception ex) { throw ex; }
			if (filesS3.Count != 0) files = files.Union(filesS3).ToList();
			readResponse.CWD = cwd;
			try
			{
				if ((cwd.Permission != null && !cwd.Permission.Read))
				{
					readResponse.Files = null;
					accessMessage = cwd.Permission.Message;
					throw new UnauthorizedAccessException("'" + cwd.Name + "' is not accessible. You need permission to perform the read action.");
				}
			}
			catch (Exception e)
			{
				ErrorDetails er = new ErrorDetails();
				er.Message = e.Message.ToString();
				er.Code = er.Message.Contains("is not accessible. You need permission") ? "401" : "417";
				if ((er.Code == "401") && !string.IsNullOrEmpty(accessMessage)) { er.Message = accessMessage; }
				readResponse.Error = er;
			}
			initialResponse = response;
			readResponse.Files = files;
			FileData.AddRange(myList);
		}

		private async Task<FileManagerDirectoryContent> CreateDirectoryContentInstanceAsync(bool isRoot, string name, string id, bool value, string type, long size, DateTime createddate, DateTime modifieddate, Task<bool> child, string filterpath) // Change bool to Task<bool>
		{
			FileManagerDirectoryContent tempFile = new FileManagerDirectoryContent();
			tempFile.Name = name;
			tempFile.IsFile = value;
			tempFile.Type = type;
			tempFile.Size = size;
			tempFile.Id = id;
			tempFile.ParentId = isRoot ? null : response.Prefix;
			tempFile.DateCreated = createddate;
			tempFile.DateModified = modifieddate;
			tempFile.HasChild = await child; // Use await keyword to get the boolean value from the task
			tempFile.FilterPath = filterpath;
			tempFile.FilterId = filterpath;
			tempFile.Permission = GetPathPermission(filterpath + (value ? name : Path.GetFileNameWithoutExtension(name)), value);
			return await Task.FromResult(tempFile); // Use Task.FromResult to return a completed task with a value
		}


		public async Task<bool> checkChild(string path) // Add async and Task<bool>
		{
			try { await ListingObjectsAsync("/", path, true); } catch (AmazonS3Exception) { throw; }
			return childResponse?.CommonPrefixes.Count > 0 ? true : false;
		}


		private string getFileName(string fileName, string path)
		{
			return fileName.Replace(RootName.Replace("/", "") + path, "").Replace("/", "");
		}
		private string getFilePath(string pathString)
		{
			return pathString.Substring(0, pathString.Length - pathString.Split("/")[pathString.Split("/").Length - 2].Length - 1).Substring(RootName.Length - 1);
		}

		public string getFilterPath(string fullPath, string path)
		{
			string name = fullPath.ToString().Replace(RootName.Replace("/", "") + path, "").Replace("/", "");
			int nameIndex = fullPath.LastIndexOf(name);
			fullPath = fullPath.Substring(0, nameIndex);
			int rootIndex = fullPath.IndexOf(RootName.Substring(0, RootName.Length - 1));
			fullPath = fullPath.Substring(rootIndex + RootName.Length - 1);
			return fullPath;
		}

		protected virtual string[] GetFolderDetails(string path)
		{
			string[] str_array = path.Split('/'), fileDetails = new string[2];
			string parentPath = "";
			for (int i = 0; i < str_array.Length - 1; i++)
			{
				parentPath += str_array[i] + "/";
			}
			fileDetails[0] = parentPath;
			fileDetails[1] = str_array[str_array.Length - 1];
			return fileDetails;
		}

		protected virtual AccessPermission GetPathPermission(string path, bool isFile)
		{
			string[] fileDetails = GetFolderDetails(path);
			if (isFile)
			{
				return GetPermission(fileDetails[0].TrimStart('/') + fileDetails[1], fileDetails[1], true);
			}
			return GetPermission(fileDetails[0].TrimStart('/') + fileDetails[1], fileDetails[1], false);

		}
		protected virtual AccessPermission GetPermission(string location, string name, bool isFile)
		{
			AccessPermission permission = new AccessPermission();
			if (!isFile)
			{
				if (this.AccessDetails.AccessRules == null) { return null; }
				foreach (AccessRule folderRule in AccessDetails.AccessRules)
				{
					if (folderRule.Path != null && folderRule.IsFile == false && (folderRule.Role == null || folderRule.Role == AccessDetails.Role))
					{
						if (folderRule.Path.IndexOf("*") > -1)
						{
							string parentPath = folderRule.Path.Substring(0, folderRule.Path.IndexOf("*"));
							if ((location).IndexOf((parentPath)) == 0 || parentPath == "")
							{
								permission = UpdateFolderRules(permission, folderRule);
							}
						}
						else if ((folderRule.Path) == (location) || (folderRule.Path) == (location + Path.DirectorySeparatorChar) || (folderRule.Path) == (location + "/"))
						{
							permission = UpdateFolderRules(permission, folderRule);
						}
						else if ((location).IndexOf((folderRule.Path)) == 0)
						{
							permission = UpdateFolderRules(permission, folderRule);
						}
					}
				}
				return permission;
			}
			else
			{
				if (this.AccessDetails.AccessRules == null) return null;
				string nameExtension = Path.GetExtension(name).ToLower();
				string fileName = Path.GetFileNameWithoutExtension(name);
				//string currentPath = GetPath(location);
				string currentPath = (location + "/");
				foreach (AccessRule fileRule in AccessDetails.AccessRules)
				{
					if (!string.IsNullOrEmpty(fileRule.Path) && fileRule.IsFile && (fileRule.Role == null || fileRule.Role == AccessDetails.Role))
					{
						if (fileRule.Path.IndexOf("*.*") > -1)
						{
							string parentPath = fileRule.Path.Substring(0, fileRule.Path.IndexOf("*.*"));
							if (currentPath.IndexOf((parentPath)) == 0 || parentPath == "")
							{
								permission = UpdateFileRules(permission, fileRule);
							}
						}
						else if (fileRule.Path.IndexOf("*.") > -1)
						{
							string pathExtension = Path.GetExtension(fileRule.Path).ToLower();
							string parentPath = fileRule.Path.Substring(0, fileRule.Path.IndexOf("*."));
							if (((parentPath) == currentPath || parentPath == "") && nameExtension == pathExtension)
							{
								permission = UpdateFileRules(permission, fileRule);
							}
						}
						else if (fileRule.Path.IndexOf(".*") > -1)
						{
							string pathName = Path.GetFileNameWithoutExtension(fileRule.Path);
							string parentPath = fileRule.Path.Substring(0, fileRule.Path.IndexOf(pathName + ".*"));
							if (((parentPath) == currentPath || parentPath == "") && fileName == pathName)
							{
								permission = UpdateFileRules(permission, fileRule);
							}
						}
						else if ((fileRule.Path) == (Path.GetFileNameWithoutExtension(location)) || fileRule.Path == location || (fileRule.Path + nameExtension == location))
						{
							permission = UpdateFileRules(permission, fileRule);
						}
					}
				}
				return permission;
			}

		}
		protected virtual bool HasPermission(Permission rule)
		{
			return rule == Permission.Allow ? true : false;
		}
		protected virtual AccessPermission UpdateFolderRules(AccessPermission folderPermission, AccessRule folderRule)
		{
			folderPermission.Copy = HasPermission(folderRule.Copy);
			folderPermission.Download = HasPermission(folderRule.Download);
			folderPermission.Write = HasPermission(folderRule.Write);
			folderPermission.WriteContents = HasPermission(folderRule.WriteContents);
			folderPermission.Read = HasPermission(folderRule.Read);
			folderPermission.Upload = HasPermission(folderRule.Upload);
			folderPermission.Message = string.IsNullOrEmpty(folderRule.Message) ? string.Empty : folderRule.Message;
			return folderPermission;
		}
		protected virtual AccessPermission UpdateFileRules(AccessPermission filePermission, AccessRule fileRule)
		{
			filePermission.Copy = HasPermission(fileRule.Copy);
			filePermission.Download = HasPermission(fileRule.Download);
			filePermission.Write = HasPermission(fileRule.Write);
			filePermission.Read = HasPermission(fileRule.Read);
			filePermission.Message = string.IsNullOrEmpty(fileRule.Message) ? string.Empty : fileRule.Message;
			return filePermission;
		}
		// Download file(s) or folder(s)
		public virtual FileStreamResult Download(string path, string[] Names, params FileManagerDirectoryContent[] data)
		{
			return DownloadAsync(path, Names, data).GetAwaiter().GetResult();
		}

		public virtual async Task<FileStreamResult> DownloadAsync(string path, string[] names, params FileManagerDirectoryContent[] data)
		{
			GetBucketList().GetAwaiter();
			FileStreamResult fileStreamResult = null;

			if (names.Length == 1)
			{
				GetBucketList().GetAwaiter();
				await ListingObjectsAsync("/", RootName.Replace("/", "") + path + names[0], false);
			}

			if (names.Length == 1 && response.CommonPrefixes.Count == 0)
			{
				try
				{
					AccessPermission pathPermission = GetPathPermission(path + names[0], true);
					if (pathPermission != null && (!pathPermission.Read || !pathPermission.Download))
					{
						throw new UnauthorizedAccessException("'" + names[0] + "' is not accessible. Access is denied.");
					}

					GetBucketList().GetAwaiter();
					await ListingObjectsAsync("/", RootName.Replace("/", "") + path, false);

					Stream stream = await fileTransferUtility.OpenStreamAsync(bucketName, RootName.Replace("/", "") + path + names[0]);

					fileStreamResult = new FileStreamResult(stream, "APPLICATION/octet-stream");
					fileStreamResult.FileDownloadName = names[0].Contains("/") ? names[0].Split("/").Last() : names[0];

					return fileStreamResult;
				}
				catch (AmazonS3Exception amazonS3Exception)
				{
					throw amazonS3Exception;
				}
			}
			else
			{
				try
				{
					var memoryStream = new MemoryStream();

					using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
					{
						foreach (string folderName in names)
						{
							AccessPermission pathPermission = GetPathPermission(path + folderName, Path.GetExtension(folderName) == "" ? false : true);
							if (pathPermission != null && (!pathPermission.Read || !pathPermission.Download))
							{
								throw new UnauthorizedAccessException("'" + folderName + "' is not accessible. Access is denied.");
							}

							var initialResponse = await GetRecursiveResponse("/", RootName.Replace("/", "") + path + folderName, false);
							await DownloadSubdirectories(archive, folderName, path + folderName, RootName.Replace("/", "") + path + folderName, initialResponse);
						}
					}

					memoryStream.Seek(0, SeekOrigin.Begin);

					fileStreamResult = new FileStreamResult(memoryStream, "APPLICATION/octet-stream");
					fileStreamResult.FileDownloadName = "Files.zip";

					return fileStreamResult;
				}
				catch (AmazonS3Exception amazonS3Exception)
				{
					throw amazonS3Exception;
				}
			}
		}

		private async Task DownloadSubdirectories(ZipArchive archive, string folderName, string folderPath, string s3FolderPath, ListObjectsResponse response)
		{
			foreach (var item in response.S3Objects)
			{
				string filePath = item.Key.Substring(item.Key.IndexOf(folderName));
				string s3FilePath = s3FolderPath;

				Stream fileStream = await fileTransferUtility.OpenStreamAsync(bucketName, s3FilePath);
				var entry = archive.CreateEntry(filePath, CompressionLevel.Optimal);

				using (var entryStream = entry.Open())
				{
					await fileStream.CopyToAsync(entryStream);
				}
			}
			foreach (var subdirectory in response.CommonPrefixes)
			{
				string subdirectoryName = subdirectory.Replace(s3FolderPath, "");
				var subdirectoryResponse = await GetRecursiveResponse("/", s3FolderPath + subdirectoryName, false);
				await DownloadSubdirectories(archive, folderName, folderName + subdirectoryName, s3FolderPath + subdirectoryName, subdirectoryResponse);
			}
		}

		public static async Task<ListObjectsResponse> GetRecursiveResponse(string delimiter, string prefix, bool childCheck)
		{
			try
			{
				ListObjectsRequest request = new ListObjectsRequest { BucketName = bucketName, Delimiter = delimiter, Prefix = prefix };
				return await client.ListObjectsAsync(request);
			}
			catch (AmazonS3Exception amazonS3Exception) { throw amazonS3Exception; }
		}
		public virtual FileStreamResult GetImage(string path, string id, bool allowCompress, params FileManagerDirectoryContent[] data)
		{
			try
			{
				AccessPermission PathPermission = GetPathPermission(path, false);
				if (PathPermission != null && !PathPermission.Read)
				{
					return null;
				}
				GetBucketList().GetAwaiter();
				ListingObjectsAsync("/", RootName.Replace("/", "") + path, false).Wait();
				string fileName = path.ToString().Split("/").Last();
				fileName = fileName.Replace("../", "");
				Stream stream = fileTransferUtility.OpenStream(bucketName, RootName.Replace("/", "") + path);
				return new FileStreamResult(stream, "APPLICATION/octet-stream");
			}
			catch (Exception ex) { throw ex; }
		}
	}
}
