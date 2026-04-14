using System;
using System.Windows.Controls;
using Npgsql;

namespace Прокат_авто
{
	public partial class ClientProfilePage : Page
	{
		private int clientId;
		private string connectionString;

		public ClientProfilePage(int clientId, string connectionString)
		{
			InitializeComponent();
			this.clientId = clientId;
			this.connectionString = connectionString;
			LoadClientData();
		}

		private void LoadClientData()
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();

					// Исправленный SQL запрос с правильным именем таблицы drivers_license
					string sql = @"
                        SELECT 
                            c.surname, 
                            c.name, 
                            c.patronymic, 
                            c.phone_number, 
                            c.address, 
                            c.email,
                            p.series,
                            p.number as passport_number,
                            p.issued_by_whom,
                            p.when_issued,
                            dl.number as license_number,
                            dl.category,
                            dl.date_of_issue,
                            dl.validity_period
                        FROM public.client c
                        LEFT JOIN public.passport p ON c.id_passport = p.id_passport
                        LEFT JOIN public.drivers_license dl ON c.id_drivers_license = dl.id_drivers_license
                        WHERE c.id_client = @clientId";

					using (var cmd = new NpgsqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@clientId", clientId);
						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								// ФИО - отдельные поля
								txtSurname.Text = reader.IsDBNull(0) ? "Не указано" : reader.GetString(0);
								txtName.Text = reader.IsDBNull(1) ? "Не указано" : reader.GetString(1);
								txtPatronymic.Text = reader.IsDBNull(2) ? "Не указано" : reader.GetString(2);

								// Контакты
								txtPhone.Text = reader.IsDBNull(3) ? "Не указан" : reader.GetString(3);
								txtAddress.Text = reader.IsDBNull(4) ? "Не указан" : reader.GetString(4);
								txtEmail.Text = reader.IsDBNull(5) ? "Не указан" : reader.GetString(5);

								// Паспортные данные
								if (!reader.IsDBNull(6) && !reader.IsDBNull(7))
								{
									decimal series = reader.GetDecimal(6);
									decimal number = reader.GetDecimal(7);
									txtPassportSeries.Text = series.ToString("0000");
									txtPassportNumber.Text = number.ToString("000000");
									txtIssuedBy.Text = reader.IsDBNull(8) ? "Не указано" : reader.GetString(8);
									txtWhenIssued.Text = reader.IsDBNull(9) ? "Не указано" : reader.GetString(9);
								}
								else
								{
									SetDefaultPassportData();
								}

								// Водительское удостоверение
								if (!reader.IsDBNull(10))
								{
									txtLicenseNumber.Text = reader.GetString(10);
									txtCategory.Text = reader.IsDBNull(11) ? "Не указана" : reader.GetString(11);
									txtLicenseIssueDate.Text = reader.IsDBNull(12) ? "Не указана" : reader.GetDateTime(12).ToShortDateString();
									txtValidityPeriod.Text = reader.IsDBNull(13) ? "Не указан" : reader.GetDateTime(13).ToShortDateString();
								}
								else
								{
									SetDefaultLicenseData();
								}
							}
							else
							{
								// Клиент не найден
								SetDefaultClientData();
								SetDefaultPassportData();
								SetDefaultLicenseData();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка загрузки профиля: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

				SetDefaultClientData();
				SetDefaultPassportData();
				SetDefaultLicenseData();

				// Показываем сообщение об ошибке
				txtSurname.Text = $"Ошибка: {ex.Message}";
			}
		}

		private void SetDefaultClientData()
		{
			txtSurname.Text = "Данные не найдены";
			txtName.Text = "Данные не найдены";
			txtPatronymic.Text = "Данные не найдены";
			txtPhone.Text = "Данные не найдены";
			txtAddress.Text = "Данные не найдены";
			txtEmail.Text = "Данные не найдены";
		}

		private void SetDefaultPassportData()
		{
			txtPassportSeries.Text = "Данные не найдены";
			txtPassportNumber.Text = "Данные не найдены";
			txtIssuedBy.Text = "Данные не найдены";
			txtWhenIssued.Text = "Данные не найдены";
		}

		private void SetDefaultLicenseData()
		{
			txtLicenseNumber.Text = "Данные не найдены";
			txtCategory.Text = "Данные не найдены";
			txtLicenseIssueDate.Text = "Данные не найдены";
			txtValidityPeriod.Text = "Данные не найдены";
		}
	}
}