using Lers.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExternalModuleExample
{
	/// <summary>
	/// Класс, реализующий интерфейс IPlugin 
	/// </summary>
	public class Plugin : IPlugin
	{
		/// <summary>
		/// Экземпляр хост-интерфейса клиента
		/// </summary>
		internal static IPluginHost Host { get; private set; }

		/// <summary>
		/// Список открытых окон
		/// </summary>
		private List<MainForm> visibleForms = new List<MainForm>();

		/// <summary>
		/// Метод, вызывающийся во время запуска клиента
		/// </summary>
		/// <param name="pluginHost"></param>
		public void Initialize(IPluginHost pluginHost)
		{
			// Копируем экземпляр хост-интерфейса клиента в нашу программу
			Host = pluginHost;

			// Ищем меню Сервис.
			foreach (var item in Host.MainWindow.MainMenu.Items)
			{
				if (item.ID == (int)Lers.UI.SystemMenuId.Service)
				{
					// Добавляем подпункт в пункт главного меню Сервис.
					item.AddItem("Пример внешнего модуля", Properties.Resources.Icon, true, OnItemClick);
				}
			}
		}

		/// <summary>
		/// Событие выбора приложения в меню Сервис
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnItemClick(object sender, EventArgs e)
		{
			// Проверим, открыто ли такое окно
			MainForm currentForm = GetOpenedForm();

			// Если такого окна нет, то открываем новое
			if (currentForm == null)
				NewForm();

			//Такое окно есть, переводим фокус на него
			else
			{
				currentForm.Show();
				currentForm.Focus();
			}
		}

		/// <summary>
		/// Возвращает открытую форму или null если формы нет
		/// </summary>
		/// <returns></returns>
		private MainForm GetOpenedForm()
		{
			lock (this.visibleForms)
			{
				foreach (MainForm form in this.visibleForms)
				{
					return form;
				}
			}

			return null;
		}

		/// <summary>
		/// Открываем новое окно
		/// </summary>
		private void NewForm()
		{
			// Создаём новый экземпляр формы
			MainForm mainForm = new MainForm() { Text = "Пример внешнего модуля" };

			// Инициализируем форму
			mainForm.Initialize(Host);

			// Добавляем форму в список окон в программе
			Host.MainWindow.AddPage(mainForm);

			// Открываем форму
			mainForm.Show();

			// Добавляем событие на закрытие формы
			mainForm.FormClosed += new System.Windows.Forms.FormClosedEventHandler(currentForm_FormClosed);

			lock (this.visibleForms)
			{
				this.visibleForms.Add(mainForm);
			}
		}

		/// <summary>
		/// Закрыто окно. Удаляем его из списка открытых окон.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void currentForm_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
		{
			lock (this.visibleForms)
			{
				this.visibleForms.Remove((MainForm)sender);
			}
		}
	}
}
