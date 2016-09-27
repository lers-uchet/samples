using Lers.Core;
using Lers.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lers.Utils;

namespace ExternalModuleExample
{
	/// <summary>
	/// Класс экранной формы
	/// </summary>
	public partial class MainForm : Form
	{
		/// <summary>
		/// Экземпляр хост-интерфейса клиента
		/// </summary>
		IPluginHost host;

		public MainForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Инициализация формы
		/// </summary>
		/// <param name="host"></param>
		internal void Initialize(IPluginHost host)
		{
			// Сохраняем хост-интерфейса клиента себе в программу
			this.host = host;

			// Получам список точек учёта от сервера
			MeasurePoint[] measurePointList = host.Server.MeasurePoints.GetList();

			// Если список точек учёта не пуст
			if (measurePointList != null)
			{
				// Проходим по всему списку
				foreach (MeasurePoint measurePoint in measurePointList)
				{					
					// Добавляем запись в таболицу MainView
					MainView.Rows.Add(measurePoint.FullTitle, EnumUtils.GetDescription(measurePoint.SystemType));
				}

				// Ставим авто-ширину столбцов в такой режим, что суммарная ширина всех столбцов 
				// в точности заполняет отображаемую область MainView
				MainView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			}

			else
				// Если список точек учёта пуст, выводим сообщение
				MessageBox.Show("Список точек учёта пуст",
					"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

	}
}
