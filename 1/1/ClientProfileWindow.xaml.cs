using System;
using System.Windows;
using Npgsql;

namespace Прокат_авто
{
	public partial class ClientProfileWindow : Window
	{
		private int clientId;
		private string connectionString;

		public ClientProfileWindow(int clientId, string connectionString)
		{
			InitializeComponent();
			this.clientId = clientId;
			this.connectionString = connectionString;

			// По умолчанию показываем каталог
			ShowCatalog();
		}

		private void btnCatalog_Click(object sender, RoutedEventArgs e)
		{
			ShowCatalog();
		}

		private void btnProfile_Click(object sender, RoutedEventArgs e)
		{
			var profilePage = new ClientProfilePage(clientId, connectionString);
			MainFrame.Navigate(profilePage);
		}

		private void btnContracts_Click(object sender, RoutedEventArgs e)
		{
			var contractsPage = new ClientContractsPage(clientId, connectionString);
			MainFrame.Navigate(contractsPage);
		}

		private void btnInfo_Click(object sender, RoutedEventArgs e)
		{
			var infoPage = new ClientInfoPage(connectionString);
			MainFrame.Navigate(infoPage);
		}

		private void btnLogout_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
				MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes)
			{
				var mainWindow = new MainWindow();
				mainWindow.Show();
				this.Close();
			}
		}

		private void ShowCatalog()
		{
			var catalogPage = new CarCatalogPage(clientId, connectionString);
			MainFrame.Navigate(catalogPage);
		}
	}
}