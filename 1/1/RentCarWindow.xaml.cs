using System;
using System.Windows;
using Npgsql;

namespace Прокат_авто
{
	public partial class RentCarWindow : Window
	{
		private int clientId;
		private int carId;
		private string connectionString;
		private decimal pricePerDay;

		public RentCarWindow(int clientId, int carId, string connectionString)
		{
			InitializeComponent();
			this.clientId = clientId;
			this.carId = carId;
			this.connectionString = connectionString;

			// Устанавливаем даты в code-behind, а не в XAML
			dpStartDate.SelectedDate = DateTime.Today;
			dpEndDate.SelectedDate = DateTime.Today.AddDays(1);

			dpStartDate.SelectedDateChanged += DpStartDate_SelectedDateChanged;
			dpEndDate.SelectedDateChanged += DpEndDate_SelectedDateChanged;

			LoadCarPrice();
			CalculateTotal();
		}

		private void LoadCarPrice()
		{
			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					string sql = @"
                        SELECT COALESCE(r.price, 2000)
                        FROM public.car c
                        LEFT JOIN public.rate r ON c.id_rate = r.id_rate
                        WHERE c.id_car = @carId";

					using (var cmd = new NpgsqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@carId", carId);
						var result = cmd.ExecuteScalar();

						if (result != null && result != DBNull.Value)
						{
							pricePerDay = Convert.ToDecimal(result);
						}
						else
						{
							pricePerDay = 2000;
						}

						txtPricePerDay.Text = $"Цена за день: {pricePerDay:F2} руб.";
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки цены: {ex.Message}");
				pricePerDay = 2000;
				txtPricePerDay.Text = $"Цена за день: {pricePerDay:F2} руб. (по умолчанию)";
			}
		}

		private void DpStartDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			CalculateTotal();
		}

		private void DpEndDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			CalculateTotal();
		}

		private void CalculateTotal()
		{
			if (dpStartDate.SelectedDate.HasValue && dpEndDate.SelectedDate.HasValue)
			{
				DateTime start = dpStartDate.SelectedDate.Value;
				DateTime end = dpEndDate.SelectedDate.Value;

				if (end <= start)
				{
					txtTotalAmount.Text = "Ошибка: дата окончания должна быть позже даты начала";
					txtTotalAmount.Foreground = System.Windows.Media.Brushes.Red;
					return;
				}

				int days = (end - start).Days;
				decimal total = days * pricePerDay;

				txtDaysCount.Text = $"Количество дней: {days}";
				txtTotalAmount.Text = $"Итого к оплате: {total:F2} руб.";
				txtTotalAmount.Foreground = System.Windows.Media.Brushes.Green;
			}
		}

		private void btnConfirm_Click(object sender, RoutedEventArgs e)
		{
			if (!dpStartDate.SelectedDate.HasValue || !dpEndDate.SelectedDate.HasValue)
			{
				MessageBox.Show("Выберите даты аренды!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			DateTime startDate = dpStartDate.SelectedDate.Value;
			DateTime endDate = dpEndDate.SelectedDate.Value;

			if (endDate <= startDate)
			{
				MessageBox.Show("Дата окончания должна быть позже даты начала!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			int days = (endDate - startDate).Days;
			decimal totalAmount = days * pricePerDay;

			try
			{
				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();

					using (var transaction = conn.BeginTransaction())
					{
						// Получаем ID сотрудника
						string getEmployeeSql = "SELECT id_employee FROM public.employee LIMIT 1";
						int employeeId = 0;
						using (var cmd = new NpgsqlCommand(getEmployeeSql, conn, transaction))
						{
							var result = cmd.ExecuteScalar();
							if (result != null && result != DBNull.Value)
								employeeId = Convert.ToInt32(result);
							else
							{
								MessageBox.Show("В системе нет зарегистрированных сотрудников!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
								return;
							}
						}

						// Получаем ID статуса "Активен"
						string getStatusSql = "SELECT id_starus_of_the_agreement FROM public.starus_of_the_agreement WHERE title = 'Активен'";
						int statusId = 0;
						using (var cmd = new NpgsqlCommand(getStatusSql, conn, transaction))
						{
							var result = cmd.ExecuteScalar();
							if (result != null && result != DBNull.Value)
							{
								statusId = Convert.ToInt32(result);
							}
							else
							{
								// Создаем статус "Активен", если его нет
								string insertStatusSql = "INSERT INTO public.starus_of_the_agreement (title) VALUES ('Активен') RETURNING id_starus_of_the_agreement";
								using (var insertCmd = new NpgsqlCommand(insertStatusSql, conn, transaction))
								{
									statusId = Convert.ToInt32(insertCmd.ExecuteScalar());
								}
							}
						}

						// Получаем ID статуса автомобиля "В аренде"
						string getCarStatusSql = "SELECT id_status FROM public.status WHERE title = 'В аренде'";
						int carStatusId = 0;
						using (var cmd = new NpgsqlCommand(getCarStatusSql, conn, transaction))
						{
							var result = cmd.ExecuteScalar();
							if (result != null && result != DBNull.Value)
							{
								carStatusId = Convert.ToInt32(result);
							}
							else
							{
								// Создаем статус "В аренде", если его нет
								string insertStatusSql = "INSERT INTO public.status (title) VALUES ('В аренде') RETURNING id_status";
								using (var insertCmd = new NpgsqlCommand(insertStatusSql, conn, transaction))
								{
									carStatusId = Convert.ToInt32(insertCmd.ExecuteScalar());
								}
							}
						}

						// Создаем договор
						string insertSql = @"
                            INSERT INTO public.contract (id_client, id_employee, id_car, id_starus_of_the_agreement, 
                                                         ""rental amount"", ""start date"", ""end date"", ""total amount"")
                            VALUES (@clientId, @employeeId, @carId, @statusId, @rentalAmount, @startDate, @endDate, @totalAmount)
                            RETURNING id_contract";

						int contractId;
						using (var cmd = new NpgsqlCommand(insertSql, conn, transaction))
						{
							cmd.Parameters.AddWithValue("@clientId", clientId);
							cmd.Parameters.AddWithValue("@employeeId", employeeId);
							cmd.Parameters.AddWithValue("@carId", carId);
							cmd.Parameters.AddWithValue("@statusId", statusId);
							cmd.Parameters.AddWithValue("@rentalAmount", pricePerDay);
							cmd.Parameters.AddWithValue("@startDate", startDate);
							cmd.Parameters.AddWithValue("@endDate", endDate);
							cmd.Parameters.AddWithValue("@totalAmount", totalAmount);

							contractId = (int)cmd.ExecuteScalar();
						}

						// Обновляем статус автомобиля на "В аренде"
						string updateCarSql = "UPDATE public.car SET id_status = @carStatusId WHERE id_car = @carId";
						using (var updateCmd = new NpgsqlCommand(updateCarSql, conn, transaction))
						{
							updateCmd.Parameters.AddWithValue("@carStatusId", carStatusId);
							updateCmd.Parameters.AddWithValue("@carId", carId);
							updateCmd.ExecuteNonQuery();
						}

						transaction.Commit();

						MessageBox.Show($"Договор №{contractId} успешно оформлен!\nОбщая сумма: {totalAmount:F2} руб.",
									  "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
						DialogResult = true;
						Close();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при оформлении договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}