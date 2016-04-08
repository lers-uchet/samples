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

			string login = "Admin";
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

			// Получаем точку учета по уникальному номеру, который задается в свойствах точки учета.

			MeasurePoint measurePoint = server.MeasurePoints.GetByNumber(123);

			// Убеждаемся, что мы получили указатель на объект MeasurePoint.
			// Если точки учета с таким номером не существует или она недоступна для текущего пользовтеля, то метод GetByNumber() вернет null.

			if (measurePoint != null)
			{
				// Выводим полное наименование точки учета.

				Console.WriteLine("Точка учета \'{0}\'", measurePoint.FullTitle);

				if (measurePoint.SystemType != SystemType.Gas)
				{
					Console.WriteLine("Точка учета должна иметь систему Газонабжение.");
					Console.Read();
					return;
				}

				// Сохраняем суточные данные о потреблении по точке учета.
				// Чтобы сохранить данные по воде, используйте Lers.Data.WaterConsumptionRecord, по электроэнергии - Lers.Data.ElectricConsumptionRecord.

				MeasurePointConsumptionRecordCollection data = new MeasurePointConsumptionRecordCollection(DeviceDataType.Day);

				MeasurePointConsumptionRecordGas record = new MeasurePointConsumptionRecordGas(DateTime.Today.AddDays(-1)); // За вчерашние сутки
				record.V = 100; // Объем газа
				record.T = 20;	// Температура газа

				data.Add(record);

				MeasurePointConsumptionRecordGas record2 = new Lers.Data.MeasurePointConsumptionRecordGas(DateTime.Today); // За сегодняшние сутки
				record2.V = 80; // Объем газа
				record2.T = 15; // Температура газа

				data.Add(record2);

				// Устанавливаем флаг для перезаписи существующих данных и сохраняем данные.
				
				MeasurePointSetConsumptionOptions options = new MeasurePointSetConsumptionOptions();
				options.OverwriteExistingRecords = true;

				measurePoint.Data.SetConsumption(data, options);

				// Запрашиваем данные о суточном потреблении газа за двое суток.

				MeasurePointConsumptionRecordCollection consumption = measurePoint.Data.GetConsumption(DateTime.Today.AddDays(-1), DateTime.Today, Lers.Data.DeviceDataType.Day);

				// Выводим данные на экран.

				foreach (MeasurePointConsumptionRecordGas recordGas in consumption)
				{
					Console.WriteLine("{0}: V = {1}, T = {2}", recordGas.DateTime, recordGas.V, recordGas.T);
				}
			}
			else
			{
				Console.WriteLine("Точка учета не существует или недоступна для данной учетной записи.");
			}

			Console.ReadLine();

			// Отключаемся от сервера.
			// Указываем таймаут 2 секунды на нормальное завершение сеанса, после выхода которого, соединение будет разорвано.

			server.Disconnect(2000);
		}
	}
}
