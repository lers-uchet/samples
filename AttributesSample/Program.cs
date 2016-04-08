using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using Lers.Core;
using Lers.Data;

namespace LersSample
{
	class Program
	{
		static void Main(string[] args)
		{
			// Создаем экземпляр объекта LersServer, через который осуществляется работа с сервером ЛЭРС УЧЕТ.

			Lers.LersServer server = new Lers.LersServer();
						
			// Создаем данные для авторизации в системе (логин и пароль).

			string login = "admin";
			SecureString password = Lers.Networking.SecureStringHelper.ConvertToSecureString("admin");

			Lers.Networking.BasicAuthenticationInfo authInfo = new Lers.Networking.BasicAuthenticationInfo(login, password);

			// Выполняем подключение к серверу, указывая имя компьютера и TCP-порт, а так же данные для авторизации.

			try
			{
				server.Connect("localhost", 10000, authInfo);
			}
			catch (Exception exc)
			{
				Console.WriteLine("Ошибка подключения к серверу.\r\n" + exc.Message);
				Console.Read();
				return;
			}

			Console.WriteLine("Версия сервера: " + server.Version.ToString());

			// Получаем объект учета по уникальному номеру, который задается в свойствах объекта учета.
			// Указываем через флаг, что нам нужны атрибуты и системы объекта учета.
			Node node = server.Nodes.GetByNumber(1232, NodeInfoFlags.Attributes | NodeInfoFlags.Systems);

			// Убеждаемся, что мы получили указатель на объект Node.
			// Если объект учета с таким номером не существует или он недоступна для текущего пользовтеля, то метод GetByNumber() вернет null.

			if (node != null)
			{
				// Выводим наименование Объекта учета.

				Console.WriteLine("Объект учета \'{0}\'", node.Title);

				MeasurePoint[] measurePoints = node.Systems.HotWater.MeasurePoints.ToArray();

				// Запрашиваем данные о суточном потреблении гвс за двое суток.

				MeasurePointConsumptionRecord[] consumption1 = measurePoints[0].Data.GetConsumption(DateTime.Today.AddDays(-1), DateTime.Today, Lers.Data.DeviceDataType.Day).ToArray();

				MeasurePointConsumptionRecord[] consumption2 = measurePoints[1].Data.GetConsumption(DateTime.Today.AddDays(-1), DateTime.Today, Lers.Data.DeviceDataType.Day).ToArray();

				string scheme = (string)node.Attributes["схема"];

				MeasurePointConsumptionRecord[] consumption = new MeasurePointConsumptionRecord[consumption1.Length];

				switch (scheme)
				{ 
					// Первая схема измерения
					case "1":

						// Здесь реализуем расчет по схеме измерения, например складываем потребления по двум точкам учета.

						for (int i = 0; i < consumption1.Length; i++)
						{
							MeasurePointConsumptionRecordWater record = new MeasurePointConsumptionRecordWater(consumption1[i].DateTime);

							record.M_in = ((MeasurePointConsumptionRecordWater)consumption1[i]).M_in + ((MeasurePointConsumptionRecordWater)consumption2[i]).M_in;
							record.M_out = ((MeasurePointConsumptionRecordWater)consumption1[i]).M_out + ((MeasurePointConsumptionRecordWater)consumption2[i]).M_out;

							consumption[i] = record;
						}

						break;

					// Вторая схема измерения
					case "2":

						// Здесь реализуем расчет по схеме измерения, например вычисляем разницу потребления по двум точкам учета.

						for (int i = 0; i < consumption1.Length; i++)
						{
							MeasurePointConsumptionRecordWater record = new MeasurePointConsumptionRecordWater(consumption1[i].DateTime);

							record.M_in = ((MeasurePointConsumptionRecordWater)consumption1[i]).M_in - ((MeasurePointConsumptionRecordWater)consumption2[i]).M_in;
							record.M_out = ((MeasurePointConsumptionRecordWater)consumption1[i]).M_out - ((MeasurePointConsumptionRecordWater)consumption2[i]).M_out;

							consumption[i] = record;
						}

						break;

					default:
						throw new Exception("Неподдерживаемый номер схемы измерения.");
				}

				// Выводим данные на экран.

				foreach (MeasurePointConsumptionRecordWater recordWater in consumption)
				{
					Console.WriteLine("{0}: M1 = {1}, M2 = {2}", recordWater.DateTime, recordWater.M_in, recordWater.M_out);
				}
			}
			else
			{
				Console.WriteLine("Объект учета не существует или недоступен для данной учетной записи.");
			}

			Console.ReadLine();

			// Отключаемся от сервера.
			// Указываем таймаут 2 секунды на нормальное завершение сеанса, после выхода которого, соединение будет разорвано.

			server.Disconnect(2000);
		}
	}
}
