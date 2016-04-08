using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web;
using Lers.Data;
using Lers.Core;
using Lers.Utils;

namespace Lers.Web.DataAdapter.KhvAdm
{
	/// <summary>
	/// Реализует обработчик запроса на выдачу данных в формате экспорта, утвержденном администрацией г. Хабаровск
	/// </summary>
	internal class Handler: IHttpHandler
	{
		/// <summary>
		/// Используется в качестве источника при протоколировании.
		/// </summary>
		private static readonly string CLASS_NAME = typeof(Handler).FullName;

		/// <summary>
		/// Начальная дата для экспорта данных
		/// Берется из параметра dateFrom
		/// </summary>
		private DateTime beginDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

		/// <summary>
		/// Конечная дата для экспорта данных
		/// Берется из параметра dateTo
		/// </summary>
		private DateTime endDate = DateTime.Now;

		/// <summary> Тип временных меток: 1 - часовые, 0 - суточные; -1 - и часовые и суточные </summary>
		private int dataPeriodType = 0;

		/// <summary>
		/// Экземпляр класса для подключения к серверу ЛЭРС УЧЕТ
		/// </summary>
		private LersServer server;

		/// <summary>
		/// Протоколирует в файл
		/// </summary>
		private Logger logger;

		/// <summary>
		/// Возвращает значение, позволяющее определить, может ли другой запрос использовать экземпляр класса IHttpHandler.
		/// </summary>
		bool IHttpHandler.IsReusable
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Получает информацию о запросе
		/// </summary>
		/// <returns>Информация о запросе</returns>
		private string GetRequestInfo()
		{
			if (HttpContext.Current == null || HttpContext.Current.Request == null)
				return "<Не удалось получить дополнительную информацию о запросе>";

			HttpRequest currentRequest = HttpContext.Current.Request;

			// Адрес, с которого пришёл запрос (может быть адрес прокси)
			string publicAddress = currentRequest.ServerVariables["REMOTE_ADDR"];

			// Локальный адрес компа, заполняется при перенаправлении запроса через прокси
			string localAddress = currentRequest.ServerVariables["HTTP_X_FORWARDED_FOR"];


			StringBuilder info = new StringBuilder();

			info.AppendFormat("IP-адрес запроса: {0}.\r\nИсходный IP-адрес запроса: {1}\r\n", publicAddress, localAddress);

			info.AppendFormat("Адрес web-страницы: {0}\r\n", currentRequest.Url);

			return info.ToString();
		}

		/// <summary>
		/// Обрабатывает веб запрос. Исходная точка в HttpHandler
		/// </summary>
		/// <param name="context">Контекст Http запроса</param>
		void IHttpHandler.ProcessRequest(HttpContext context)
		{
			InitLogger();

			try
			{
				string msg = "Получен запрос на экспорт данных в формате ДГК.\r\n" + GetRequestInfo();

				LogInfo(msg);
				
				Init();

				// Разбираем входные параметры
				ParseInputParams();

				// Экспортируем данные
				ExportData();

				if (context.Response.IsClientConnected)
					LogInfo("Данные успешно отправлены.");
			}
			catch (ConfigurationErrorsException ex)
			{
				LogError(ex.Message);
			}
			catch (Lers.Networking.RequestTimeoutException exc)
			{
				LogError("Вышло время ожидания на выполнение операции.\r\n");
			}
			catch (Lers.Networking.ServerConnectionException exc)
			{
				LogError("Ошибка при попытки установить соединение с сервером ЛЭРС УЧЕТ.\r\n");
			}
			catch (Lers.Networking.AuthorizationFailedException exc)
			{
				LogError("Ошибка при попытки установить соединение с сервером ЛЭРС УЧЕТ.\r\n");
			}
			catch (VersionMismatchException exc)
			{
				LogError("Ошибка при попытки установить соединение с сервером ЛЭРС УЧЕТ.\r\n");
			}
			catch(Exception exc)
			{
				string msg = "Ошибка экспорта данных в формате ДГК. ";
				LogError(msg);
				throw;
			}
			finally
			{
				this.logger.LogDebug("Закрываем подключение...", CLASS_NAME);

				if(server != null && server.IsConnected)
					server.Disconnect(2000);

				this.logger.Close();

				context.Response.End();
			}
		}

		#region Протоколирование

		/// <summary>
		/// Протоколирует сообщение об ошибке
		/// </summary>
		/// <param name="message">Текст сообщения</param>
		private void LogError(string message)
		{
			this.logger.LogError("Dgk:\t" + message, CLASS_NAME);
		}

		/// <summary>
		/// Протоколирует информационное сообщение
		/// </summary>
		/// <param name="message">Текст сообщения</param>
		private void LogInfo(string message)
		{
			this.logger.LogMessage("Dgk:\t" + message, CLASS_NAME);
		}

		/// <summary>
		/// Протоколирует отладочное сообщение
		/// </summary>
		/// <param name="message">Текст сообщения</param>
		private void LogDebug(string message)
		{
			this.logger.LogDebug("Dgk:\t" + message, CLASS_NAME);
		}

		/// <summary>
		/// Протоколирует предупреждающее сообщение
		/// </summary>
		/// <param name="message">Текст сообщения</param>
		private void LogWarning(string message)
		{
			this.logger.LogWarning("Dgk:\t" + message, CLASS_NAME);
		}

		/// <summary>
		/// Инициализирует логгер
		/// </summary>
		private void InitLogger()
		{
			// Считываем путь до журнала протокола
			string logFile = System.Configuration.ConfigurationManager.AppSettings["DataAdapterFileLogPath"] ?? "";

			if (logFile == "")
				logFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\LERS\Logs\Web\DataAdapter.log";

			// Если каталога нет, создаем его
			if (!Directory.Exists(Path.GetDirectoryName(logFile)))
				Directory.CreateDirectory(Path.GetDirectoryName(logFile));

			bool debug = false;

			// Считываем значение включено ли протоколирование отладочных сообщений
			string fileLogDebug = System.Configuration.ConfigurationManager.AppSettings["DataAdapterFileLogDebug"] ?? "";

			if (fileLogDebug == "1")
				debug = true;

			this.logger = Logger.Create(logFile);

			this.logger.LogDebugMessages = debug;
		}

		#endregion

		#region Инициализация и разбор входных параметров

		/// <summary>
		/// Инициализация
		/// </summary>
		private void Init()
		{
			LogDebug("Инициализация...");

			// Отключаем буферизацию
			HttpContext.Current.Response.BufferOutput = false;

			// ServerAddress
			string serverAddress = ConfigurationManager.AppSettings["ServerAddress"] ?? "";

			if (serverAddress == "")
				serverAddress = "localhost";

			// ServerPort
			int serverPort;

			if (!Int32.TryParse(ConfigurationManager.AppSettings["ServerPort"], out serverPort))
				serverPort = 10000;

			// Login
			string login = ConfigurationManager.AppSettings["AdmLogin"];

			if(string.IsNullOrEmpty(login))
				throw new ConfigurationErrorsException("В конфигурации приложения отсутствует параметр: AdmLogin");

			// Password
			string password = ConfigurationManager.AppSettings["AdmPassword"];

			if(string.IsNullOrEmpty(password))
				throw new ConfigurationErrorsException("В конфигурации приложения отсутствует параметр: AdmPassword");

			LogDebug("Инициализация завершена.");

			// Соединяемся

			LogDebug("Соединение с сервером ЛЭРС УЧЕТ...");

			this.server = new LersServer();

			this.server.VersionMismatch += server_VersionMismatch;

			System.Security.SecureString securePassword = Networking.SecureStringHelper.ConvertToSecureString(password);

			this.server.Connect(serverAddress, (ushort)serverPort, new Networking.BasicAuthenticationInfo(login, securePassword));

			LogDebug("Соединение установлено.");
		}

		/// <summary>
		/// Обработчик ошибки несовместимости версий сервера и фреймворка
		/// </summary>
		void server_VersionMismatch(object sender, VersionMismatchEventArgs e)
		{
#if DEBUG
			e.Ignore = true;
#endif
		}

		/// <summary>
		/// Разбор входных параметров
		/// </summary>
		private void ParseInputParams()
		{
			NameValueCollection paramCollection = HttpContext.Current.Request.Params;

			DateTime result;

			// Начальная дата
			if (TryParseDateParam(paramCollection, "dateFrom", out result))
				beginDate = result;

			// Конечная дата
			if (TryParseDateParam(paramCollection, "DateTo", out result))
				endDate = result;

			// Из регламента:
			// ДГК может в любое время выполнять запросы данных за произвольный период,
			// но на глубину не более чем два месяца, включая отчетный.

			// Комментарий Клауса:
			// Если запрашивается большой интервал, то подрезаем его с начала,
			// иначе получаем исключение "Слишком большой размер пакета ( > 20МБ )

			// Если запрашивается интервал больше 3 месяцев, подрезаем его с начала.
			if(((TimeSpan)endDate.Subtract(beginDate)).TotalDays > 60)
			{
				beginDate = endDate.Subtract(new TimeSpan(60, 0, 0, 0));

				LogWarning("Интервал выборки данных превышает 60 дней. Дата начала интервала заменена на " + beginDate);
			}

			//Если интервал не корректный то устанавливаем интервал по умолчанию
			if (beginDate > endDate)
			{
				beginDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

				endDate = DateTime.Now;

				LogWarning("Конечная дата периода превышает начальную. Параметры были установлены по умолчанию.");
			}

			// Тип данных
			if (!Int32.TryParse(paramCollection["PeriodType"], out dataPeriodType))
				dataPeriodType = -1;	// Суточные и часовые

			// По умолчанию выдаем и суточные и часовые данные
			if (dataPeriodType != -1 && dataPeriodType != 0 && dataPeriodType != 1)
				dataPeriodType = -1;

			LogDebug(String.Format("параметры запроса: beginDate = {0}, endDate = {1}, dataPeriodType = {2}",
				beginDate, endDate, dataPeriodType));
		}

		/// <summary>
		/// Пытается разобрать параметр дата задающую начало и конец периода
		/// </summary>
		/// <param name="paramCollection">Коллекция параметров, полученная из HttpContext.Request.Params</param>
		/// <param name="paramName">Имя параметра для разбора</param>
		/// <param name="value">Дата полученная из указанного параметра</param>
		/// <returns>Результат разбора: true - успех, false - провал.</returns>
		private bool TryParseDateParam(NameValueCollection paramCollection, string paramName, out DateTime result)
		{
			result = new DateTime();

			// Проверяем есть ли вообще параметры, есть ли необходимый нам параметр, не пустой ли он
			if (paramCollection.Count > 0 &&
				!String.IsNullOrEmpty(paramCollection[paramName]))
			{
				if (DateTime.TryParseExact(paramCollection[paramName], "yyyyMMddHHmm", null, System.Globalization.DateTimeStyles.AssumeLocal, out result))
					return true;

				if (DateTime.TryParseExact(paramCollection[paramName], "yyyyMMdd", null, System.Globalization.DateTimeStyles.AssumeLocal, out result))
					return true;
			}

			return false;
		}

		#endregion

		#region Выдача данных

		/// <summary>
		/// Экспортирует данные потребления указанного типа
		/// </summary>
		/// <param name="measurePoint">Точка учета</param>
		/// <param name="isDay">Тип данных: true - суточные, false - часовые</param>
		private void ExportConsumption(MeasurePoint measurePoint, bool isDay)
		{
			// TODO: Сократить количество запросов на сервер, одновременно запрашивая суточные и часовые данные

			// Получаем данные потребления по точке учета
			MeasurePointConsumptionRecordCollection consumption = measurePoint.Data.GetConsumption(beginDate, endDate, ((isDay) ? DeviceDataType.Day : DeviceDataType.Hour));

			// Измеряемый ресурс
			int systemTypeId = (int)measurePoint.SystemType;

			// Номер точки учета
			int number = measurePoint.Number ?? 0;

			string output = "";

			foreach (MeasurePointConsumptionRecordWater dataRecord in consumption)
			{
				if (dataRecord.M_in == null)
					continue;

				// Накапливаем данные в строке
				output = number + ";"
				   + ((isDay) ? dataRecord.DateTime.ToString("yyyy-MM-dd") : dataRecord.DateTime.ToString("yyyy-MM-dd HH:mm"))
				   + ";2;"
				   + systemTypeId + ";"
				   + (dataRecord.T_in ?? 0) + ";"
				   + (dataRecord.T_out == null ? "" : dataRecord.T_out.ToString()) + ";"
				   + (dataRecord.M_in ?? 0) + ";"
				   + (dataRecord.M_out == null ? "" : dataRecord.M_out.ToString()) + ";"
				   + (dataRecord.Q_in == null? "" : dataRecord.Q_in.ToString()) + ";"
				   + (dataRecord.Q_out == null ? "" : dataRecord.Q_out.ToString()) + ";"
				   + (dataRecord.P_in == null ? "" : dataRecord.P_in.ToString()) + ";"
				   + (dataRecord.P_out == null ? "" : dataRecord.P_out.ToString()) + ";"
				   + (dataRecord.WorkTime == null ? "" : dataRecord.WorkTime.ToString()) + ";"
				   + Convert.ToInt32(isDay) + ";\n";

				// Выдаем браузеру
				HttpContext.Current.Response.Write(output);
			}
		}

		/// <summary>
		/// Отправляет заголовок
		/// </summary>
		private void SendHeader()
		{
			HttpResponse response = HttpContext.Current.Response;

			// Определяем имя файла
			string fileName = "" + ((beginDate != DateTime.MinValue) ? beginDate.ToString("yyyyMMddHHmm") : "")
				+ "-" + ((endDate != DateTime.MinValue) ? endDate.ToString("yyyyMMddHHmm") : "")
				+ ((dataPeriodType > -1) ? "_" + dataPeriodType : "")
				+ ".csv";

			// Подготавливаем заголовок
			// Ставим в качестве ответа, файл в кодировке по умолчанию
			response.ContentEncoding = System.Text.Encoding.Default;
			response.ContentType = "application/x-download";
			response.AddHeader("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(fileName));

			// Выдаем браузеру заголовок данных потребления
			response.Write("MeasurePointID;DataDate;DataSource;ResourceTypeID;T_in;T_out;M_in;M_out;H_in;H_out;P_in;P_out;WorkTime;IsDay\n");
		}

		/// <summary>
		/// Экспортирует данные потребления
		/// </summary>
		private void ExportData()
		{
			LogDebug("Отправка данных для администрации г. Хабаровска в ответ на запрос...");

			SendHeader();

			MeasurePoint[] measurePoints = this.server.MeasurePoints.GetList(MeasurePointType.Regular);

			// Выдаем все суточные данные потребления
			if (dataPeriodType == 0 || dataPeriodType == -1)
			{
				foreach (MeasurePoint measurepoint in measurePoints)
				{
					// Выгружаем данные только по воде
					if (measurepoint.SystemType != SystemType.ColdWater && measurepoint.SystemType != SystemType.HotWater && measurepoint.SystemType != SystemType.Heat)
						continue;

					ExportConsumption(measurepoint, true);

					if (!HttpContext.Current.Response.IsClientConnected)
					{
						LogDebug("Клиент был отключен от веб-сервера.");
						return;
					}
				}
			}

			// Выдаем все часовые данные потребления
			if (dataPeriodType == 1 || dataPeriodType == -1)
			{
				foreach (MeasurePoint measurepoint in measurePoints)
				{
					// Выгружаем данные только по воде
					if (measurepoint.SystemType != SystemType.ColdWater && measurepoint.SystemType != SystemType.HotWater && measurepoint.SystemType != SystemType.Heat)
						continue;

					ExportConsumption(measurepoint, false);

					if (!HttpContext.Current.Response.IsClientConnected)
					{
						LogDebug("Клиент был отключен от веб-сервера.");
						return;
					}
				}
			}
		}

		#endregion
	}
}