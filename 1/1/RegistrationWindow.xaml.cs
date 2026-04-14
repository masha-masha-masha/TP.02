using System;
using System.Windows;
using Npgsql;

namespace Прокат_авто
{ 
	public partial class RegistrationWindow : Window
	{
		private string connectionString;

		// Свойство для возврата зарегистрированного логина
		public string RegisteredLogin { get; private set; }

		public RegistrationWindow(string connectionString)
		{
			InitializeComponent();
			this.connectionString = connectionString;
			this.Owner = Application.Current.MainWindow;
		}

		// Кнопка регистрации
		private void btnRegister_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string login = txtLogin.Text.Trim();
				string password = txtPassword.Password;
				string confirmPassword = txtConfirmPassword.Password;

				// Валидация
				if (string.IsNullOrEmpty(login))
				{
					txtStatus.Text = "Введите логин!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Red;
					txtLogin.Focus();
					return;
				}

				if (login.Length < 3)
				{
					txtStatus.Text = "Логин должен быть не менее 3 символов!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Red;
					txtLogin.Focus();
					return;
				}

				if (string.IsNullOrEmpty(password))
				{
					txtStatus.Text = "Введите пароль!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Red;
					txtPassword.Focus();
					return;
				}

				if (password.Length < 4)
				{
					txtStatus.Text = "Пароль должен быть не менее 4 символов!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Red;
					txtPassword.Focus();
					return;
				}

				if (password != confirmPassword)
				{
					txtStatus.Text = "Пароли не совпадают!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Red;
					txtPassword.Focus();
					return;
				}

				// Регистрация в БД
				txtStatus.Text = "Регистрация...";
				txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
				btnRegister.IsEnabled = false;
				btnCancel.IsEnabled = false;

				if (RegisterClient(login, password))
				{
					RegisteredLogin = login;
					txtStatus.Text = "Регистрация успешна!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Green;

					// Закрываем окно с успехом
					DialogResult = true;
					this.Close();
				}
				else
				{
					txtStatus.Text = "Логин уже существует!";
					txtStatus.Foreground = System.Windows.Media.Brushes.Red;
					btnRegister.IsEnabled = true;
					btnCancel.IsEnabled = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
							  MessageBoxButton.OK, MessageBoxImage.Error);
				btnRegister.IsEnabled = true;
				btnCancel.IsEnabled = true;
			}
		}

		// Регистрация в базе данных
		private bool RegisterClient(string login, string password)
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();

					// Проверка существования логина
					string checkSql = "SELECT COUNT(*) FROM public.client_authorization WHERE login = @login";
					using (var checkCmd = new NpgsqlCommand(checkSql, conn))
					{
						checkCmd.Parameters.AddWithValue("@login", login);
						long count = Convert.ToInt64(checkCmd.ExecuteScalar());
						if (count > 0)
						{
							return false; // Логин уже существует
						}
					}

					// Добавление нового клиента
					string insertSql = @"
                        INSERT INTO public.client_authorization (login, password)
                        VALUES (@login, @password)";

					using (var cmd = new NpgsqlCommand(insertSql, conn))
					{
						cmd.Parameters.AddWithValue("@login", login);
						cmd.Parameters.AddWithValue("@password", password);
						cmd.ExecuteNonQuery();
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка регистрации: {ex.Message}");
				throw;
			}
		}

		// Кнопка отмены
		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			this.Close();
		}

		// Обработка Enter в поле логина
		private void TxtLogin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				txtPassword.Focus();
			}
		}

		// Обработка Enter в поле пароля
		private void TxtPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				txtConfirmPassword.Focus();
			}
		}

		// Обработка Enter в поле подтверждения пароля
		private void TxtConfirmPassword_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				btnRegister_Click(sender, e);
			}
		}
	}
}