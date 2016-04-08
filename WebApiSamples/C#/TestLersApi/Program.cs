using System;

namespace TestLersApi
{
	class Program
	{
		static void Main(string[] args)
		{
			// Создаем прокси класс для работы со службой
			LersApi.Api client = new LersApi.Api();

			// Устанавливаем куки-контейнер, необходим для авторизации пользователя
			client.CookieContainer = new System.Net.CookieContainer();

			// Входим в систему ЛЭРС УЧЕТ
			var response = client.Login("demo", "demo");

			// Если произошла ошибка, прекращаем работу
			if (response.ErrorCode != LersApi.ErrorCode.None)
			{
				Console.WriteLine(response.ErrorMessage);
				Console.ReadKey();
				return;
			}
			
			// Получаем список точек учета
			var response2 = client.GetMeasurePointList();

			// Если список пустой, выходим
			if (response2.MeasurePointList == null || response2.MeasurePointList.Length == 0)
			{
				Console.WriteLine("Список точек учета пуст");
				Console.ReadKey();
				return;
			}

			// Получаем список объектов учета
			var response4 = client.GetNodeListExtended(LersApi.NodeInfoFlags.Customer | LersApi.NodeInfoFlags.Systems | LersApi.NodeInfoFlags.Serviceman | LersApi.NodeInfoFlags.ServiceCompany);

			// Если список пустой, выходим
			if (response4.NodeList == null || response4.NodeList.Length == 0)
			{
				Console.WriteLine("Список объектов учета пуст");
				Console.ReadKey();
				return;
			}
			
			// Получаем потребление по первой точке учета из списка
			var endDate = DateTime.Today;

			var startDate = new DateTime(endDate.Year-1, endDate.Month, 1);

			var response3 = client.GetMeasurePointConsumption(response2.MeasurePointList[0].Id, startDate, endDate, LersApi.DeviceDataType.Day);

			// Если список пустой, выходим
			if (response3.Data == null || response3.Data.Length == 0)
			{
				Console.WriteLine("По точке учета нет данных потребления");
				Console.ReadKey();
				return;
			}

			if (response3.Data[0].ResourceKind == LersApi.ResourceKind.Water)
			{
				Console.WriteLine(response3.Data[0].DateTime + ": " + ((LersApi.WaterConsumptionRecord)response3.Data[0]).T_in);
			}
			else
				Console.WriteLine(response3.Data[0].DateTime);

			// Экспорт данных в формат XML 80020
			var response5 = client.ExportMeasurePointDataToXml80020(response2.MeasurePointList[0].Id, startDate, endDate);

			Console.ReadKey();
			return;
		}
	}
}
