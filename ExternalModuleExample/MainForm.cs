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
					string measurePointType = "";

					// Смотрим тип точки учёта и в соответствии со значением присваиваем название
                    switch (measurePoint.SystemType)
                    {
                        case SystemType.ColdWater:
							measurePointType = "Холодное водоснабжение"; break;

                        case SystemType.Electricity:
							measurePointType = "Электричество"; break;

                        case SystemType.Gas:
							measurePointType = "Газоснабдение"; break;

                        case SystemType.Heat:
							measurePointType = "Теплоснабжение"; break;

                        case SystemType.HotWater:
							measurePointType = "Горячее водоснабжение"; break;

                        case SystemType.None:
							measurePointType = "Нет типа"; break;

                        case SystemType.Steam:
							measurePointType = "Пароснабжение"; break;

                        default:
							measurePointType = "Не удалось получить тип"; break;
                    }

					// Добавляем запись в таболицу MainView
					MainView.Rows.Add(measurePoint.FullTitle, measurePointType);
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
