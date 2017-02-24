using System;
using System.IO;
using System.Net;
using Lers;
using Lers.Core;
using Newtonsoft.Json;

namespace SetNodeCoorinates
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 5)
			{
				ShowUsage();
				return;
			}

			try
			{
				string serverAddress = args[0];
				var serverPort = ushort.Parse(args[1]);
				string login = args[2];
				string password = args[3];
				string city = args[4];

				var server = new LersServer();
				server.VersionMismatch += (s, e) => e.Ignore = true;

				var auth = new Lers.Networking.BasicAuthenticationInfo(login, Lers.Networking.SecureStringHelper.ConvertToSecureString(password));
				server.Connect(serverAddress, serverPort, auth);

				var nodes = server.Nodes.GetList();

				for (int i = 0; i < nodes.Length; ++i)
				{
					var node = nodes[i];

					string searchString = $"{city} {node.Address}";

					var loc = GetCoordinates(searchString);

					if (loc != null)
					{
						node.GeoLocation = loc;
						node.Save();
					}

					Console.WriteLine($"Обработано {++i} из {nodes.Length}");
				}
			}
			catch (Exception exc)
			{
				Console.WriteLine($"Ошибка установки координат объектов учёта. {exc.Message}");
			}
		}

		private static void ShowUsage()
		{
			Console.WriteLine("Использование:");
			Console.WriteLine("SetNodeCoordinates lersServerAddress lersServerPort login password cityName");
			Console.WriteLine("lersServerAddress: адрес сервера ЛЭРС УЧЁТ");
			Console.WriteLine("lersServerPort: порт сервера ЛЭРС УЧЁТ");
			Console.WriteLine("login, password: логин и пароль на сервере ЛЭРС УЧЁТ. Учётная запись должна иметь право редактирования объектов учёта");
			Console.WriteLine("cityName: город, в котором расположены объекты учёта");
		}

		private static GeoLocation GetCoordinates(string address)
		{
			var request = WebRequest.Create(CreateSearchUrl(address));

			var webRequest = (HttpWebRequest)request;

			webRequest.UserAgent = "Lers Client";

			using (var response = request.GetResponse())
			{
				using (var stream = response.GetResponseStream())
				{
					var sr = new StreamReader(stream);

					string jsonResponse = sr.ReadToEnd();

					dynamic[] responseObject = JsonConvert.DeserializeObject<dynamic[]>(jsonResponse);

					if (responseObject.Length == 0)
					{
						return null;
					}
					else
					{
						dynamic firstObject = responseObject[0];

						var loc = new GeoLocation();

						loc.Latitude = firstObject.lat;
						loc.Longitude = firstObject.lon;

						return loc;
					}
				}
			}
		}

		private static string CreateSearchUrl(string whatToSearch)
		{
			string search = whatToSearch.Replace(' ', '+');

			return $"http://nominatim.openstreetmap.org/search?q={search}&format=json";
		}
	}
}
