using System;
using System.Windows;
using System.Windows.Input;
using Npgsql;

namespace Прокат_авто
{
	public partial class MainWindow : Window
	{
		private string connectionString = "Host=localhost;Port=5432;Database=avto;Username=postgres;Password=wer23";
		private int currentUserId = 0;

		public MainWindow()
		{
			InitializeComponent();

			System.Diagnostics.Debug.WriteLine("=== MainWindow инициализирован ===");
			System.Diagnostics.Debug.WriteLine($"btnLogin = {(btnLogin != null ? "OK" : "NULL")}");
			System.Diagnostics.Debug.WriteLine($"btnRegister = {(btnRegister != null ? "OK" : "NULL")}");
		}

		// Обработка нажатия Enter
		private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && btnLogin != null)
			{
				btnLogin_Click(sender, e);
			}
		}

		// Проверка для КЛИЕНТА
		private bool CheckClientLogin(string login, string password, out int clientId)
		{
			clientId = 0;
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();

					string sql = @"
                        SELECT c.id_client 
                        FROM public.client c
                        INNER JOIN public.client_authorization ca ON c.id_client_authorization = ca.""id_client authorization""
                        WHERE ca.login = @login AND ca.password = @password";

					using (var cmd = new NpgsqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@login", login);
						cmd.Parameters.AddWithValue("@password", password);

						object result = cmd.ExecuteScalar();
						System.Diagnostics.Debug.WriteLine($"Результат запроса: {result}");

						if (result != null && result != DBNull.Value)
						{
							clientId = Convert.ToInt32(result);
							System.Diagnostics.Debug.WriteLine($"Найден ID клиента: {clientId}");
							return true;
						}
						else
						{
							txtError.Text = "Неверный логин или пароль!";
							return false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка CheckClientLogin: {ex.Message}");
				txtError.Text = $"Ошибка БД: {ex.Message}";
				return false;
			}
		}

		// ГЛАВНЫЙ ОБРАБОТЧИК ВХОДА
		private void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			if (txtLogin == null || txtPassword == null || txtError == null)
			{
				System.Diagnostics.Debug.WriteLine("Ошибка: элементы не инициализированы");
				return;
			}

			string login = txtLogin.Text.Trim();
			string password = txtPassword.Password;

			if (string.IsNullOrEmpty(login))
			{
				txtError.Text = "Введите логин!";
				txtLogin.Focus();
				return;
			}
			if (string.IsNullOrEmpty(password))
			{
				txtError.Text = "Введите пароль!";
				txtPassword.Focus();
				return;
			}

			txtError.Text = "Проверка...";
			txtError.Foreground = System.Windows.Media.Brushes.Blue;

			int tempClientId;
			if (CheckClientLogin(login, password, out tempClientId))
			{
				currentUserId = tempClientId;
				txtError.Text = "Вход выполнен!";
				txtError.Foreground = System.Windows.Media.Brushes.Green;

				// Открываем окно клиента
				ClientProfileWindow clientWindow = new ClientProfileWindow(currentUserId, connectionString);
				clientWindow.Show();
				this.Close();
			}
			else
			{
				txtError.Text = "Неверный логин или пароль!";
				txtError.Foreground = System.Windows.Media.Brushes.Red;
			}
		}

		// Регистрация
		private void btnRegister_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				RegistrationWindow regWindow = new RegistrationWindow(connectionString);
				regWindow.Owner = this;
				bool? result = regWindow.ShowDialog();

				if (result == true && !string.IsNullOrEmpty(regWindow.RegisteredLogin))
				{
					txtLogin.Text = regWindow.RegisteredLogin;
					txtPassword.Focus();
					txtError.Text = "Регистрация успешна! Теперь войдите.";
					txtError.Foreground = System.Windows.Media.Brushes.Green;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при регистрации: {ex.Message}",
							  "Ошибка",
							  MessageBoxButton.OK,
							  MessageBoxImage.Error);
			}
		}

		// Загрузка окна
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					if (txtError != null)
					{
						txtError.Text = "Введите логин и пароль";
						txtError.Foreground = System.Windows.Media.Brushes.Green;
					}
					System.Diagnostics.Debug.WriteLine("БД подключена");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка БД: {ex.Message}");
				if (txtError != null)
				{
					txtError.Text = "Нет подключения к БД!";
					txtError.Foreground = System.Windows.Media.Brushes.Red;
				}
			}
		}
	}
}