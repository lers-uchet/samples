using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Lers.Utils
{
	/// <summary>
	/// Протоколирует сообщения в файл.
	/// </summary>
	public class Logger : IDisposable
	{
		/// <summary>
		/// Тип сообщения в журнале.
		/// </summary>
		private enum LogType
		{
			/// <summary>
			/// Отладочное сообщение.
			/// </summary>
			Debug = 0,

			/// <summary>
			/// Информационное сообщение.
			/// </summary>
			Info = 1,

			/// <summary>
			/// Предупреждение.
			/// </summary>
			Warning = 2,

			/// <summary>
			/// Ошибка.
			/// </summary>
			Error = 3,
		}

		private Stream stream;

		private StreamWriter streamWriter;

		/// <summary>
		/// Определяет, нужно ли протоколировать отладочные сообщения.
		/// </summary>
		public bool LogDebugMessages { get; set; }

		/// <summary>
		/// Определяет, нужно ли протоколировать идентификатор потока
		/// </summary>
		public bool LogThreadId { get; set; }

		/// <summary>
		/// Инициализирует новый экземпляр класса.
		/// </summary>
		/// <param name="stream">Поток записи.</param>
		private Logger(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
						
			this.stream = stream;
			this.streamWriter = new StreamWriter(stream, Encoding.UTF8);

			WritePreamble();
		}
				
		/// <summary>
		/// Создает новый экземпляр, протоколирующий сообщения в указанный файл.
		/// </summary>
		/// <param name="fileName">Имя файла журнала.</param>
		/// <returns>Возвращает экземпляр <see cref="Logger"/>.</returns>
		public static Logger Create(string fileName)
		{
			// Если папка не существует, то создаем её.

			string logDirectory = Path.GetDirectoryName(fileName);

			if (!Directory.Exists(logDirectory))
			{
				Directory.CreateDirectory(logDirectory);
			}

			// Открываем файловый поток на запись. Если файл уже существует, то будем дописывать в него,
			// если нет - создаем новый файл.

			FileMode fileMode;

			if (File.Exists(fileName))
				fileMode = FileMode.Append;
			else
				fileMode = FileMode.CreateNew;

			FileStream stream = new FileStream(fileName, fileMode, FileAccess.Write, FileShare.Read);

			// Создаем новый экземпляр класса и возвращаем его.

			try
			{
				return new Logger(stream);
			}
			catch
			{
				// При ошибке закрываем файловый поток.

				stream.Close();

				throw;
			}
		}

		/// <summary>
		/// Закрывает файл журнала.
		/// </summary>
		public void Close()
		{
			Dispose();
		}
		
		/// <summary>
		/// Протоколирует информационное сообщение.
		/// </summary>
		/// <param name="message">Текст сообщения.</param>
		/// <param name="source">Источник сообщения.</param>
		public void LogMessage(string message, string source)
		{
			Log(LogType.Info, message, source);
		}

		/// <summary>
		/// Протоколирует предупреждающее сообщение.
		/// </summary>
		/// <param name="message">Текст сообщения.</param>
		/// <param name="source">Источник сообщения.</param>
		public void LogWarning(string message, string source)
		{
			Log(LogType.Warning, message, source);

		}

		/// <summary>
		/// Протоколирует сообщение об ошибке.
		/// </summary>
		/// <param name="message">Текст сообщения.</param>
		/// <param name="source">Источник сообщения.</param>
		public void LogError(string message, string source)
		{
			Log(LogType.Error, message, source);
		}

		/// <summary>
		/// Протоколирует отладочное.
		/// </summary>
		/// <param name="message">Текст сообщения.</param>
		/// <param name="source">Источник сообщения.</param>
		public void LogDebug(string message, string source)
		{
			// Отладочные сообщения протоколируем только если включено.

			if (this.LogDebugMessages)
				Log(LogType.Debug, message, source);
		}

		/// <summary>
		/// Протоколирует сообщение в журнал.
		/// </summary>
		/// <param name="logType">Важность сообщения.</param>
		/// <param name="message">Текст сообщения.</param>
		/// <param name="source">Источник сообщения.</param>
		private void Log(LogType logType, string message, string source)
		{
			CheckDisposed();

			if (message == null)
				throw new ArgumentNullException("message");

			if (source == null)
				throw new ArgumentNullException("source");

			// Сонхронизируем доступ из разных потоков.

			lock (this.streamWriter)
			{
				// Записываем строку и очищаем буфер, чтобы строка сразу появилась в лог-файле.

				if(this.LogThreadId)
					this.streamWriter.WriteLine("{0:dd-MM-yyyy HH:mm:ss.fff}\t{1}:{2:000}\t{3}\t\t\t{4}",
						DateTime.Now, FormatLogType(logType), Thread.CurrentThread.ManagedThreadId, message, source);
				else
					this.streamWriter.WriteLine("{0:dd-MM-yyyy HH:mm:ss.fff}\t{1}\t{2}\t\t\t{3}", DateTime.Now, FormatLogType(logType), message, source);

				this.streamWriter.Flush();
			}

			// В отладочной версии дополнительно протоколируем в окно Output отладчика.

#if DEBUG
			System.Diagnostics.Debugger.Log((int)logType, source, message + "\r\n");
#endif
		}

		/// <summary>
		/// Записывает заголовок в файл журнала.
		/// </summary>
		private void WritePreamble()
		{
			this.streamWriter.WriteLine("========================================");
			this.streamWriter.WriteLine("== Журнал открыт " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff"));
			this.streamWriter.WriteLine("========================================");

			this.streamWriter.Flush();
		}

		private static string FormatLogType(LogType logType)
		{
			switch (logType)
			{
				case LogType.Debug:
					return "D";
				
				case LogType.Info:
					return "I";
				
				case LogType.Warning:
					return "W";
				
				case LogType.Error:
					return "E";

				default:
					throw new ArgumentOutOfRangeException("logType");
			}
		}

		#region IDisposable
		
		private bool isDisposed = false;

		/// <summary>
		/// Проверяет, что экземпляр не уничтожен.
		/// </summary>
		private void CheckDisposed()
		{
			if (this.isDisposed)
				throw new ObjectDisposedException("Использование экземпляра невозможно, т.к. объект был закрыт.");
		}
				
		/// <summary>
		/// Освобождает ресурсы.
		/// </summary>
		public void Dispose()
		{
			if (!this.isDisposed)
			{
				this.streamWriter.Close();
				this.stream.Close();

				this.streamWriter = null;
				this.stream = null;

				this.isDisposed = true;
			}
		}

		#endregion
	}
}
