using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Npgsql;

namespace Прокат_авто
{
	public partial class AddPaymentWindow : Window
	{
		private int contractId;
		private string connectionString;
		private ObservableCollection<PaymentMethodItem> paymentMethods;
		private ObservableCollection<PaymentTypeItem> paymentTypes;

		public AddPaymentWindow(int contractId, string connectionString)
		{
			InitializeComponent();
			this.contractId = contractId;
			this.connectionString = connectionString;

			dpPaymentDate.SelectedDate = DateTime.Today;

			LoadPaymentMethods();
			LoadPaymentTypes();
		}

		private void LoadPaymentMethods()
		{
			try
			{
				paymentMethods = new ObservableCollection<PaymentMethodItem>();

				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					string sql = "SELECT \"id_ payment method\", title FROM public.payment_method ORDER BY title";

					using (var cmd = new NpgsqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							paymentMethods.Add(new PaymentMethodItem
							{
								Id = reader.GetInt32(0),
								Title = reader.GetString(1)
							});
						}
					}
				}

				cmbPaymentMethod.ItemsSource = paymentMethods;
				if (paymentMethods.Count > 0)
					cmbPaymentMethod.SelectedIndex = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки методов оплаты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadPaymentTypes()
		{
			try
			{
				paymentTypes = new ObservableCollection<PaymentTypeItem>();

				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();
					string sql = "SELECT id_tipe, title FROM public.type ORDER BY title";

					using (var cmd = new NpgsqlCommand(sql, conn))
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							paymentTypes.Add(new PaymentTypeItem
							{
								Id = reader.GetInt32(0),
								Title = reader.GetString(1)
							});
						}
					}
				}

				cmbPaymentType.ItemsSource = paymentTypes;
				if (paymentTypes.Count > 0)
					cmbPaymentType.SelectedIndex = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки типов платежей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void txtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			// Разрешаем только цифры и десятичную точку
			foreach (char c in e.Text)
			{
				if (!char.IsDigit(c) && c != ',' && c != '.')
				{
					e.Handled = true;
					return;
				}
			}
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			// Проверка ввода
			if (!dpPaymentDate.SelectedDate.HasValue)
			{
				MessageBox.Show("Выберите дату платежа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (string.IsNullOrWhiteSpace(txtAmount.Text))
			{
				MessageBox.Show("Введите сумму платежа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			decimal amount;
			if (!decimal.TryParse(txtAmount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out amount) || amount <= 0)
			{
				MessageBox.Show("Введите корректную сумму платежа (больше 0)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (cmbPaymentMethod.SelectedItem == null)
			{
				MessageBox.Show("Выберите метод оплаты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (cmbPaymentType.SelectedItem == null)
			{
				MessageBox.Show("Выберите тип платежа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				int paymentMethodId = ((PaymentMethodItem)cmbPaymentMethod.SelectedItem).Id;
				int paymentTypeId = ((PaymentTypeItem)cmbPaymentType.SelectedItem).Id;
				DateTime paymentDate = dpPaymentDate.SelectedDate.Value;

				using (var conn = new NpgsqlConnection(connectionString))
				{
					conn.Open();

					using (var transaction = conn.BeginTransaction())
					{
						// 1. Добавляем запись в таблицу payment
						string insertPaymentSql = @"
                            INSERT INTO public.payment (id_payment_method, id_type, payment_date, amount)
                            VALUES (@paymentMethodId, @paymentTypeId, @paymentDate, @amount)
                            RETURNING id_payment";

						int paymentId;
						using (var cmd = new NpgsqlCommand(insertPaymentSql, conn, transaction))
						{
							cmd.Parameters.AddWithValue("@paymentMethodId", paymentMethodId);
							cmd.Parameters.AddWithValue("@paymentTypeId", paymentTypeId);
							cmd.Parameters.AddWithValue("@paymentDate", paymentDate);
							cmd.Parameters.AddWithValue("@amount", amount);

							paymentId = (int)cmd.ExecuteScalar();
						}

						// 2. Связываем платеж с договором
						string insertContractPaymentSql = @"
                            INSERT INTO public.""contract-payment"" (id_contract, id_payment)
                            VALUES (@contractId, @paymentId)";

						using (var cmd = new NpgsqlCommand(insertContractPaymentSql, conn, transaction))
						{
							cmd.Parameters.AddWithValue("@contractId", contractId);
							cmd.Parameters.AddWithValue("@paymentId", paymentId);
							cmd.ExecuteNonQuery();
						}

						transaction.Commit();
					}
				}

				MessageBox.Show($"Платеж на сумму {amount:F2} руб. успешно добавлен!",
							  "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

				DialogResult = true;
				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при добавлении платежа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}

	public class PaymentMethodItem
	{
		public int Id { get; set; }
		public string Title { get; set; }
	}

	public class PaymentTypeItem
	{
		public int Id { get; set; }
		public string Title { get; set; }
	}
}